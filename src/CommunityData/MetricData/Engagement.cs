using Octokit;
using System.Collections.Generic;

namespace YOSHI.CommunityData.MetricData
{
    /// <summary>
    /// This class is used to store values for metrics used to compute a community's engagement level.
    /// </summary>
    public class Engagement
    {
        public Dictionary<User, float> ActiveMembers { get; set; }
        public Dictionary<User, float> Watchers { get; set; }
        public Dictionary<User, float> Stargazers { get; set; }
        public Dictionary<User, float> NrPullReqComments { get; set; }
        // NOTE: All distributions are monthly
        public Dictionary<User, float> FileCollabDistribution { get; set; }
        public Dictionary<User, float> CommitDistribution { get; set; }
        public Dictionary<User, float> PullReqCommitDistribution { get; set; }
    }
}
