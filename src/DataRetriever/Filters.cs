using Octokit;
using System;
using System.Collections.Generic;
using YOSHI.CommunityData;

namespace YOSHI.DataRetrieverNS
{
    /// <summary>
    /// Class responsible for filtering the GitHub data. It checks that everything is within the given time window 
    /// (default 90 days + today). It filters out all data about GitHub users that are not considered members.
    /// </summary>
    public static class Filters
    {
        public static DateTimeOffset EndDateTimeWindow { get; private set; }
        public static DateTimeOffset StartDateTimeWindow { get; private set; }

        public static void SetTimeWindow(DateTimeOffset endDateTimeWindow)
        {
            int days = 90; // snapshot period of 3 months (approximated using 90 days)
            // Note: Currently other length periods are not supported.
            // Engagementprocessor uses hardcoded month thresholds of 30 and 60
            EndDateTimeWindow = endDateTimeWindow;
            StartDateTimeWindow = EndDateTimeWindow.AddDays(-days);            
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
        /// A method that takes a DateTimeOffset object and checks whether it is within the specified time window x number 
        /// of days (Default: 3 months,  i.e., x = 90 days). This window ends at the specified end of the time window and 
        /// starts at midnight x days prior.
        /// </summary>
        /// <param name="dateTime">A DateTimeOffset object</param>
        /// <returns>Whether the DateTimeOffset object falls within the time window.</returns>
        public static bool CheckWithinTimeWindow(DateTimeOffset? dateTime, int days = 90)
        {
            if (dateTime == null)
            {
                return false;
            }

            // We set the date time offset window for the 3 months earlier from now (approximated using 90 days)
            DateTimeOffset startDate = EndDateTimeWindow.AddDays(-days);
            return dateTime >= startDate && dateTime <= EndDateTimeWindow;
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
