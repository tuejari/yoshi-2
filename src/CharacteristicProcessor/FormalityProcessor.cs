using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using YOSHI.CommunityData;
using YOSHI.CommunityData.MetricData;

namespace YOSHI.CharacteristicProcessorNS
{
    public static partial class CharacteristicProcessor
    {
        /// <summary>
        /// A method that computes several metrics used to measure community formality. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the formality.</param>
        private static void ComputeFormality(Community community)
        {
            Formality formality = community.Metrics.Formality;
            formality.MeanMembershipType = MeanMembershipType(community.Data.CommitsWithinTimeWindow, community.Data.MemberUsernames);
            formality.Milestones = community.Data.Milestones.Count;
            formality.Lifetime = ProjectLifetimeInDays(community.Data.Commits);

            community.Characteristics.Formality = (float)formality.MeanMembershipType / (formality.Milestones / formality.Lifetime);
        }

        /// <summary>
        /// This method computes the average membership type from a list of members.
        /// </summary>
        /// <returns>A float denoting the average membership type.</returns>
        private static float MeanMembershipType(List<GitHubCommit> commits, HashSet<string> memberUsernames)
        {
            // We transform the lists of contributors and collaborators to only the usernames, so it becomes easier
            // to compute the difference of two lists. 
            HashSet<string> committers = new HashSet<string>();
            HashSet<string> authors = new HashSet<string>();

            foreach (GitHubCommit commit in commits)
            {
                if (commit.Committer != null && commit.Committer.Login != null && memberUsernames.Contains(commit.Committer.Login))
                {
                    committers.Add(commit.Committer.Login);
                }
                if (commit.Author != null && commit.Author.Login != null && memberUsernames.Contains(commit.Author.Login))
                {
                    committers.Add(commit.Author.Login);
                }
            }
            HashSet<string> contributors = authors.Except(committers).ToHashSet();
            HashSet<string> collaborators = committers;

            if ((contributors.Count + collaborators.Count) != memberUsernames.Count)
            {
                throw new Exception("less/more contributors/collaborators than members");
            }

            float meanMembershipType = (float)(contributors.Count + collaborators.Count * 2) /
                (memberUsernames.Count);

            return meanMembershipType;
        }

        /// <summary>
        /// This method is used to compute the project lifetime in number of days, using the first commit and last 
        /// commit.
        /// </summary>
        /// <param name="commits">A list of commits from a repository.</param>
        /// <returns>The project lifetime in number of days.</returns>
        private static int ProjectLifetimeInDays(IReadOnlyList<GitHubCommit> commits)
        {
            // We use committer date instead of author date, since that's when the commit was last applied.
            // Source: https://stackoverflow.com/questions/18750808/difference-between-author-and-committer-in-git
            DateTime dateFirstCommit = DateTimeOffset.MaxValue.Date;
            DateTime dateLastCommit = DateTimeOffset.MinValue.Date;
            foreach (GitHubCommit commit in commits)
            {
                DateTime commitDate = commit.Commit.Committer.Date.Date;
                // If current earliest commit is later than current commit
                if (dateFirstCommit.CompareTo(commitDate) > 0)
                {
                    dateFirstCommit = commitDate;
                } // If current latest commit is earlier than current commit
                if (dateLastCommit.CompareTo(commitDate) < 0)
                {
                    dateLastCommit = commitDate;
                }
            }
            TimeSpan timespan = dateLastCommit - dateFirstCommit;
            return timespan.Days;
        }
    }
}