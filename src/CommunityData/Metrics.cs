using YOSHI.CommunityData.MetricData;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing metrics per community characteristic. 
    /// </summary>
    public class Metrics
    {
        public Dispersion Dispersion { get; set; }

        public Metrics()
        {
            this.Dispersion = new Dispersion();
        }
    }
}