using System;
using YOSHI.CommunityData;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for transforming the numeric values of community characteristics into community 
    /// patterns.
    /// </summary>
    public static class PatternProcessor
    {
        // The thresholds that will be used to compute the patterns for each community.
        private static readonly int th_geographic_distance = 4926;      // Kilometers
        //private static readonly float th_cultural_distance = 15.0F;     // Percentages
        private static readonly float th_formality_lvl_low = 0.1F;
        private static readonly float th_formality_lvl_high = 20F;
        private static readonly float th_engagement_lvl = 3.5F;
        //private static readonly float th_cohesion_lvl = 11.0F;
        private static readonly int th_longevity = 93;                  // Days

        /// <summary>
        /// This method implements the decision tree from the YOSHI paper <see cref="Program"/>.
        /// </summary>
        /// <param name="community">The community whose patterns should be computed.</param>
        public static void ComputePattern(Community community)
        {
            throw new NotImplementedException();
        }
    }
}
