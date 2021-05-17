using Octokit;
using System.Collections.Generic;
using System.Linq;
using YOSHI.CommunityData;
using YOSHI.CommunityData.MetricData;

namespace YOSHI.CharacteristicProcessorNS
{
    public static partial class CharacteristicProcessor
    {
        /// <summary>
        /// A method that computes several metrics used to measure community engagement. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the engagement.</param>
        private static void ComputeEngagement(Community community)
        {
            GitHubData data = community.Data;
            Engagement engagement = community.Metrics.Engagement;
            engagement.MedianNrCommentsPerPullReq =
                MedianNrCommentsPerPullReq(data.MapPullReqsToComments, data.MemberUsernames);
            //engagement.MedianMonthlyPullCommitCommentsDistribution;
            engagement.MedianActiveMember = MedianActiveMember(data.CommitsWithinTimeWindow, data.MemberUsernames);
            engagement.MedianWatcher = MedianWatcher(data.Watchers, data.MemberUsernames);
            engagement.MedianStargazer = MedianStargazer(data.Stargazers, data.MemberUsernames);

            community.Characteristics.Engagement =
                (float)(engagement.MedianNrCommentsPerPullReq + engagement.MedianMonthlyPullCommitCommentsDistribution
                + engagement.MedianActiveMember + engagement.MedianWatcher + engagement.MedianStargazer
                + engagement.MedianCommitDistribution + engagement.MedianFileCollabDistribution) / 7;
        }

        /// <summary>
        /// Given a list pull requests and a list of members within the snapshot period, compute the median number of 
        /// pull request comments per pull request for each member. I.e., compute for each member the average number of 
        /// pull request comments per pull request, and compute the median. 
        /// </summary>
        /// <param name="mapPullReqsToComments">A mapping from pull requests to their corresponding comments.</param>
        /// <param name="members">A set of members within the last 90 days.</param>
        /// <returns>The median value of pull request review comments per member.</returns>
        private static double MedianNrCommentsPerPullReq(Dictionary<PullRequest, List<PullRequestReviewComment>> mapPullReqsToComments, HashSet<string> members)
        {
            // Compute the comments per pull request
            // Note: the pull requests and comments not from members and not within the snapshot period have been
            // filtered in the DataRetriever.
            List<int> commentsPerPullReq = mapPullReqsToComments.Values
                                                  .Select(list => list.Count())
                                                  .ToList();

            // From the comments per Pull Request, compute the median
            // Re-architecting Software Forges... "Finally, in average, we observed that the number of discussions,
            // comments or threads spreading from a thread or discussion is comprised between 0 or 1."
            return Statistics.ComputeMedian(commentsPerPullReq);
        }

        /// <summary>
        /// Given a list of commits and a list of members within the snapshot period, compute the median activity status
        /// for each member. I.e., compute for each member whether they are an active member (i.e., committed in the 
        /// last 30 days) (1) or not (0), and compute the median. 
        /// </summary>
        /// <param name="commits">A list of commits.</param>
        /// <param name="members">A set of members within the last 90 days.</param>
        /// <returns>The median value whether members are active. Can be 0, 0.5, 1.</returns>
        private static double MedianActiveMember(List<GitHubCommit> commits, HashSet<string> members)
        {
            // A member is considered active if they made a commit in the last 30 days
            HashSet<string> activeMembers = new HashSet<string>();
            foreach (GitHubCommit commit in commits)
            {
                // Check whether the commit is within the last 30 days
                if (Util.CheckWithinTimeWindow(commit.Commit.Committer.Date.Date, 30))
                {
                    if (commit.Committer != null && commit.Committer.Login != null && members.Contains(commit.Committer.Login))
                    {
                        activeMembers.Add(commit.Committer.Login);
                    }
                    if (commit.Author != null && commit.Author.Login != null && members.Contains(commit.Author.Login))
                    {
                        activeMembers.Add(commit.Author.Login);
                    }
                }
            }

            return MedianContains(activeMembers, members);
        }

        /// <summary>
        /// Method added for clarity. Given a list of watchers and a list of members within the snapshot period, compute
        /// the median watcher status for all members. I.e., compute for each member whether they are a watcher (1) or not (0),
        /// and compute the median. 
        /// </summary>
        /// <param name="watchers">A readonly list of watcher members.</param>
        /// <param name="members">A set of members within the last 90 days.</param>
        /// <returns>The median value whether members are also watchers. Can be 0, 0.5, 1.</returns>
        private static double MedianWatcher(IReadOnlyList<User> watchers, HashSet<string> members)
        {
            return MedianContains(Util.ConvertUsersToUsernames(watchers, members), members);
        }

        /// <summary>
        /// Method added for clarity. Given a list of stargazers and a list of members within the snapshot period, 
        /// compute the median stargazer status for all members. I.e., compute for each member whether they are a 
        /// stargazer (1) or not (0), and compute the median. 
        /// </summary>
        /// <param name="stargazers">A readonly list of stargazers members.</param>
        /// <param name="members">A set of members within the last 90 days.</param>
        /// <returns>The median value whether members are also stargazers. Can be 0, 0.5, 1.</returns>
        private static double MedianStargazer(IReadOnlyList<User> stargazers, HashSet<string> members)
        {
            return MedianContains(Util.ConvertUsersToUsernames(stargazers, members), members);
        }

        /// <summary>
        /// Given a set of users and a set of members active in the last 90 days, compute a list containing for each 
        /// member whether they are contained in the set of users (1) or not (0). Then compute the median of that list.
        /// </summary>
        /// <param name="users">A set of users.</param>
        /// <param name="members">A set of members.</param>
        /// <returns>The median value whether members occur in the set of users.</returns>
        private static double MedianContains(HashSet<string> users, HashSet<string> members)
        {
            List<int> userValues = new List<int>();
            foreach (string member in members)
            {
                // Check if the member occurs in the list of users
                if (users.Contains(member))
                {
                    userValues.Add(1);
                }
                else
                {
                    userValues.Add(0);
                }
            }
            return Statistics.ComputeMedian(userValues);
        }
    }
}