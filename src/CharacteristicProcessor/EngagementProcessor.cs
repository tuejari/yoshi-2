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
            Engagement engagement = community.Metrics.Engagement;
            //engagement.MedianNrPullReqComments;
            //engagement.MedianMonthlyPullCommitCommentsDistribution;
            //engagement.MedianActiveMember;
            //engagement.MedianWatcher;
            //engagement.MedianStargazer;
            //engagement.MedianCommitDistribution;
            //engagement.MedianFileCollabDistribution;

            community.Characteristics.Engagement =
                (float)(engagement.MedianNrPullReqComments + engagement.MedianMonthlyPullCommitCommentsDistribution
                + engagement.MedianActiveMember + engagement.MedianWatcher + engagement.MedianStargazer
                + engagement.MedianCommitDistribution + engagement.MedianFileCollabDistribution) / 7;
        }
    }
}