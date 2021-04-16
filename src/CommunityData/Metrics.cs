using YOSHI.CommunityData.MetricData;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing metrics per community characteristic. 
    /// </summary>
    public class Metrics
    {
        public Structure Structure { get; set; }
        public Dispersion Dispersion { get; set; }
        public Formality Formality { get; set; }
        public Cohesion Cohesion { get; set; }
        public Longevity Longevity { get; set; }
        public Engagement Engagement { get; set; }
    }
}