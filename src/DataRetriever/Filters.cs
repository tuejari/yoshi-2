﻿using Octokit;
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
        public static readonly DateTime StartDateTimeWindow;

        static Filters()
        {
            int days = 90; // snapshot period of 3 months (approximated using 90 days)
            // Note: Currently other periods are not supported.
            // Engagementprocessor uses hardcoded month thresholds of 30 and 60
            StartDateTimeWindow = EndDateTimeWindow.AddDays(-days).Date;
        }

        /// <summary>
        /// Extracts commits committed within the given time window (default 3 months, approximated using 90 days). 
        /// Checks that the commits have a committer.
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A list of commits that all were committed within the time window.</returns>
        public static List<GitHubCommit> ExtractCommitsWithinTimeWindow(IReadOnlyList<GitHubCommit> commits)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                if ((commit.Committer != null && commit.Committer.Login != null && CheckWithinTimeWindow(commit.Commit.Committer.Date))
                    || (commit.Author != null && commit.Author.Login != null && CheckWithinTimeWindow(commit.Commit.Author.Date)))
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
        public static List<GitHubCommit> FilterDetailedCommits(IReadOnlyList<GitHubCommit> commits, HashSet<string> memberUsernames)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                if ((ValidCommitterWithinTimeWindow(commit, memberUsernames)
                    || ValidAuthorWithinTimeWindow(commit, memberUsernames))
                    && commit.Files != null)
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
        /// <param name="memberUsernames">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of commits</returns>
        public static IReadOnlyList<GitHubCommit> FilterAllCommits(IReadOnlyList<GitHubCommit> commits, HashSet<string> memberUsernames)
        {
            // Get all commits in the last 90 days
            List<GitHubCommit> filteredCommits = new List<GitHubCommit>();
            foreach (GitHubCommit commit in commits)
            {
                // Note: filter out commits from today
                if ((ValidCommitter(commit, memberUsernames) || ValidAuthor(commit, memberUsernames))
                    && (commit.Commit.Committer.Date < EndDateTimeWindow || commit.Commit.Author.Date < EndDateTimeWindow))
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
                // Check that committer date also falls within the time window before adding the author in the list of members
                if (commit.Committer != null && commit.Committer.Login != null && CheckWithinTimeWindow(commit.Commit.Committer.Date, days))
                {
                    usernames.Add(commit.Committer.Login);
                }
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
        /// This method retrieves all User objects and usernames for all committers and commit authors in the last 90
        /// days. Note: It is possible that open pull request authors have commits on their own forks. These are not detected 
        /// as members as they have not yet made a contribution. 
        /// </summary>
        /// <param name="commits">A list of commits</param>
        /// <returns>A tuple containing a list of users and a list of usernames.</returns>
        public static HashSet<string> ExtractMembersFromCommits(List<GitHubCommit> commits, HashSet<string> memberUsernames, int days = 90)
        {
            // Get the user info of all members that have made at least one commit in the last 90 days
            HashSet<string> usernames = new HashSet<string>();
            foreach (GitHubCommit commit in commits)
            {
                // Check that committer date also falls within the time window before adding the author in the list of members
                if (commit.Committer != null && commit.Committer.Login != null
                    && memberUsernames.Contains(commit.Committer.Login) && CheckWithinTimeWindow(commit.Commit.Committer.Date, days))
                {
                    usernames.Add(commit.Committer.Login);
                }
                // Check that author date also falls within the time window before adding the author in the list of members
                if (commit.Author != null && commit.Author.Login != null
                    && memberUsernames.Contains(commit.Author.Login) && CheckWithinTimeWindow(commit.Commit.Author.Date, days))
                {
                    usernames.Add(commit.Author.Login);
                }
            }
            // TODO: Apply alias resolution
            return usernames;
        }

        /// <summary>
        /// Filter milestones from today. We only take all  milestones that were closed prior to today.
        /// </summary>
        /// <param name="milestones">A list of milestones.</param>
        /// <returns>A list of milestones that were last updated before today.</returns>
        public static IReadOnlyList<Milestone> FilterMilestones(IReadOnlyList<Milestone> milestones)
        {
            List<Milestone> filteredMilestones = new List<Milestone>();

            foreach (Milestone milestone in milestones)
            {
                // Remove closed milestones that were updated after the time window.
                if (milestone.UpdatedAt < EndDateTimeWindow)
                {
                    filteredMilestones.Add(milestone);
                }
            }

            return filteredMilestones;
        }

        /// <summary>
        /// Given a list of users, extracts a set of usernames. Also checks whether users are considered members within 
        /// the time period.
        /// </summary>
        /// <param name="users">The list of users that we want to extract the usernames from.</param>
        /// <param name="memberUsernames">The list of members within the time period.</param>
        /// <returns>A set of usernames</returns>
        public static HashSet<string> ExtractUsernamesFromUsers(IReadOnlyList<User> users, HashSet<string> memberUsernames)
        {
            HashSet<string> names = new HashSet<string>();
            foreach (User user in users)
            {
                if (user.Login != null && memberUsernames.Contains(user.Login))
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
        /// <param name="memberUsernames">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of pull requests</returns>
        public static List<PullRequest> FilterPullRequests(IReadOnlyList<PullRequest> pullRequests, HashSet<string> memberUsernames)
        {
            // Extract only the pull requests that fall within the 3-month time window (approximately 90 days)
            // Note: this cannot be added as a parameter in the GitHub API request.
            List<PullRequest> filteredPullRequests = new List<PullRequest>();
            foreach (PullRequest pullRequest in pullRequests)
            {
                // TODO: BUG: If the last update was today, it will be excluded, even if other parts were within the
                // 3-month time window
                // If we also check for createdAt within the 3-month window, we would still be able to exclude pull
                // requests where the created at is before the 3 month, update in the 3 months and last update today
                // Not sure how to deal with this yet. (Problem not just related to pull requests, also comments and
                // milestones). Need to check all UpdatedAt, CreatedAt, ClosedAt, MergedAt
                if (CheckWithinTimeWindow(pullRequest.UpdatedAt) && pullRequest.User != null
                    && pullRequest.User.Login != null && memberUsernames.Contains(pullRequest.User.Login))
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
        /// <param name="memberUsernames">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of pull request comments</returns>
        public static List<PullRequestReviewComment> FilterComments(IReadOnlyList<PullRequestReviewComment> comments, HashSet<string> memberUsernames)
        {
            // Filter out all comments that are not within the time window, do not have an author, or are not 
            // considered current members (i.e., have not committed in the last 90 days). 
            // Note: the 3 months period cannot be added as a parameter in the GitHub API request.
            List<PullRequestReviewComment> filteredComments = new List<PullRequestReviewComment>();
            foreach (PullRequestReviewComment comment in comments)
            {
                if (CheckWithinTimeWindow(comment.UpdatedAt) && comment.User != null && comment.User.Login != null && memberUsernames.Contains(comment.User.Login))
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
        /// <param name="memberUsernames">A set of usernames of those considered members.</param>
        /// <returns>A filtered list of commit comments</returns>
        public static List<CommitComment> FilterComments(IReadOnlyList<CommitComment> comments, HashSet<string> memberUsernames)
        {
            List<CommitComment> filteredComments = new List<CommitComment>();
            foreach (CommitComment comment in comments)
            {
                if (CheckWithinTimeWindow(comment.CreatedAt) && comment.User != null && comment.User.Login != null && memberUsernames.Contains(comment.User.Login))
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
        public static bool CheckWithinTimeWindow(DateTimeOffset dateTime, int days = 90)
        {
            if (dateTime == null)
            {
                return false;
            }

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

        /// <summary>
        /// Given a commit, check whether the committer is valid (i.e., the committer is not null, the committer's login
        /// is not null, and the committer is considered a member in the last 3 months).
        /// </summary>
        /// <param name="commit">The commit to check</param>
        /// <param name="memberUsernames">A set of members</param>
        /// <returns>Whether the committer of the given commit is valid</returns>
        public static bool ValidCommitter(GitHubCommit commit, HashSet<string> memberUsernames)
        {
            return commit.Committer != null
                && commit.Committer.Login != null
                && memberUsernames.Contains(commit.Committer.Login);
        }

        /// <summary>
        /// Given a commit, check whether the author is valid (i.e., the author is not null, the author's login
        /// is not null, and the author is considered a member in the last 3 months).
        /// </summary>
        /// <param name="commit">The commit to check</param>
        /// <param name="memberUsernames">A set of members</param>
        /// <returns>Whether the author of the given commit is valid</returns>
        public static bool ValidAuthor(GitHubCommit commit, HashSet<string> memberUsernames)
        {
            return commit.Author != null
                && commit.Author.Login != null
                && memberUsernames.Contains(commit.Author.Login);
        }

        /// <summary>
        /// Given a commit, check whether the committer is valid (i.e., the committer is not null, the committer's login
        /// is not null, the committer date is within the 3 month window, and the committer is considered a member in 
        /// the last 3 months).
        /// </summary>
        /// <param name="commit">The commit to check</param>
        /// <param name="memberUsernames">A set of members</param>
        /// <returns>Whether the committer of the given commit is valid</returns>
        public static bool ValidCommitterWithinTimeWindow(GitHubCommit commit, HashSet<string> memberUsernames)
        {
            return ValidCommitter(commit, memberUsernames) && CheckWithinTimeWindow(commit.Commit.Committer.Date);
        }

        /// <summary>
        /// Given a commit, check whether the author is valid (i.e., the author is not null, the author's login
        /// is not null, the author date is within the 3 month window, and the author is considered a member in 
        /// the last 3 months).
        /// </summary>
        /// <param name="commit">The commit to check</param>
        /// <param name="memberUsernames">A set of members</param>
        /// <returns>Whether the committer of the given commit is valid</returns>
        public static bool ValidAuthorWithinTimeWindow(GitHubCommit commit, HashSet<string> memberUsernames)
        {
            return ValidAuthor(commit, memberUsernames) && CheckWithinTimeWindow(commit.Commit.Author.Date);
        }
    }
}
