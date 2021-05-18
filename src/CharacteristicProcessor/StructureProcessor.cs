using Octokit;
using System.Collections.Generic;
using System.Linq;
using YOSHI.CommunityData;

namespace YOSHI.CharacteristicProcessorNS
{
    public static partial class CharacteristicProcessor
    {
        /// <summary>
        /// A method that computes several metrics used to measure community structure and then decides whether a
        /// community exhibits a structure or not.
        /// </summary>
        /// <param name="community">The community for which we need to compute the structure.</param>
        public static void ComputeStructure(Community community)
        {
            GitHubData data = community.Data;
            // Note: we compute all connections between members to potentially obtain member graphs in the future to
            // check whether the structure 
            // TODO: Transform these mappings to a graph structure
            CommonProjectsConnections(data.MapUserRepositories, community.RepoName, community.Characteristics);
            FollowConnections(data.MapUserFollowers, data.MapUserFollowing, community.Characteristics);
            PullReqConnections(data.MapPullReqsToComments, data.MemberUsernames, community.Characteristics);
        }

        /// <summary>
        /// We compute the common projects connections between all users. 
        /// </summary>
        /// <param name="mapUserRepositories">A mapping from usernames to the repositories that they worked on.</param>
        /// <returns>A mapping for each members to a set of other members who worked on a common repository.</returns>
        private static Dictionary<string, HashSet<string>> CommonProjectsConnections(
            Dictionary<string, HashSet<string>> mapUserRepositories,
            string repoName,
            Characteristics characteristics)
        {
            // Find common projects by comparing the names of repositories they worked on
            Dictionary<string, HashSet<string>> commonProjectConnections = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, HashSet<string>> firstUser in mapUserRepositories)
            {
                foreach (KeyValuePair<string, HashSet<string>> secondUser in mapUserRepositories)
                {
                    if (firstUser.Key != secondUser.Key)
                    {
                        // We compute the intersections of the list of repositories and then count the number of items
                        IEnumerable<string> commonProjects = firstUser.Value.Intersect(secondUser.Value);
                        if (commonProjects.Count() > 0)
                        {
                            // Two members have a common repository to which they are contributing, except for the 
                            // currently analyzed repository. We set this community's structure to true.
                            characteristics.Structure = true;
                        }
                    }
                }
            }

            return commonProjectConnections;
        }

        /// <summary>
        /// This method computes the follower/following connections between each of the members,
        /// but its result does not distinguish between followers and following. 
        /// </summary>
        /// <param name="mapUserFollowers">A mapping for each username to a list of the users followers.</param>
        /// <param name="mapUserFollowing">A mapping for each username to a list of the users that they themselves 
        /// follow.</param>
        /// <returns>A mapping for each username to a combined set of followers and following from which the names 
        /// have been extracted.</returns>
        private static Dictionary<string, HashSet<string>> FollowConnections(
            Dictionary<string, HashSet<string>> mapUserFollowers,
            Dictionary<string, HashSet<string>> mapUserFollowing,
            Characteristics characteristics)
        {
            Dictionary<string, HashSet<string>> followConnections = new Dictionary<string, HashSet<string>>();

            // Obtain a mapping from all users (usernames) to the names of the followers and following
            foreach (string user in mapUserFollowers.Keys)
            {
                followConnections.Add(user, new HashSet<string>(mapUserFollowers[user].Union(mapUserFollowing[user])));
                if (followConnections[user].Count() > 0)
                {
                    // Two members have a follower/following relation. We set this community's structure to true.
                    characteristics.Structure = true;
                }
            }

            return followConnections;
        }

        /// <summary>
        /// Computes the connections between pull request authors and pull request commenters.
        /// </summary>
        /// <param name="mapPullReqsToComments">A mapping from each pull request to their pull request review comments.</param>
        /// <returns>A mapping for each user to all other users that they're connected to through pull requests.</returns>
        private static Dictionary<string, HashSet<string>> PullReqConnections(
            Dictionary<PullRequest, List<PullRequestReviewComment>> mapPullReqsToComments,
            HashSet<string> members,
            Characteristics characteristics)
        {
            Dictionary<string, HashSet<string>> pullReqConnections = new Dictionary<string, HashSet<string>>();
            // Initialize dictionary for every member to an empty set
            foreach (string member in members)
            {
                pullReqConnections.Add(member, new HashSet<string>());
            }

            // Add the connections for each pull request commenter and author
            foreach (KeyValuePair<PullRequest, List<PullRequestReviewComment>> mapPullReqToComments in mapPullReqsToComments)
            {
                string pullReqAuthor = mapPullReqToComments.Key.User.Login;
                // Make sure that the pull request author is also a member
                // (i.e., whether they committed to this repository at least once)
                if (pullReqAuthor != null && members.Contains(pullReqAuthor))
                {
                    foreach (PullRequestReviewComment comment in mapPullReqToComments.Value)
                    {
                        string pullReqCommenter = comment.User.Login;
                        // Make sure that the pull request commenter is also a member
                        // (i.e., whether they committed to this repository at least once)
                        if (pullReqCommenter != null && members.Contains(pullReqCommenter))
                        {
                            pullReqConnections[pullReqCommenter].Add(pullReqAuthor);
                            pullReqConnections[pullReqAuthor].Add(pullReqCommenter);

                            // Two members have had a recent pull request interaction. We set this community's structure
                            // to true.
                            characteristics.Structure = true;
                        }
                    }
                }
            }

            return pullReqConnections;
        }
    }
}