using Octokit;
using System;
using System.Collections.Generic;

namespace YOSHI.DataRetrieverNS
{
    /// <summary>
    /// Class responsible for filtering the GitHub data. It checks that everything is within the given time window 
    /// (default 90 days). It filters out all data about GitHub users that are not considered members.
    /// </summary>
    public static class Filters
    {
        public static readonly DateTime EndDateTimeWindow = new DateTimeOffset(DateTime.Today).Date;
        public static readonly DateTime StartDateTimeWindow = EndDateTimeWindow.AddDays(-90).Date;

        /// <summary>
        /// Extracts commits committed within the given time window (default 3 months, approximated using 90 days). 
        /// Checks that the commits have a committer.
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A list of commits that all were committed within the time window.</returns>
        public static List<GitHubCommit> FilterCommits(IReadOnlyList<GitHubCommit> commits)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                if (CheckWithinTimeWindow(commit.Commit.Committer.Date) && commit.Committer.Login != null)
                {
                    filteredCommits.Add(commit);
                }
            }
            return filteredCommits;
        }

        /// <summary>
        /// Extracts commits committed within the given time window (default 3 months, approximated using 90 days). 
        /// Checks that the commits have a committer and that the commit has information on what files were affected.
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A list of commits that all were committed within the time window.</returns>
        public static List<GitHubCommit> FilterDetailedCommits(IReadOnlyList<GitHubCommit> commits)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                if (CheckWithinTimeWindow(commit.Commit.Committer.Date) && commit.Committer.Login != null && commit.Files != null)
                {
                    filteredCommits.Add(commit);
                }
            }
            return filteredCommits;
        }

        /// <summary>
        /// Filter out all commits that do not have a committer, or are not considered current members (i.e., have not
        /// committed in the last 90 days).
        /// </summary>
        /// <param name="commits">A list of commits to filter</param>
        /// <param name="members">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of commits</returns>
        public static IReadOnlyList<GitHubCommit> FilterAllCommits(IReadOnlyList<GitHubCommit> commits, HashSet<string> members)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                if (commit.Committer != null && commit.Committer.Login != null && members.Contains(commit.Committer.Login))
                {
                    filteredCommits.Add(commit);
                }
            }
            return filteredCommits;
        }

        /// <summary>
        /// This method retrieves all User objects and usernames for all committers and commit authors in the last 90
        /// days. Note: It is possible that open pull request authors have commits on their own forks. These are not detected 
        /// as members as they have not yet made a contribution. 
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A tuple containing a list of users and a list of usernames.</returns>
        public static HashSet<string> ExtractUsernamesFromCommits(List<GitHubCommit> commits, int days = 90)
        {
            // Get the user info of all members that have made at least one commit in the last 90 days
            HashSet<string> usernames = new HashSet<string>();
            foreach (GitHubCommit commit in commits)
            {
                // Note: all commits in timewindow have already been filtered such that committers have usernames
                usernames.Add(commit.Committer.Login);
                // Check that author date also falls within the time window before adding the author in the list of members
                if (commit.Author != null && commit.Author.Login != null && CheckWithinTimeWindow(commit.Commit.Author.Date, days))
                {
                    usernames.Add(commit.Author.Login);
                }
            }
            // TODO: Apply alias resolution
            return usernames;
        }

        /// <summary>
        /// Given a list of users, extracts a set of usernames. Also checks whether users are considered members within 
        /// the time period.
        /// </summary>
        /// <param name="users">The list of users that we want to extract the usernames from.</param>
        /// <param name="members">The list of members within the time period.</param>
        /// <returns>A set of usernames</returns>
        public static HashSet<string> ExtractUsernamesFromUsers(IReadOnlyList<User> users, HashSet<string> members)
        {
            HashSet<string> names = new HashSet<string>();
            foreach (User user in users)
            {
                if (user.Login != null && members.Contains(user.Login))
                {
                    names.Add(user.Login);
                }
            }
            return names;
        }

        /// <summary>
        /// Filter out all pull requests that are not within the time window, do not have an author, or are not considered
        /// current members (i.e., have not committed in the last 90 days).
        /// </summary>
        /// <param name="pullRequests">A list of pull request to filter</param>
        /// <param name="members">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of pull requests</returns>
        public static List<PullRequest> FilterPullRequests(IReadOnlyList<PullRequest> pullRequests, HashSet<string> members)
        {
            // Extract only the pull requests that fall within the 3-month time window (approximately 90 days)
            // Note: this cannot be added as a parameter in the GitHub API request.
            List<PullRequest> filteredPullRequests = new List<PullRequest>();
            foreach (PullRequest pullRequest in pullRequests)
            {
                if (CheckWithinTimeWindow(pullRequest.UpdatedAt) && pullRequest.User != null
                    && pullRequest.User.Login != null && members.Contains(pullRequest.User.Login))
                {
                    filteredPullRequests.Add(pullRequest);
                }
            }
            return filteredPullRequests;
        }

        /// <summary>
        /// Filter out all comments that are not within the time window, do not have an author, or are not considered
        /// current members (i.e., have not committed in the last 90 days).
        /// </summary>
        /// <param name="comments">A list of pull request comments to filter</param>
        /// <param name="members">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of pull request comments</returns>
        public static List<PullRequestReviewComment> FilterComments(IReadOnlyList<PullRequestReviewComment> comments, HashSet<string> members)
        {
            // Filter out all comments that are not within the time window, do not have an author, or are not 
            // considered current members (i.e., have not committed in the last 90 days). 
            // Note: the 3 months period cannot be added as a parameter in the GitHub API request.
            List<PullRequestReviewComment> filteredComments = new List<PullRequestReviewComment>();
            foreach (PullRequestReviewComment comment in comments)
            {
                if (CheckWithinTimeWindow(comment.UpdatedAt) && comment.User != null && comment.User.Login != null && members.Contains(comment.User.Login))
                {
                    filteredComments.Add(comment);
                }
            }
            return filteredComments;
        }

        /// <summary>
        /// Filter out all comments that are not within the time window, do not have an author, or are not considered
        /// current members (i.e., have not committed in the last 90 days).
        /// </summary>
        /// <param name="comments">A list of commit comments to filter</param>
        /// <param name="members">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of commit comments</returns>
        public static List<CommitComment> FilterComments(IReadOnlyList<CommitComment> comments, HashSet<string> members)
        {
            // TODO: Atm a copy of filter comments for pull request review comments. Try and make a generic method.
            List<CommitComment> filteredComments = new List<CommitComment>();
            foreach (CommitComment comment in comments)
            {
                if (CheckWithinTimeWindow(comment.CreatedAt) && comment.User != null && comment.User.Login != null && members.Contains(comment.User.Login))
                {
                    filteredComments.Add(comment);
                }
            }
            return filteredComments;
        }

        /// <summary>
        /// A method that takes a DateTimeOffset object and checks whether it is within the specified time window x number 
        /// of days (Default: 3 months,  i.e., x = 90 days). This window ends at today's midnight time and starts at 
        /// midnight x days prior.
        /// </summary>
        /// <param name="dateTime">A DateTimeOffset object</param>
        /// <returns>Whether the DateTimeOffset object falls within the time window.</returns>
        /// <exception cref="System.NullReferenceException">Thrown when the datetime parameter is null.</exception>
        public static bool CheckWithinTimeWindow(DateTimeOffset dateTime, int days = 90)
        {
            // We set the date time offset window for the 3 months earlier from now (approximated using 90 days)
            DateTime startDate = EndDateTimeWindow.AddDays(-days).Date;
            DateTime date = dateTime.Date; // Extract the date from the datetime object
            return date >= startDate && date < EndDateTimeWindow;
        }

        /// <summary>
        /// Given a list of repositories, extract the names of the repositories, exclude the name of the current 
        /// repository.
        /// </summary>
        /// <param name="repositories">A list of repositories.</param>
        /// <param name="currentRepoName">The name of the repository we're currently processing.</param>
        /// <returns>A set of repository names excluding the current repository name.</returns>
        public static HashSet<string> ExtractRepoNamesFromRepos(IReadOnlyList<Repository> repositories, string currentRepoName)
        {
            HashSet<string> repoNames = new HashSet<string>();
            foreach (Repository repo in repositories)
            {
                if (repo.Name != currentRepoName)
                {
                    repoNames.Add(repo.Name);
                }
            }
            return repoNames;
        }
    }
}
