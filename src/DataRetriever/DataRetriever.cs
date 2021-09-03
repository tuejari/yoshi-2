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

        public static async Task ExtractStats(Community community)
        {
            string repoName = community.RepoName;
            string repoOwner = community.RepoOwner;

            try
            {
                // Retrieve repository statistics and check whether or not they should be excluded according to our exclusion criteria
                Repository repo = await GitHubRateLimitHandler.Delegate(Client.Repository.Get, repoOwner, repoName);
                if (repo.Fork)
                {
                    Console.WriteLine("{0}, {1}, Fork", repoOwner, repoName);
                    return;
                }
                if (repo.Archived)
                {
                    Console.WriteLine("{0}, {1}, Archived", repoOwner, repoName);
                    return;
                }
                if (!repo.HasIssues)
                {
                    Console.WriteLine("{0}, {1}, No Issues", repoOwner, repoName);
                    return;
                }

                MilestoneRequest stateFilter = new MilestoneRequest { State = ItemStateFilter.Closed };
                IReadOnlyList<Milestone> milestones = await GitHubRateLimitHandler.Delegate(
                    Client.Issue.Milestone.GetAllForRepository, repoOwner, repoName, stateFilter, MaxSizeBatches);
                if (milestones.Count < 1)
                {
                    Console.WriteLine("{0}, {1}, No milestones", repoOwner, repoName);
                    return;
                }

                IReadOnlyList<RepositoryContributor> contributors = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.GetAllContributors, repoOwner, repoName);
                if (contributors.Count < 10)
                {
                    Console.WriteLine("{0}, {1}, Too few contributors: {2}", repoOwner, repoName, contributors.Count);
                    return;
                }

                CommitRequest commitRequest = new CommitRequest { Until = Filters.EndDateTimeWindow };
                IReadOnlyList<GitHubCommit> commits = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.Commit.GetAll, repoOwner, repoName, commitRequest, MaxSizeBatches);
                if (commits.Count < 100)
                {
                    Console.WriteLine("{0}, {1}, Too few commits: {2}", repoOwner, repoName, commits.Count);
                    return;
                }

                IReadOnlyList<RepositoryTag> tags = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.GetAllTags, repoOwner, repoName, MaxSizeBatches);
                int numTags = 0;
                foreach (RepositoryTag tag in tags)
                {
                    GitHubCommit cmt = await GitHubRateLimitHandler.Delegate(
                        Client.Repository.Commit.Get, repoOwner, repoName, tag.Commit.Sha);
                    if (cmt.Commit != null && cmt.Commit.Committer != null && cmt.Commit.Committer.Date < Filters.EndDateTimeWindow)
                    {
                        numTags++;
                    }
                }

                HashSet<string> members = new HashSet<string>();
                foreach (GitHubCommit cmt in commits)
                {
                    if (cmt.Committer != null && cmt.Committer.Login != null)
                    {
                        members.Add(cmt.Committer.Login);
                    }
                    if (cmt.Author != null && cmt.Author.Login != null)
                    {
                        members.Add(cmt.Author.Login);
                    }
                }
                // The following line was used to extract the characteristics for the communities analyzed by YOSHI. 
                //Console.WriteLine("{0}/{1}: {2}, {3}, {4}, {5}",
                //repoOwner, repoName, numTags, commits.Count, members.Count, repo.Language);

                Branch mainBranch = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.Branch.Get, repoOwner, repoName, repo.DefaultBranch);
                GitHubCommit commit = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.Commit.Get, repoOwner, repoName, mainBranch.Commit.Sha);
                if (commit.Commit.Committer.Date <= new DateTime(2021, 4, 13))
                {
                    Console.WriteLine("{0}, {1}, Too old latest commit: {2}", repoOwner, repoName, commit.Commit.Committer.Date);
                    return;
                }

                IReadOnlyList<Release> releases = await GitHubRateLimitHandler.Delegate(
                    Client.Repository.Release.GetAll, repoOwner, repoName, MaxSizeBatches);
                IReadOnlyList<User> watchers = await GitHubRateLimitHandler.Delegate(
                    Client.Activity.Watching.GetAllWatchers, repoOwner, repoName, MaxSizeBatches);

                Console.WriteLine(repo.Id.ToString() + ';' + repoOwner + ';' + repoName + ";;" + releases.Count.ToString() + ';'
                    + commits.Count.ToString() + ';' + contributors.Count.ToString() + ';' + milestones.Count.ToString() + ';'
                    + repo.Language.ToString() + ";;;" + repo.StargazersCount.ToString() + ';' + watchers.Count.ToString() + ';'
                    + repo.ForksCount.ToString() + ';' + repo.Size.ToString() + ";;" + commit.Commit.Committer.Date.ToString()
                    + ";https://github.com/" + repoOwner + '/' + repoName + ";;" + repo.Description);
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
