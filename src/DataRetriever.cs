using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Services.Maps;
using YOSHI.CommunityData;
using yoshi_revision.src.Util;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for retrieving data from GitHub. 
    /// </summary>
    public static class DataRetriever
    {
        public static readonly GitHubClient Client;
        // Default 24-hour operations with a basic Windows App, Non-profit, and Education key.
        // Info about rate limiting: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-api-best-practices
        public static int BingRequestsLeft { get; set; } = 50000;
        private static readonly ApiOptions MaxSizeBatches = new ApiOptions // allows us to fetch with 100 at a time
        {
            PageSize = 100
        };

        static DataRetriever()
        {
            try
            {
                // Read the GitHub Access Token and the Bing Maps Key from Windows Environment Variables
                string githubAccessToken = Environment.GetEnvironmentVariable("YOSHI_GitHubAccessToken"); // TODO: Maybe use application / oauth key
                string bingMapsKey = Environment.GetEnvironmentVariable("YOSHI_BingMapsKey");

                // Set the GitHub Client and set the authentication token from GitHub for the GitHub REST API
                Client = new GitHubClient(new ProductHeaderValue("yoshi"));
                Credentials tokenAuth = new Credentials(githubAccessToken);
                Client.Credentials = tokenAuth;

                // Set the authentication token from Bing Maps used for Geocoding
                MapService.ServiceToken = bingMapsKey;
            }
            catch (Exception e)
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
            // NOTE: Manual inspection of projects, requirements are at least 100 commits, at least 10 members, at least 50,000 LOC, must use milestones and issues
            try
            {
                data.Commits = await RateLimitHandler.Delegate(Client.Repository.Commit.GetAll, repoOwner, repoName, MaxSizeBatches);
                // There must be at least 100 commits
                if (data.Commits.Count < 100)
                {
                    throw new InvalidRepositoryException("This repository has fewer than 100 commits.");
                }
                (data.Members, data.MemberUsernames) = await GetAllMembers(data.Commits);
                // There must be at least 10 members
                if (data.MemberUsernames.Count < 10)
                {
                    throw new InvalidRepositoryException("This repository has fewer than 10 members.");
                }
                data.Repo = await RateLimitHandler.Delegate(Client.Repository.Get, repoOwner, repoName);
                data.Milestones = await RateLimitHandler.Delegate(Client.Issue.Milestone.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                // There must be at least one milestone
                if (data.Milestones.Count < 1)
                {
                    throw new InvalidRepositoryException("This repository does not have any milestones.");
                }
                // NOTE: Commit comments commit_id == commit SHA
                data.CommitComments = await RateLimitHandler.Delegate(Client.Repository.Comment.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                data.MapPullReqsToComments = await MapPullRequestComments(repoOwner, repoName);
                data.Watchers = await RateLimitHandler.Delegate(Client.Activity.Watching.GetAllWatchers, repoOwner, repoName, MaxSizeBatches);
                data.Stargazers = await RateLimitHandler.Delegate(Client.Activity.Starring.GetAllStargazers, repoOwner, repoName, MaxSizeBatches); // TODO: Check whether we should get those with timestamps or not
                (data.MapUserFollowers, data.MapUserFollowing, data.MapUserRepositories, data.Coordinates) = await RetrieveDataPerMember(data);
                // There must be enough location data available to compute dispersion. TODO: Determine the threshold (maybe as percentage)
                if (data.Coordinates.Count < 2)
                {
                    throw new InvalidRepositoryException("Not enough location data available.");
                }
            }
            catch (InvalidRepositoryException)
            {
                throw;
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
        /// This method retrieves all User objects and usernames for all committers.
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A tuple containing a list of users and a list of usernames.</returns>
        private static async Task<(List<User>, HashSet<string>)> GetAllMembers(IReadOnlyList<GitHubCommit> commits)
        {
            // Get the user info of all members that have made at least one commit
            HashSet<string> usernames = new HashSet<string>();
            foreach (GitHubCommit commit in commits)
            {
                if (commit.Committer != null && commit.Committer.Login != null)
                {
                    usernames.Add(commit.Committer.Login);
                }
                if (commit.Author != null && commit.Author.Login != null)
                {
                    usernames.Add(commit.Author.Login);
                }
            }
            // TODO: Apply alias resolution
            List<User> members = new List<User>();
            HashSet<string> memberUsernames = new HashSet<string>(); // A separate list to exclude usernames that cause exceptions
            foreach (string username in usernames)
            {
                try
                {
                    User user = await RateLimitHandler.Delegate(Client.User.Get, username);
                    members.Add(user);
                    memberUsernames.Add(username);
                }
                catch
                {
                    // Skip the usernames that cause exceptions
                    continue;
                }
            }
            return (members, memberUsernames);
        }

        /// <summary>
        /// For all repository members we retrieve their followers (i.e., who's following them) and following 
        /// (i.e., who they're following), we retrieve the repositories they worked on, and we retrieve their coordinates .
        /// </summary>
        /// <param name="data">The data object of the community in which we store all retrieved GitHub data.</param>
        private static async Task<(
            Dictionary<string, HashSet<string>> mapUserFollowers,
            Dictionary<string, HashSet<string>> mapUserFollowing,
            Dictionary<string, IReadOnlyList<Repository>> mapUserRepositories,
            List<GeoCoordinate> coordinates)>
            RetrieveDataPerMember(GitHubData data)
        {
            Dictionary<string, HashSet<string>> mapUserFollowers = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> mapUserFollowing = new Dictionary<string, HashSet<string>>();
            Dictionary<string, IReadOnlyList<Repository>> mapUserRepositories = new Dictionary<string, IReadOnlyList<Repository>>();
            List<GeoCoordinate> coordinates = new List<GeoCoordinate>();

            foreach (User member in data.Members) // NOTE: We loop over all user objects instead of usernames to use location data
            {
                string username = member.Login;

                // Get the given user's followers
                IReadOnlyList<User> followers = await RateLimitHandler.Delegate(Client.User.Followers.GetAll, username, MaxSizeBatches);
                // Limit the following users to members that are also part of the current repository
                HashSet<string> followersNames = new HashSet<string>();
                foreach (User follower in followers)
                {
                    if (follower.Login != null && data.MemberUsernames.Contains(follower.Login))
                    {
                        followersNames.Add(follower.Login);
                    }
                }

                // Get the given user's users that they're following
                IReadOnlyList<User> following = await RateLimitHandler.Delegate(Client.User.Followers.GetAllFollowing, username, MaxSizeBatches);
                // Limit the following users to members that are also part of the current repository
                HashSet<string> followingNames = new HashSet<string>();
                foreach (User followingUser in following)
                {
                    if (followingUser.Login != null && data.MemberUsernames.Contains(followingUser.Login))
                    {
                        followingNames.Add(followingUser.Login);
                    }
                }

                // TODO: Check whether these repositories are all repositories that the user has at least one commit to the main branch / gh-pages
                IReadOnlyList<Repository> repositories = await RateLimitHandler.Delegate(Client.Repository.GetAllForUser, username, MaxSizeBatches); // TODO: use repositories per member to retrieve main programming languages (skillsets)
                // TODO: This method only retrieves owned repositories for a user, not all repositories that they worked on.
                
                // Retrieve the member's coordinates
                GeoCoordinate coordinate;
                try
                {
                    coordinate = await GetLongitudeLatitude(member.Location);
                }
                catch (ArgumentException)
                {
                    // Continue with the next user if this user was causing an exception
                    Console.WriteLine("Could not retrieve the location from {0} in repo {1}", username, data.Repo.Name);
                    continue;
                }
                catch (Exception)
                {
                    // Throw an exception, blocking the application, if we reached the Bing Request limit
                    throw;
                }

                // Store all user data
                mapUserFollowers.Add(username, followersNames);
                mapUserFollowing.Add(username, followingNames);
                mapUserRepositories.Add(username, repositories);
                if (coordinate != null)
                {
                    coordinates.Add(coordinate);
                }
            }

            return (mapUserFollowers, mapUserFollowing, mapUserRepositories, coordinates);
        }

        /// <summary>
        /// This method retrieves all pull requests for a repository, and then retrieves the pull request review comments
        /// for each pull request and maps them in a dictionary.
        /// </summary>
        /// <param name="repoOwner">Repository owner</param>
        /// <param name="repoName">Repository name</param>
        /// <returns>A dictionary mapping pull requests to pull request review comments.</returns>
        private static async Task<Dictionary<PullRequest, IReadOnlyList<PullRequestReviewComment>>> MapPullRequestComments(string repoOwner, string repoName)
        {
            // We want all pull requests, since they often do not get closed correctly or closed at all, even if they're merged
            PullRequestRequest stateFilter = new PullRequestRequest { State = ItemStateFilter.All };
            IReadOnlyList<PullRequest> pullRequests = await RateLimitHandler.Delegate(Client.PullRequest.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);

            // Map each pull request to its corresponding comments
            Dictionary<PullRequest, IReadOnlyList<PullRequestReviewComment>> mapPullReqsToComments = new Dictionary<PullRequest, IReadOnlyList<PullRequestReviewComment>>();
            foreach (PullRequest pullRequest in pullRequests)
            {
                var pullRequestComments = await RateLimitHandler.Delegate(Client.PullRequest.ReviewComment.GetAll, repoOwner, repoName, pullRequest.Number, MaxSizeBatches);
                mapPullReqsToComments.Add(pullRequest, pullRequestComments);
            }

            return mapPullReqsToComments;
        }

        /// <summary>
        /// This method uses a Geocoding API to perform forward geocoding, i.e., enter an address and obtain coordinates.
        /// 
        /// Bing Maps TOU: https://www.microsoft.com/en-us/maps/product/terms-april-2011
        /// </summary>
        /// <param name="address">The address of which we want the coordinates.</param>
        /// <returns>A GeoCoordinate containing the longitude and latitude found from the given address.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the returned status in MapLocationFinderResult is anything but "Success".</exception>
        private static async Task<GeoCoordinate> GetLongitudeLatitude(string address)
        {
            if (BingRequestsLeft > 50) // Give ourselves a small buffer to not go over the limit.
            {
                BingRequestsLeft--;
                MapLocationFinderResult result = await MapLocationFinder.FindLocationsAsync(address, null, 1);
                if (result.Status == MapLocationFinderStatus.Success)
                {
                    GeoCoordinate coordinate = new GeoCoordinate(
                    result.Locations[0].Point.Position.Latitude,
                    result.Locations[0].Point.Position.Longitude
                    );
                    return coordinate;
                }
                else
                {
                    throw new ArgumentException("The location finder was unsuccessful and returned status " + result.Status.ToString());
                }
            }
            else
            {
                throw new Exception("No more Bing Requests left.");
            }
        }
    }
}
