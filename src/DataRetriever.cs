using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
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
        private static readonly GitHubClient Client;
        // Default 24-hour operations with a basic Windows App, Non-profit, and Education key.
        // Info about rate limiting: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-api-best-practices
        public static int BingRequestsLeft { get; set; } = 50000;

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
            // TODO: Test whether GetAll methods work or need pagination(?)
            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;
            GitHubData data = community.Data;

            try
            {
                data.Repo = await RateLimitDelegate(Client.Repository.Get, repoOwner, repoName);
                data.Contributors = await RateLimitDelegate(Client.Repository.GetAllContributors, repoOwner, repoName);
                data.Collaborators = await RateLimitDelegate(Client.Repository.Collaborator.GetAll, repoOwner, repoName);
                data.Milestones = await RateLimitDelegate(Client.Issue.Milestone.GetAllForRepository, repoOwner, repoName);
                data.Commits = await RateLimitDelegate(Client.Repository.Commit.GetAll, repoOwner, repoName);
                data.CommitComments = await RateLimitDelegate(Client.Repository.Comment.GetAllForRepository, repoOwner, repoName);
                data.PullReqComments = await RateLimitDelegate(Client.PullRequest.ReviewComment.GetAllForRepository, repoOwner, repoName);
                data.Watchers = await RateLimitDelegate(Client.Activity.Watching.GetAllWatchers, repoOwner, repoName);
                data.Stargazers = await RateLimitDelegate(Client.Activity.Starring.GetAllStargazers, repoOwner, repoName); // TODO: Check whether we should get those with timestamps or not
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
                    IReadOnlyList<User> followers = await RateLimitDelegate(Client.User.Followers.GetAll, username); // Lists the given user's followers
                    IReadOnlyList<User> following = await RateLimitDelegate(Client.User.Followers.GetAllFollowing, username); // Lists all users that the given user is following
                    IReadOnlyList<Repository> repositories = await RateLimitDelegate(Client.Repository.GetAllForUser, username);
                    try
                    {
                        GeoCoordinate coordinate = await GetLongitudeLatitude(collaborator.Location);
                        data.MapUserFollowers.Add(username, followers);
                        data.MapUserFollowing.Add(username, following);
                        data.MapUserRepositories.Add(username, repositories);
                        data.Coordinates.Add(coordinate);
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
                    IReadOnlyList<User> followers = await RateLimitDelegate(Client.User.Followers.GetAll, username); // Lists the given user's followers
                    IReadOnlyList<User> following = await RateLimitDelegate(Client.User.Followers.GetAllFollowing, username); // Lists all users that the given user is following
                    IReadOnlyList<Repository> repositories = await RateLimitDelegate(Client.Repository.GetAllForUser, username);
                    // The class RepositoryContributor does not contain the contributors location, so we retrieve
                    // them as a user too.
                    User user = await RateLimitDelegate(Client.User.Get, username);
                    try
                    {
                        GeoCoordinate coordinate = await GetLongitudeLatitude(user.Location);
                        data.MapUserFollowers.Add(username, followers);
                        data.MapUserFollowing.Add(username, following);
                        data.MapUserRepositories.Add(username, repositories);
                        contributorsAsUsers.Add(user);
                        data.Coordinates.Add(coordinate);
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
                }
            }
            data.Contributors = updatedContributors; // Removed all contributors without login
            data.ContributorsAsUsers = contributorsAsUsers; // Store the data of contributors as User class (more info)
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
            if (BingRequestsLeft > 10) // Give ourselves a small buffer to not go over the limit.
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

        // AUXILIARY: Methods used to delegate GitHub API calls and handling of rate limits. 

        /// <summary>
        /// This method is used to delegate the GitHub API requests. It handles the rate limit. 
        /// </summary>
        /// <typeparam name="T">The type that func will return.</typeparam>
        /// <param name="func">The function that we want to call.</param>
        /// <param name="repoOwner">The name of the repository owner, whose repository we want data from.</param>
        /// <param name="repoName">The name of the repository we want to get data from.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        /// <exception cref="System.Exception">Throws an exception if after 3 times of trying to retrieve data, 
        /// the data RateLimitExceededException still occurs, or if another exception is thrown.</exception>
        private async static Task<T> RateLimitDelegate<T>(Func<string, string, Task<T>> func, string repoOwner, string repoName)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Task<T> task = func(repoOwner, repoName);
                    return await task;
                }
                catch (RateLimitExceededException)
                {
                    // When we exceed the rate limit we check when the limit resets and wait until that time before we try 2 more times.
                    TimeSpan timespan = TimeSpan.FromHours(1);

                    ApiInfo apiInfo = Client.GetLastApiInfo();
                    RateLimit rateLimit = apiInfo?.RateLimit;
                    DateTimeOffset? whenDoesTheLimitReset = rateLimit?.Reset;
                    if (whenDoesTheLimitReset != null)
                    {
                        timespan = (DateTimeOffset)whenDoesTheLimitReset - DateTimeOffset.Now;
                        timespan = timespan.Add(TimeSpan.FromSeconds(30)); // Add 30 seconds to the timespan
                        Console.WriteLine("Waiting until: " + whenDoesTheLimitReset.ToString());
                    }
                    else
                    {
                        // If we don't know the reset time, we wait the default time of 1 hour
                        Console.WriteLine("Waiting until: " + DateTimeOffset.Now.AddHours(1));
                    }
                    Thread.Sleep(timespan); // Wait until the rate limit resets
                    Console.WriteLine("Done waiting for the rate limit reset, continuing now: " + DateTimeOffset.Now.ToString());
                }
            }
            throw new Exception("Failed too many times to retrieve GitHub data.");
        }

        /// <summary>
        /// This method is used to delegate the GitHub API requests. It handles the rate limit. 
        /// </summary>
        /// <typeparam name="T">The type that func will return.</typeparam>
        /// <param name="func">The function that we want to call.</param>
        /// <param name="repoOwner">The name of the repository owner, whose repository we want data from.</param>
        /// <param name="repoName">The name of the repository we want to get data from.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        /// <exception cref="System.Exception">Throws an exception if after 3 times of trying to retrieve data, 
        /// the data RateLimitExceededException still occurs, or if another exception is thrown.</exception>
        private async static Task<T> RateLimitDelegate<T>(Func<string, Task<T>> func, string username)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Task<T> task = func(username);
                    return await task;
                }
                catch (RateLimitExceededException)
                {
                    // When we exceed the rate limit we check when the limit resets and wait until that time before we try 2 more times.
                    TimeSpan timespan = TimeSpan.FromHours(1);

                    ApiInfo apiInfo = Client.GetLastApiInfo();
                    RateLimit rateLimit = apiInfo?.RateLimit;
                    DateTimeOffset? whenDoesTheLimitReset = rateLimit?.Reset;
                    if (whenDoesTheLimitReset != null)
                    {
                        timespan = (DateTimeOffset)whenDoesTheLimitReset - DateTimeOffset.Now;
                        timespan = timespan.Add(TimeSpan.FromSeconds(30)); // Add 30 seconds to the timespan
                        Console.WriteLine("Waiting until: " + whenDoesTheLimitReset.ToString());
                    }
                    else
                    {
                        // If we don't know the reset time, we wait the default time of 1 hour
                        Console.WriteLine("Waiting until: " + DateTimeOffset.Now.AddHours(1));
                    }
                    Thread.Sleep(timespan); // Wait until the rate limit resets
                    Console.WriteLine("Done waiting for the rate limit reset, continuing now: " + DateTimeOffset.Now.ToString());
                }
            }
            throw new Exception("Failed too many times to retrieve GitHub data.");
        }
    }
}
