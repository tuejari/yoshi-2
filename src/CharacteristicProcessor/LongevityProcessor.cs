using Octokit;
using System;
using System.Collections.Generic;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS;

namespace YOSHI.CharacteristicProcessorNS
{
    public static partial class CharacteristicProcessor
    {

        /// <summary>
        /// A method that computes several metrics used to measure community longevity. It modifies the given community. 
        /// </summary>
        /// <param name="community">The community for which we need to compute the longevity.</param>
        public static void ComputeLongevity(Community community)
        {
            community.Metrics.Longevity.MeanCommitterLongevity = MeanCommitterLongevity(community.Data.Commits, community.Data.MemberUsernames);

            community.Characteristics.Longevity = community.Metrics.Longevity.MeanCommitterLongevity;
        }

        /// <summary>
        /// This method is used to compute the average committer longevity of all members active in the past 3 months. 
        /// </summary>
        /// <param name="commits">A list of commits for the current repository.</param>
        /// <param name="memberUsernames">A list of usernames of all members.</param>
        /// <returns>The average committer longevity.</returns>
        private static float MeanCommitterLongevity(IReadOnlyList<GitHubCommit> commits, HashSet<string> memberUsernames)
        {
            // We group the list of commits' datetimes per committer
            Dictionary<string, List<DateTime>> mapUserCommitDate = new Dictionary<string, List<DateTime>>();
            foreach (GitHubCommit commit in commits)
            {
                if (Filters.ValidCommitter(commit, memberUsernames))
                {
                    string committer = commit.Committer.Login;
                    if (!mapUserCommitDate.ContainsKey(committer))
                    {
                        mapUserCommitDate.Add(committer, new List<DateTime>());
                    }
                    mapUserCommitDate[committer].Add(commit.Commit.Committer.Date.Date);
                }

                if (Filters.ValidAuthor(commit, memberUsernames))
                {
                    string author = commit.Author.Login;
                    if (!mapUserCommitDate.ContainsKey(author))
                    {
                        mapUserCommitDate.Add(author, new List<DateTime>());
                    }
                    mapUserCommitDate[author].Add(commit.Commit.Author.Date.Date);
                }
            }

            int totalCommitterLongevityInDays = 0;
            // For each committer, we compute the dates of their first- and last commit.
            foreach (KeyValuePair<string, List<DateTime>> userCommitDate in mapUserCommitDate)
            {
                // We use committer date instead of author date, since that's when the commit was last applied.
                // Source: https://stackoverflow.com/questions/18750808/difference-between-author-and-committer-in-git
                // NOTE: this limits the metric, as we do not compute the longevity for each member.
                DateTime dateFirstCommit = DateTimeOffset.MaxValue.Date;
                DateTime dateLastCommit = DateTimeOffset.MinValue.Date;
                foreach (DateTime dateCurrentCommit in userCommitDate.Value)
                {
                    // If current earliest commit is later than current commit
                    if (dateFirstCommit.CompareTo(dateCurrentCommit) > 0)
                    {
                        dateFirstCommit = dateCurrentCommit;
                    }
                    // If current latest commit is earlier than current commit
                    if (dateLastCommit.CompareTo(dateCurrentCommit) < 0)
                    {
                        dateLastCommit = dateCurrentCommit;
                    }
                }
                // Add the difference between committers first and last commits to the total commit longevity
                totalCommitterLongevityInDays += (dateLastCommit - dateFirstCommit).Days;
            }
            float meanCommitterLongevity = (float)totalCommitterLongevityInDays / mapUserCommitDate.Count;
            return meanCommitterLongevity;
        }
    }
}