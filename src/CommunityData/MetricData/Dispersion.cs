using System;
using System.Collections.Generic;
using System.Text;

namespace YOSHI.CommunityData.MetricData
{
    /// <summary>
    /// This class is used to store values for metrics used to compute a community's dispersion.
    /// </summary>
    public class Dispersion
    {
        public float GeographicalDistanceMap { get; set; }
        public float HofstedeCulturalDistance { get; set; }
    }
}
