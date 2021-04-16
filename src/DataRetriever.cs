using Octokit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps;
using YOSHI.CommunityData;
using yoshi_revision.src;
using yoshi_revision.src.Util;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for retrieving data from GitHub. 
    /// </summary>
    public static class DataRetriever
    {
        private static readonly GitHubClient Client;

        static DataRetriever()
        {
            try
            {
                Client = new GitHubClient(new ProductHeaderValue("yoshi"));

                // Set the authentication token from GitHub for the GitHub REST API
                Credentials tokenAuth = new Credentials(""); // TODO: Read GitHub private key from local file or use application key
                Client.Credentials = tokenAuth;

                // Set the authentication token from Bing Maps used for Geocoding
                MapService.ServiceToken = ""; // TODO: Read private key from local file

            } catch (Exception e)
            {
                throw new Exception("Error during client initialization.", e);
            }
        }

        /// <summary>
        /// Method that retrieves all GitHub data that is needed to compute only the structure metrics and modifies the 
        /// community data to store that information. 
        /// </summary>
        /// <param name="community">The community for which we need to retrieve GitHub Data.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task RetrieveStructureData(Community community)
        {
            //throw new NotImplementedException();
            // TODO: Split structure data and miscellaneous data
            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;
            GitHubData data = community.Data;

            try
            {
                data.Repo = await Client.Repository.Get(repoName, repoOwner);
                data.Contributors = await Client.Repository.GetAllContributors(repoName, repoOwner);
                data.Collaborators = await Client.Repository.Collaborator.GetAll(repoName, repoOwner);
                data.Milestones = await Client.Issue.Milestone.GetAllForRepository(repoName, repoOwner);
                data.Commits = await Client.Repository.Commit.GetAll(repoName, repoOwner);
                data.CommitComments = await Client.Repository.Comment.GetAllForRepository(repoName, repoOwner);
                data.PullReqComments = await Client.PullRequest.ReviewComment.GetAllForRepository(repoName, repoOwner);
                data.Watchers = await Client.Activity.Watching.GetAllWatchers(repoName, repoOwner);
                data.Stargazers = await Client.Activity.Starring.GetAllStargazers(repoName, repoOwner); // TODO: Check whether we should get those with timestamps or not
                await RetrieveDataPerMember(data);
            } 
            catch (Exception e)
            {
                throw new Exception("Something went wrong while retrieving data from GitHub", e);
            }
        }

        /// <summary>
        /// Method that retrieves all GitHub data that is needed to compute all but structure metrics and modifies the 
        /// community data to store that information. 
        /// </summary>
        /// <param name="community">The community for which we need to retrieve GitHub Data.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public static async Task RetrieveMiscellaneousData(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For all repository members (contributors and collaborators) we retrieve their followers (i.e., who's 
        /// following them) and following (i.e., who they're following)
        /// </summary>
        /// <param name="data">The data object of the community in which we store all retrieved GitHub data.</param>
        private static async Task RetrieveDataPerMember(GitHubData data)
        {
            // TODO: Retrieve locations and transform them to GeoCoordinates
            // TODO: If possible, check whether data.Collaborators and contributorsAsUsers can be combined to reduce
            // code duplication. (Would result in looping twice over contributors)
            // TODO: Remove potential duplicates between collaborators and contributors (combine information from 
            // aliases, but remove duplicate user entries, i.e., people marked as both a collaborator and as a contributor)
            List<User> updatedCollaborators = new List<User>();
            foreach (User collaborator in data.Collaborators)
            {
                string username = collaborator.Login;
                if (username != null)
                {
                    updatedCollaborators.Add(collaborator);
                    IReadOnlyList<User> followers = await Client.User.Followers.GetAll(username); // Lists the given user's followers
                    IReadOnlyList<User> following = await Client.User.Followers.GetAllFollowing(username); // Lists all users that the given user is following
                    IReadOnlyList<Repository> repositories = await Client.Repository.GetAllForUser(username);
                    GeoCoordinate coordinate = await GeoService.GetLongitudeLatitude(collaborator.Location);
                    data.MapUserFollowers.Add(username, followers);
                    data.MapUserFollowing.Add(username, following);
                    data.MapUserRepositories.Add(username, repositories);
                    data.Coordinates.Add(coordinate);
                }
            }
            data.Collaborators = updatedCollaborators; // Removed all collaborators without login

            List<RepositoryContributor> updatedContributors = new List<RepositoryContributor>();
            List<User> contributorsAsUsers = new List<User>();
            foreach (RepositoryContributor contributor in data.Contributors)
            {
                string username = contributor.Login;
                if (username != null)
                {
                    updatedContributors.Add(contributor);
                    IReadOnlyList<User> followers = await Client.User.Followers.GetAll(username); // Lists the given user's followers
                    IReadOnlyList<User> following = await Client.User.Followers.GetAllFollowing(username); // Lists all users that the given user is following
                    IReadOnlyList<Repository> repositories = await Client.Repository.GetAllForUser(username);
                    // The class RepositoryContributor does not contain the contributors location, so we retrieve
                    // them as a user too.
                    User user = await Client.User.Get(username);
                    GeoCoordinate coordinate = await GeoService.GetLongitudeLatitude(user.Location);
                    data.MapUserFollowers.Add(username, followers);
                    data.MapUserFollowing.Add(username, following);
                    data.MapUserRepositories.Add(username, repositories);
                    contributorsAsUsers.Add(user);
                    data.Coordinates.Add(coordinate);
                }
            }
            data.Contributors = updatedContributors; // Removed all contributors without login
            data.ContributorsAsUsers = contributorsAsUsers; // Store the data of contributors as User class (more info)
        }

    }
}
