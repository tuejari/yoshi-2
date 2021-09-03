using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
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
                string githubAccessToken = Environment.GetEnvironmentVariable("YOSHI_GitHubAccessToken");

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

        public static async Task ExtractMailsUsingThirdQuartile(Community community)
        {
            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;

            try
            {
                // Retrieve all commits until the end date of the time window
                CommitRequest commitRequest = new CommitRequest { Until = Filters.EndDateTimeWindow };
                IReadOnlyList<GitHubCommit> commits = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.Commit.GetAll, 
                    repoOwner, 
                    repoName, 
                    commitRequest, 
                    MaxSizeBatches);

                List<GitHubCommit> commitsWithinTimeWindow = Filters.ExtractCommitsWithinTimeWindow(commits);

                // Determine the members active in the three month time period
                HashSet<string> tempMembers = Filters.ExtractUsernamesFromCommits(commitsWithinTimeWindow);
                (List<User> members, HashSet<string> memberUsernames) = await RetrieveMembers(tempMembers);

                // Calculate the number of commits per member
                // (over the entire life span until the end date of the analysis period)
                Dictionary<string, int> commitsPerMember = new Dictionary<string, int>();

                foreach (GitHubCommit c in commits)
                {
                    if (c.Committer != null && c.Committer.Login != null)
                    {
                        string committer = c.Committer.Login;
                        if (!commitsPerMember.ContainsKey(committer))
                        {
                            commitsPerMember.Add(committer, 0);
                        }

                        commitsPerMember[committer]++;
                    }
                    // Only count authored commits if they are not the committer too
                    // This prevents double counting of the same commit
                    if (c.Author != null && c.Author.Login != null && 
                        (c.Committer == null || c.Author.Login != c.Committer.Login))
                    {
                        string author = c.Author.Login ?? "";
                        if (!commitsPerMember.ContainsKey(author))
                        {
                            commitsPerMember.Add(author, 0);
                        }

                        commitsPerMember[author]++;
                    }
                }

                List<int> commitsDistribution = commitsPerMember.Values.ToList();
                commitsDistribution.Sort((a, b) => a.CompareTo(b)); // Ascending sort

                List<double> doubles = commitsDistribution.Select<int, double>(i => i).ToList();
                (double q1, double q2, double q3) = Statistics.Quartiles(doubles.ToArray());

                Console.WriteLine("Quartiles - q1: {0}, q2: {1}, q3: {2}", q1, q2, q3);

                // Set of usernames of all developers having a number of commits higher than the third quartile
                HashSet<string> usernames = new HashSet<string>(); 

                foreach (KeyValuePair<string, int> membersCommits in commitsPerMember)
                {
                    if (membersCommits.Value > q3)
                    {
                        usernames.Add(membersCommits.Key);
                    }
                }

                // Report the members who have a public email, have committed in the
                // analysis period and have a number of commits higher than the third quartile.
                foreach (User member in members)
                {
                    if (usernames.Contains(member.Login) && member.Email != null)
                    {
                        Console.WriteLine("{0},{1},{2},{3},{4}", repoOwner, repoName, 
                            member.Login, commitsPerMember[member.Login], member.Email);
                    }
                }
            }
            catch
            {
                throw;
            }
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
            HashSet<string> bots = new HashSet<string>();
            HashSet<string> organizations = new HashSet<string>();
            foreach (string username in usernames)
            {
                try
                {
                    // Snapshot at time of retrieval, there is no way to retrieve users information from a past time
                    User user = await GitHubRateLimitHandler.Delegate(Client.User.Get, username);

                    // Exclude organizations and bots
                    // Note: not all bots/organizations have the correct accounttype. We are bound to let through some
                    // bots/organizations this way, but it is better than nothing.
                    if (user.Type == AccountType.User)
                    {
                        members.Add(user);
                        updatedUsernames.Add(username);
                    }
                    else
                    {
                        if (user.Type == AccountType.Bot)
                        {
                            bots.Add(user.Login);
                        }
                        else // organization
                        {
                            organizations.Add(user.Login);
                        }
                    }
                }
                catch
                {
                    // Skip the usernames that cause exceptions
                    continue;
                }
            }

            return (members, updatedUsernames);
        }
    }
}
