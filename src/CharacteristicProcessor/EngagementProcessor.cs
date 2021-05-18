using Octokit;
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
            engagement.MedianActiveMember = MedianContains(data.ActiveMembers, data.MemberUsernames);
            engagement.MedianWatcher = MedianContains(data.Watchers, data.MemberUsernames);
            engagement.MedianStargazer = MedianContains(data.Stargazers, data.MemberUsernames);
            engagement.MedianCommitDistribution = MedianCommitDistribution(data.CommitsWithinTimeWindow, data.MemberUsernames);
            engagement.MedianFileCollabDistribution = MedianFileCollabDistribution(data.CommitsWithinTimeWindow, data.MemberUsernames);

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
        private static double MedianNrCommentsPerPullReq(Dictionary<PullRequest, List<PullRequestReviewComment>> mapPullReqsToComments)
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
        /// Method added for clarity. Given a list of stargazers and a list of members within the snapshot period, 
        /// compute the median stargazer status for all members. I.e., compute for each member whether they are a 

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commitsWithinWindow"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        private static double MedianCommitDistribution(List<GitHubCommit> commitsWithinWindow, HashSet<string> members)
        {
            Dictionary<string, int> nrCommitsPerUser = new Dictionary<string, int>();
            foreach (string member in members)
            {
                nrCommitsPerUser.Add(member, 0);
            }

            foreach (GitHubCommit commit in commitsWithinWindow)
            {
                // Note: all commits within the timewindow have already accessed committer, so we do not need to check
                // that committer is not null.
                nrCommitsPerUser[commit.Committer.Login]++;
                if (commit.Author != null && commit.Author.Login != null && Filters.CheckWithinTimeWindow(commit.Commit.Author.Date))
                {
                    nrCommitsPerUser[commit.Author.Login]++;
                }
            }

            return (double)Statistics.ComputeMedian(nrCommitsPerUser.Values.ToList()) / commitsWithinWindow.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commits"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        private static double MedianFileCollabDistribution(List<GitHubCommit> commits, HashSet<string> members)
        {
            // Extract the committers per file and the changed filenames
            (
                HashSet<(string, string)> changedFileNames,
                Dictionary<string, HashSet<string>> committersPerFile
            ) = ExtractCommittersPerFile(commits, members);

            // Extract largest non-overlapping sets of changed filenames
            Graph<string> filenamesGraph = new Graph<string>();
            filenamesGraph.AddEdges(changedFileNames);
            List<HashSet<string>> connectedComponents = filenamesGraph.GetConnectedComponents().ToList();

            // Merge files in the dictionary whose names got changed
            // Note: "ref" is used to indicate that committersPerFile may be modified by the method
            MergeKeysWithUpdatedNames(connectedComponents, ref committersPerFile);

            List<int> nrCommittersPerFile = committersPerFile.Values
                                                  .Select(set => set.Count())
                                                  .ToList();

            return Statistics.ComputeMedian(nrCommittersPerFile) / nrCommittersPerFile.Count;
        }

        /// <summary>
        /// Merge files in the dictionary whose names got changed
        /// </summary>
        /// <param name="updatedFilenames">A list of sets of updated filenames.</param>
        /// <param name="committersPerFile">A dictionary of committers per file.</param>
        private static void MergeKeysWithUpdatedNames(
            List<HashSet<string>> updatedFilenames,
            ref Dictionary<string, HashSet<string>> committersPerFile
        )
        {
            foreach (HashSet<string> set in updatedFilenames)
            {
                // Find the filename used in the dictionary. The first one returned from this set will be kept in the
                // dictionary
                // Note: not all filenames need to occur in the dictionary, sometimes older files (outside the 3-month
                // window) get their names changed (or relocated which causes their name to be changed)
                string nameUsedInDict = "";
                foreach (string name in set)
                {
                    if (committersPerFile.ContainsKey(name))
                    {
                        nameUsedInDict = name;
                        break;
                    }
                }

                // Remove the name from the set so we're left with the filenames that we want to remove from the dictionary
                if (nameUsedInDict != "")
                {
                    set.Remove(nameUsedInDict);
                }

                // Foreach filename that we want to remove from the dictionary, merge their values with the file that we
                // want to keep in the dictionary and remove it
                foreach (string name in set)
                {
                    if (committersPerFile.ContainsKey(name))
                    {
                        committersPerFile[nameUsedInDict].UnionWith(committersPerFile[name]);
                        committersPerFile.Remove(name);
                    }
                }
            }
        }

        /// <summary>
        /// Given a list of commits and a list of members, extract for each file the unique committers that have 
        /// modified that file, while keeping track of name changes.
        /// </summary>
        /// <param name="commits"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        private static (HashSet<(string, string)>, Dictionary<string, HashSet<string>>)
            ExtractCommittersPerFile(List<GitHubCommit> commits, HashSet<string> members)
        {
            HashSet<(string, string)> changedFileNames = new HashSet<(string, string)>(); // Used to keep track of changed filenames.
            Dictionary<string, HashSet<string>> committersPerFile = new Dictionary<string, HashSet<string>>(); // Used to keep track of the unique committers/authors per file. 

            foreach (GitHubCommit commit in commits)
            {
                // Loop over all files affected by the current commit
                foreach (GitHubCommitFile file in commit.Files)
                {
                    if (file.Filename != null)
                    {
                        // Keep track of changed filenames, will be resolved later
                        if (file.PreviousFileName != null)
                        {
                            changedFileNames.Add((file.Filename, file.PreviousFileName));
                        }

                        // Check if we previously saw this file, add as key to the dictionary, add the committer to its value
                        if (committersPerFile.ContainsKey(file.Filename))
                        {
                            committersPerFile[file.Filename].Add(commit.Committer.Login);
                        }
                        else
                        {
                            committersPerFile.Add(file.Filename, new HashSet<string> { commit.Committer.Login });
                        }

                        // Add the commit author to the current file's entry in the dictionary
                        if (commit.Author != null && commit.Author.Login != null && members.Contains(commit.Author.Login))
                        {
                            committersPerFile[file.Filename].Add(commit.Author.Login);
                        }
                    }
                }
            }

            return (changedFileNames, committersPerFile);
        }
    }
}