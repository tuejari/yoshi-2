using Octokit;
using System.Collections.Generic;

namespace YOSHI.CommunityData.MetricData
{
    /// <summary>
    /// This class is used to store values for metrics used to compute a community's engagement level.
    /// </summary>
    public class Engagement
    {
        public float MedianNrPullReqComments { get; set; }
        public float MedianMonthlyPullCommitCommentsDistribution { get; set; }
        public float MedianActiveMember { get; set; }
        public float MedianWatcher { get; set; }
        public float MedianStargazer { get; set; }
        public float MedianCommitDistribution { get; set; }
        public float MedianFileCollabDistribution { get; set; }
    }
}
