using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using YOSHI.CommunityData;
using YOSHI.CommunityData.MetricData;
using YOSHI.DataRetrieverNS;

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
            (community.Data.Contributors, community.Data.Collaborators, formality.MeanMembershipType, formality.MeanMembershipTypeOld) 
                = MeanMembershipType(community.Data.CommitsWithinTimeWindow, community.Data.MergedPullRequests, community.Data.MemberUsernames);
            formality.Milestones = community.Data.Milestones.Count;
            formality.Lifetime = ProjectLifetimeInDays(community.Data.Commits, community.Data.MemberUsernames);

            community.Characteristics.Formality = (float)formality.MeanMembershipType / (formality.Milestones / formality.Lifetime);
        }

        /// <summary>
        /// This method computes the average membership type from a list of members.
        /// </summary>
        /// <returns>A float denoting the average membership type.</returns>
        private static (int, int, float, float) MeanMembershipType(List<GitHubCommit> commits, List<PullRequest> mergedPullRequests, HashSet<string> memberUsernames)
        {
            // We transform the lists of contributors and collaborators to only the usernames, so it becomes easier
            // to compute the difference of two lists. 
            // NOTE: We mention that we use the commit committers and the pull request mergers as collaborators.
            // The list of commits includes the pull request merge commits. However, if this is done through GitHub's
            // web interface, these will be attributed to GitHub web-flow (https://github.com/web-flow).
            // Therefore we still parse the merged pull requests.
            HashSet<string> committers = new HashSet<string>();
            HashSet<string> authors = new HashSet<string>();

            foreach (GitHubCommit commit in commits)
            {
                if (Filters.ValidCommitterWithinTimeWindow(commit, memberUsernames))
                {
                    committers.Add(commit.Committer.Login);
                }

                if (Filters.ValidAuthorWithinTimeWindow(commit, memberUsernames))
                {
                    authors.Add(commit.Author.Login);
                }
            }

            foreach (PullRequest mergedPullRequest in mergedPullRequests)
            {
                if (mergedPullRequest.MergedBy != null && mergedPullRequest.MergedBy.Login != null && memberUsernames.Contains(mergedPullRequest.MergedBy.Login))
                {
                    committers.Add(mergedPullRequest.MergedBy.Login);
                }
            }

            HashSet<string> contributors = authors.Except(committers).ToHashSet();
            HashSet<string> collaborators = committers;

            if ((contributors.Count + collaborators.Count) != memberUsernames.Count)
            {
                throw new Exception("Found fewer or more contributors and collaborators than members");
            }

            float meanMembershipType = (float)(contributors.Count + collaborators.Count * 2) /
                (memberUsernames.Count);
            float meanMembershipTypeOld = (float)(contributors.Count) /
                (memberUsernames.Count);

            return (contributors.Count, collaborators.Count, meanMembershipType, meanMembershipTypeOld);
        }

        /// <summary>
        /// This method is used to compute the project lifetime in number of days, using the first commit and last 
        /// commit.
        /// </summary>
        /// <param name="commits">A list of commits from a repository.</param>
        /// <returns>The project lifetime in number of days.</returns>
        private static int ProjectLifetimeInDays(IReadOnlyList<GitHubCommit> commits, HashSet<string> memberUsernames)
        {
            // We use committer date instead of author date, since that's when the commit was last applied.
            // Source: https://stackoverflow.com/questions/18750808/difference-between-author-and-committer-in-git
            DateTimeOffset dateFirstCommit = DateTimeOffset.MaxValue;
            DateTimeOffset dateLastCommit = DateTimeOffset.MinValue;
            foreach (GitHubCommit commit in commits)
            {
                if (Filters.ValidCommitter(commit, memberUsernames))
                {
                    DateTimeOffset dateCurrentCommit = commit.Commit.Committer.Date;
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

                if (Filters.ValidAuthor(commit, memberUsernames))
                {
                    DateTimeOffset dateCurrentCommit = commit.Commit.Author.Date;
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
            }
            TimeSpan timespan = dateLastCommit - dateFirstCommit;
            return timespan.Days;
        }
    }
}