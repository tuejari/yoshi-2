using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;

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
                data.Commits = await GitHubRateLimitHandler.Delegate(Client.Repository.Commit.GetAll, repoOwner, repoName, MaxSizeBatches);
                if (data.Commits.Count < 100)
                {
                    return false;
                }

                Console.WriteLine("Extracting commits within last 90 days...");
                data.CommitsWithinTimeWindow = ExtractCommitsWithinTimeWindow(data.Commits);

                // There must be at least 10 members (active in the last 90 days)
                Console.WriteLine("Retrieving all members...");
                (data.Members, data.MemberUsernames) = await GetAllMembers(data.CommitsWithinTimeWindow);
                if (data.MemberUsernames.Count < 10)
                {
                    return false;
                }

                // There must be at least one milestone
                Console.WriteLine("Retrieving milestones...");
                data.Milestones = await GitHubRateLimitHandler.Delegate(Client.Issue.Milestone.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                if (data.Milestones.Count < 1)
                {
                    return false;
                }

                // There must be enough location data available to compute dispersion. TODO: Determine the threshold (maybe as percentage)
                Console.WriteLine("Retrieving coordinates...");
                data.Coordinates = await GeoService.RetrieveMemberCoordinates(data.Members, repoName);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Bing Maps API requests remaining: {0}", GeoService.BingRequestsLeft);
                Console.ResetColor();
                if (data.Coordinates.Count < 2)
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
                    = await RetrieveDataPerMember(data.MemberUsernames);

                data.MapPullReqsToComments = await MapPullRequestComments(repoOwner, repoName, data.MemberUsernames);
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
                // NOTE: Commit comments commit_id == commit SHA
                data.CommitComments = await GitHubRateLimitHandler.Delegate(Client.Repository.Comment.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
                data.Watchers = await GitHubRateLimitHandler.Delegate(Client.Activity.Watching.GetAllWatchers, repoOwner, repoName, MaxSizeBatches);
                data.Stargazers = await GitHubRateLimitHandler.Delegate(Client.Activity.Starring.GetAllStargazers, repoOwner, repoName, MaxSizeBatches); // TODO: Check whether we should get those with timestamps or not
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while retrieving data from GitHub", e);
            }

            await GitHubRequestsRemaining();
        }

        /// <summary>
        /// Extracts commits committed within the time window of 3 months (approximated using 90 days).
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A list of commits that all were committed within the time window.</returns>
        private static List<GitHubCommit> ExtractCommitsWithinTimeWindow(IReadOnlyList<GitHubCommit> commits)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> commitsWithinWindow = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                try
                {
                    if (CheckWithinTimeWindow(commit.Commit.Committer.Date))
                    {
                        commitsWithinWindow.Add(commit);
                    }
                }
                catch
                {
                    // Skip the commits that cause exceptions
                    continue;
                }
            }
            return commitsWithinWindow;
        }

        /// <summary>
        /// This method retrieves all User objects and usernames for all committers and commit authors in the last 90
        /// days.
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A tuple containing a list of users and a list of usernames.</returns>
        private static async Task<(List<User>, HashSet<string>)> GetAllMembers(List<GitHubCommit> commits)
        {
            // Get the user info of all members that have made at least one commit in the last 90 days
            HashSet<string> usernames = new HashSet<string>();
            foreach (GitHubCommit commit in commits)
            {
                try
                {
                    if (commit.Committer != null && commit.Committer.Login != null)
                    {
                        usernames.Add(commit.Committer.Login);
                    }
                    // Check that author date also falls within the time window before adding the author in the list of members
                    if (commit.Author != null && commit.Author.Login != null && CheckWithinTimeWindow(commit.Commit.Author.Date))
                    {
                        usernames.Add(commit.Author.Login);
                    }
                }
                catch
                {
                    // Skip the commits that cause exceptions
                    continue;
                }
            }
            // TODO: Apply alias resolution
            List<User> members = new List<User>();
            HashSet<string> memberUsernames = new HashSet<string>(); // A separate list to exclude usernames that cause exceptions
            foreach (string username in usernames)
            {
                try
                {
                    User user = await GitHubRateLimitHandler.Delegate(Client.User.Get, username);
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
            Dictionary<string, IReadOnlyList<Repository>> mapUserRepositories)>
            RetrieveDataPerMember(HashSet<string> memberUsernames)
        {
            Dictionary<string, HashSet<string>> mapUserFollowers = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> mapUserFollowing = new Dictionary<string, HashSet<string>>();
            Dictionary<string, IReadOnlyList<Repository>> mapUserRepositories = new Dictionary<string, IReadOnlyList<Repository>>();

            foreach (string username in memberUsernames)
            {
                // Get the given user's followers
                IReadOnlyList<User> followers = await GitHubRateLimitHandler.Delegate(Client.User.Followers.GetAll, username, MaxSizeBatches);
                // Limit the following users to members that are also part of the current repository
                HashSet<string> followersNames = new HashSet<string>();
                foreach (User follower in followers)
                {
                    if (follower.Login != null && memberUsernames.Contains(follower.Login))
                    {
                        followersNames.Add(follower.Login);
                    }
                }

                // Get the given user's users that they're following
                IReadOnlyList<User> following = await GitHubRateLimitHandler.Delegate(Client.User.Followers.GetAllFollowing, username, MaxSizeBatches);
                // Limit the following users to members that are also part of the current repository
                HashSet<string> followingNames = new HashSet<string>();
                foreach (User followingUser in following)
                {
                    if (followingUser.Login != null && memberUsernames.Contains(followingUser.Login))
                    {
                        followingNames.Add(followingUser.Login);
                    }
                }

                // TODO: Check whether these repositories are all repositories that the user has at least one commit to the main branch / gh-pages
                IReadOnlyList<Repository> repositories =
                    await GitHubRateLimitHandler.Delegate(Client.Repository.GetAllForUser, username, MaxSizeBatches);

                // Store all user data
                mapUserFollowers.Add(username, followersNames);
                mapUserFollowing.Add(username, followingNames);
                mapUserRepositories.Add(username, repositories);
            }

            return (mapUserFollowers, mapUserFollowing, mapUserRepositories);
        }

        /// <summary>
        /// This method retrieves all pull requests for a repository, and then retrieves the pull request review comments
        /// for each pull request and maps them in a dictionary.
        /// </summary>
        /// <param name="repoOwner">Repository owner</param>
        /// <param name="repoName">Repository name</param>
        /// <returns>A dictionary mapping pull requests to pull request review comments.</returns>
        private static async Task<Dictionary<PullRequest, List<PullRequestReviewComment>>> MapPullRequestComments(
            string repoOwner, string repoName, HashSet<string> memberUsernames)
        {
            // We want all pull requests, since they often do not get closed correctly or closed at all, even if they're merged
            Console.WriteLine("Retrieving pull requests...");
            PullRequestRequest stateFilter = new PullRequestRequest { State = ItemStateFilter.All };
            IReadOnlyList<PullRequest> pullRequests =
                await GitHubRateLimitHandler.Delegate(Client.PullRequest.GetAllForRepository, repoOwner, repoName, MaxSizeBatches);
            List<PullRequest> pullRequestsWithinWindow = new List<PullRequest>();

            // Extract only the pull requests that fall within the 3-month time window (approximately 90 days)
            // Note: this cannot be added as a parameter in the GitHub API request.
            Console.WriteLine("Extracting pull requests within last 90 days...");
            foreach (PullRequest pullRequest in pullRequests)
            {
                if (CheckWithinTimeWindow(pullRequest.UpdatedAt))
                {
                    pullRequestsWithinWindow.Add(pullRequest);
                }
            }

            // Map each pull request to its corresponding comments
            Console.WriteLine("Retrieving pull request comments within last 90 days per pull request...");
            Dictionary<PullRequest, List<PullRequestReviewComment>> mapPullReqsToComments =
                new Dictionary<PullRequest, List<PullRequestReviewComment>>();
            foreach (PullRequest pullRequest in pullRequestsWithinWindow)
            {
                IReadOnlyList<PullRequestReviewComment> pullRequestComments =
                    await GitHubRateLimitHandler.Delegate(Client.PullRequest.ReviewComment.GetAll, repoOwner, repoName, pullRequest.Number, MaxSizeBatches);

                // Filter out all comments that are not within the time window, do not have a commit author, or are not 
                // considered current members (i.e., have not committed in the last 90 days). 
                // Note: the 3 months period cannot be added as a parameter in the GitHub API request.
                List<PullRequestReviewComment> pullReqCommentsWithinWindow = new List<PullRequestReviewComment>();
                foreach (PullRequestReviewComment comment in pullRequestComments)
                {
                    if (CheckWithinTimeWindow(comment.UpdatedAt) && comment.User != null && comment.User.Login != null && memberUsernames.Contains(comment.User.Login))
                    {
                        pullReqCommentsWithinWindow.Add(comment);
                    }
                }

                mapPullReqsToComments.Add(pullRequest, pullReqCommentsWithinWindow);
            }

            return mapPullReqsToComments;
        }

        /// <summary>
        /// A method that takes a DateTimeOffset object and checks whether it is within the 3 months (90 days) snapshot 
        /// window. This window ends at today's midnight time and starts at midnight 90 days prior.
        /// </summary>
        /// <param name="dateTime">A DateTimeOffset object</param>
        /// <returns>Whether the DateTimeOffset object falls within the time window.</returns>
        /// <exception cref="System.NullReferenceException">Thrown when the datetime parameter is null.</exception>
        private static bool CheckWithinTimeWindow(DateTimeOffset dateTime)
        {
            // We set the date time offset window for the 3 months earlier from now (approximated using 90 days)
            DateTime EndDate = new DateTimeOffset(DateTime.Today).Date;
            DateTime StartDate = EndDate.AddDays(-90).Date;
            try
            {
                DateTime date = dateTime.Date; // Extract the date from the datetime object
                return date >= StartDate && date < EndDate;
            }
            catch
            {
                throw;
            }
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
