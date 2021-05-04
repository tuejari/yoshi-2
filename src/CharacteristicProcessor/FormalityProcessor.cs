using Octokit;
using System;
using System.Collections.Generic;
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
            float meanMembershipType = 0F;

            // TODO: Use another way to determine membership types, because we cannot retrieve collaborators and we 
            // cannot retrieve all contributors (which prevents us from applying alias resolution)

            // We transform the lists of contributors and collaborators to only the usernames, so it becomes easier
            // to compute the difference of two lists. 
            //List<string> contributorNames = new List<string>();
            //List<string> collaboratorNames = new List<string>();

            //foreach (RepositoryContributor contributor in contributors)
            //{
            //    if (contributor.Login != null)
            //    {
            //        contributorNames.Add(contributor.Login);
            //    }
            //}

            //foreach (User collaborator in collaborators)
            //{
            //    if (collaborator.Login != null)
            //    {
            //        collaboratorNames.Add(collaborator.Login);
            //    }
            //}

            //// We remove collaborators that are also marked as contributors from the list of contributors
            //// (collaborators count stronger due to more permissions)
            //List<string> cleanedContributorNames = contributorNames.Except(collaboratorNames).ToList();
            //float meanMembershipType = (float)(cleanedContributorNames.Count + collaboratorNames.Count * 2) /
            //    (cleanedContributorNames.Count + collaboratorNames.Count);

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
            DateTimeOffset dateFirstCommit = DateTimeOffset.MaxValue;
            DateTimeOffset dateLastCommit = DateTimeOffset.MinValue;
            foreach (GitHubCommit commit in commits)
            {
                DateTimeOffset commitDate = commit.Commit.Committer.Date;
                // If current evaluated commit is earlier than previous earliest commit
                if (dateFirstCommit.CompareTo(commitDate) < 0)
                {
                    dateFirstCommit = commitDate;
                } // If current evaluated commit is later than previous last commit 
                else if (dateLastCommit.CompareTo(commitDate) > 0)
                {
                    dateLastCommit = commitDate;
                }
            }
            TimeSpan timespan = dateLastCommit - dateFirstCommit;
            return timespan.Days;
        }
    }
}