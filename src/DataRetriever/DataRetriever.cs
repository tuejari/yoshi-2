using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS.Geocoding;

namespace YOSHI.DataRetrieverNS
{
    /// <summary>
    /// This class is responsible for retrieving data from GitHub. 
    /// </summary>
    public static class DataRetriever
    {
        public static readonly GitHubClient Client;
        // Default 24-hour operations with a basic Windows App, Non-profit, and Education key.
        // Info about rate limiting: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-api-best-practices

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

                // Set the GitHub Client and set the authentication token from GitHub for the GitHub REST API
                Client = new GitHubClient(new ProductHeaderValue("yoshi"));
                Credentials tokenAuth = new Credentials(githubAccessToken);
                Client.Credentials = tokenAuth;
            }
            catch (Exception e)
            {
                throw new Exception("Error during client initialization.", e);
            }
        }

        /// <summary>
        /// Method that retrieves all GitHub data that is needed to compute the validity of this repository. A repository
        /// is valid when it has at least 100 commits (all time), it has at least 10 members active in the last 90 days,
        /// it has at least 1 milestone (all time), and it has enough location data to compute dispersion. 
        /// </summary>
        /// <param name="community">The community for which we need to retrieve GitHub Data.</param>
        /// <returns>A boolean whether the community is valid or not.</returns>
        /// <exception cref="System.Exception">Thrown when something goes wrong while retrieving GitHub data.</exception>
        public static async Task<bool> RetrieveDataAndCheckValidity(Community community)
        {
            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;
            GitHubData data = community.Data;

            // Inspection of projects, requirements are at least 100 commits, at least 10 members, at least 50,000 LOC, must use milestones and issues
            try
            {
                await GitHubRequestsRemaining();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Bing Maps API requests remaining: {0}", GeoService.BingRequestsLeft);
                Console.ResetColor();

                // There must be at least 100 commits
                Console.WriteLine("Retrieving all commits...");
                IReadOnlyList<GitHubCommit> commits = await GitHubRateLimitHandler.Delegate(Client.Repository.Commit.GetAll, repoOwner, repoName, MaxSizeBatches);
                if (commits.Count < 100)
                {
                    return false;
                }

                Console.WriteLine("Filtering commits...");
                data.CommitsWithinTimeWindow = Filters.FilterCommits(commits);

                Console.WriteLine("Retrieve commit details..."); // Necessary to retrieve what files were changed each commit
                List<GitHubCommit> detailedCommitsWithinTimeWindow = new List<GitHubCommit>();
                foreach (GitHubCommit commit in data.CommitsWithinTimeWindow)
                {
                    GitHubCommit detailedCommit = await Client.Repository.Commit.Get(repoOwner, repoName, commit.Sha);
                    detailedCommitsWithinTimeWindow.Add(detailedCommit);
                }
                Console.WriteLine("Filtering detailed commits...");
                data.CommitsWithinTimeWindow = Filters.FilterDetailedCommits(detailedCommitsWithinTimeWindow);

                Console.WriteLine("Extracting usernames from commits...");
                data.MemberUsernames = Filters.ExtractUsernamesFromCommits(data.CommitsWithinTimeWindow);

                // There must be at least 10 members (active in the last 90 days)
                Console.WriteLine("Retrieving user data...");
                (data.Members, data.MemberUsernames) = await RetrieveMembers(data.MemberUsernames);
                if (data.MemberUsernames.Count < 10)
                {
                    return false;
                }
                Console.WriteLine("Filtering commits from non-members...");
                data.Commits = Filters.FilterAllCommits(commits, data.MemberUsernames);

                // There must be at least one milestone
                Console.WriteLine("Retrieving milestones...");
                data.Milestones = await GitHubRateLimitHandler.Delegate(Client.Issue.Milestone.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                if (data.Milestones.Count < 1)
                {
                    return false;
                }

                // There must be enough location data available to compute dispersion. TODO: Determine the threshold (maybe as percentage)
                Console.WriteLine("Retrieving addresses...");
                data.Addresses = await GeoService.RetrieveMemberAddresses(data.Members, repoName);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Bing Maps API requests remaining: {0}", GeoService.BingRequestsLeft);
                Console.ResetColor();
                if (data.Addresses.Count < 2)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while retrieving data from GitHub to check validity of repo: " + repoName, e);
            }

            return true;
        }

        /// <summary>
        /// Method that retrieves all GitHub data that is needed to compute only the structure metrics and modifies the 
        /// community data to store that information. It retrieves a mapping from a member to their followers, a mapping 
        /// from a member to their following, and a mapping from a member to their owned repositories.
        /// </summary>
        /// <param name="community">The community for which we need to retrieve GitHub Data.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        /// <exception cref="System.Exception">Thrown when something goes wrong while retrieving GitHub data.</exception>
        public static async Task RetrieveStructureData(Community community)
        {
            await GitHubRequestsRemaining();

            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;
            GitHubData data = community.Data;
            try
            {
                Console.WriteLine("Retrieving data per member...");
                (data.MapUserFollowers, data.MapUserFollowing, data.MapUserRepositories)
                    = await RetrieveDataPerMember(repoName, data.MemberUsernames);

                Console.WriteLine("Retrieving pull requests...");
                List<PullRequest> pullRequestsWithinWindow = await RetrievePullRequests(repoOwner, repoName, data.MemberUsernames);

                Console.WriteLine("Retrieving pull request comments within last 90 days per pull request...");
                data.MapPullReqsToComments = await RetrieveCommentsPerPullRequest(repoOwner, repoName, pullRequestsWithinWindow, data.MemberUsernames);
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while retrieving data from GitHub to compute structure of repo: " + repoName, e);
            }
        }

        /// <summary>
        /// Method that retrieves all GitHub data that is needed to compute all but structure metrics and modifies the 
        /// community data to store that information. 
        /// </summary>
        /// <param name="community">The community for which we need to retrieve GitHub Data.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        /// <exception cref="System.Exception">Thrown when something goes wrong while retrieving GitHub data.</exception>
        public static async Task RetrieveMiscellaneousData(Community community)
        {
            await GitHubRequestsRemaining();

            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;
            GitHubData data = community.Data;
            try
            {
                // A member is considered active if they made a commit in the last 30 days
                Console.WriteLine("Extracting active users...");
                data.ActiveMembers = Filters.ExtractUsernamesFromCommits(data.CommitsWithinTimeWindow, 30);

                Console.WriteLine("Retrieving commit comments...");
                IReadOnlyList<CommitComment> commitComments = await GitHubRateLimitHandler.Delegate(Client.Repository.Comment.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                data.CommitComments = Filters.FilterComments(commitComments, data.MemberUsernames);

                Console.WriteLine("Retrieving watchers...");
                IReadOnlyList<User> watchers = await GitHubRateLimitHandler.Delegate(Client.Activity.Watching.GetAllWatchers, repoOwner, repoName, MaxSizeBatches);
                data.Watchers = Filters.ExtractUsernamesFromUsers(watchers, data.MemberUsernames);

                Console.WriteLine("Retrieving stargazers...");
                IReadOnlyList<User> stargazers = await GitHubRateLimitHandler.Delegate(Client.Activity.Starring.GetAllStargazers, repoOwner, repoName, MaxSizeBatches);
                data.Stargazers = Filters.ExtractUsernamesFromUsers(stargazers, data.MemberUsernames);
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while retrieving data from GitHub", e);
            }

            await GitHubRequestsRemaining();
        }

        /// <summary>
        /// Retrieves the GitHub User information from a set of usernames. Since parameters cannot be modified in async
        /// methods, we return an extra variable without usernames that cause exceptions.
        /// </summary>
        /// <param name="usernames">A set of usernames to retrieve the GitHub data from.</param>
        /// <returns>A list of GitHub User information and an updated set of usernames, excluding all usernames that
        /// caused exceptions. </returns>
        private static async Task<(List<User>, HashSet<string>)> RetrieveMembers(HashSet<string> usernames)
        {
            List<User> members = new List<User>();
            HashSet<string> updatedUsernames = new HashSet<string>(); // A separate list to exclude usernames that cause exceptions
            foreach (string username in usernames)
            {
                try
                {
                    User user = await GitHubRateLimitHandler.Delegate(Client.User.Get, username);
                    members.Add(user);
                    updatedUsernames.Add(username);
                }
                catch
                {
                    // Skip the usernames that cause exceptions
                    continue;
                }
            }
            return (members, updatedUsernames);
        }

        /// <summary>
        /// For all repository members we retrieve their followers (i.e., who's following them) and following 
        /// (i.e., who they're following), and we retrieve the repositories they worked on.
        /// </summary>
        /// <param name="data">The data object of the community in which we store all retrieved GitHub data.</param>
        private static async Task<(
            Dictionary<string, HashSet<string>> mapUserFollowers,
            Dictionary<string, HashSet<string>> mapUserFollowing,
            Dictionary<string, HashSet<string>> mapUserRepositories)>
            RetrieveDataPerMember(string repoName, HashSet<string> memberUsernames)
        {
            Dictionary<string, HashSet<string>> mapUserFollowers = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> mapUserFollowing = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> mapUserRepositories = new Dictionary<string, HashSet<string>>();

            foreach (string username in memberUsernames)
            {
                // Get the given user's followers, limited to members that are also part of the current repository
                IReadOnlyList<User> followers = await GitHubRateLimitHandler.Delegate(Client.User.Followers.GetAll, username, MaxSizeBatches);
                HashSet<string> followersNames = Filters.ExtractUsernamesFromUsers(followers, memberUsernames);

                // Get the given user's users that they're following, limited to members that are also part of the current repository
                IReadOnlyList<User> following = await GitHubRateLimitHandler.Delegate(Client.User.Followers.GetAllFollowing, username, MaxSizeBatches);
                HashSet<string> followingNames = Filters.ExtractUsernamesFromUsers(following, memberUsernames);

                // TODO: Check whether these repositories are all repositories that the user has at least one commit to the main branch / gh-pages
                IReadOnlyList<Repository> repositories =
                    await GitHubRateLimitHandler.Delegate(Client.Repository.GetAllForUser, username, MaxSizeBatches);
                HashSet<string> repos = Filters.ExtractRepoNamesFromRepos(repositories, repoName);

                // Store all user data
                mapUserFollowers.Add(username, followersNames);
                mapUserFollowing.Add(username, followingNames);
                mapUserRepositories.Add(username, repos);
            }

            return (mapUserFollowers, mapUserFollowing, mapUserRepositories);
        }

        /// <summary>
        /// This method retrieves all pull requests for a repository. Filters all pull requests by non-committers, i.e., 
        /// users that are not considered members.
        /// </summary>
        /// <param name="repoOwner">Repository owner</param>
        /// <param name="repoName">Repository name</param>
        /// <returns>A list of pull requests.</returns>
        private static async Task<List<PullRequest>> RetrievePullRequests(string repoOwner, string repoName, HashSet<string> memberUsernames)
        {
            // We want all pull requests, since they often do not get closed correctly or closed at all, even if they're merged
            PullRequestRequest stateFilter = new PullRequestRequest { State = ItemStateFilter.All };
            IReadOnlyList<PullRequest> pullRequests =
                await GitHubRateLimitHandler.Delegate(Client.PullRequest.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);

            // Filter out all pull requests outside the time window
            Console.WriteLine("Filtering pull requests outside the time window...");
            List<PullRequest> pullRequestsWithinWindow = Filters.FilterPullRequests(pullRequests, memberUsernames);

            return pullRequestsWithinWindow;
        }

        /// <summary>
        /// Given a list of pull requests for a repository, this method retrieves the pull request review comments
        /// for each pull request and maps them in a dictionary. Filters all pull request comments by 
        /// non-committers, i.e., users that are not considered members.
        /// </summary>
        /// <param name="repoOwner">Repository owner</param>
        /// <param name="repoName">Repository name</param>
        /// <returns>A dictionary mapping pull requests to pull request review comments.</returns>
        private static async Task<Dictionary<PullRequest, List<PullRequestReviewComment>>> RetrieveCommentsPerPullRequest(
            string repoOwner, string repoName, List<PullRequest> pullRequests, HashSet<string> memberUsernames)
        {
            Dictionary<PullRequest, List<PullRequestReviewComment>> mapPullReqsToComments =
                new Dictionary<PullRequest, List<PullRequestReviewComment>>();

            // Map each pull request to its corresponding comments
            foreach (PullRequest pullRequest in pullRequests)
            {
                IReadOnlyList<PullRequestReviewComment> pullRequestComments =
                    await GitHubRateLimitHandler.Delegate(Client.PullRequest.ReviewComment.GetAll, repoOwner, repoName, pullRequest.Number, MaxSizeBatches);

                List<PullRequestReviewComment> filteredComments = Filters.FilterComments(pullRequestComments, memberUsernames);

                mapPullReqsToComments.Add(pullRequest, filteredComments);
            }

            return mapPullReqsToComments;
        }

        /// <summary>
        /// Method used to report on GitHub rate limits.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private static async Task GitHubRequestsRemaining()
        {
            ApiInfo apiInfo = Client.GetLastApiInfo();
            RateLimit rateLimit = apiInfo?.RateLimit;
            if (rateLimit == null)
            {
                MiscellaneousRateLimit miscellaneousRateLimit = await Client.Miscellaneous.GetRateLimits();
                rateLimit = miscellaneousRateLimit.Rate;
            }

            int? howManyRequestsDoIHaveLeftAfter = rateLimit?.Remaining;
            DateTimeOffset resetTime = rateLimit.Reset;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("GitHub API requests remaining: {0}, reset time: {1}", howManyRequestsDoIHaveLeftAfter, resetTime.DateTime.ToLocalTime().ToString());
            Console.ResetColor();
        }
    }
}
