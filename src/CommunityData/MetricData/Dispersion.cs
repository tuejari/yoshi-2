namespace YOSHI.CommunityData.MetricData
{
    /// <summary>
    /// This class is used to store values for metrics used to compute a community's dispersion.
    /// </summary>
    public class Dispersion
    {
        public double VarGeoDistance { get; set; }
        public double AvgGeoDistance { get; set; }
        // ---------------------------------------------------------
        public double OldVariancePdi { get; set; }
        public double OldVarianceIdv { get; set; }
        public double OldVarianceMas { get; set; }
        public double OldVarianceUai { get; set; }
        public double OldVarCulDistance { get; set; }
        public double OldAvgCulDispersion { get; set; }
        // ---------------------------------------------------------
        public double NewVariancePdi { get; set; }
        public double NewVarianceIdv { get; set; }
        public double NewVarianceMas { get; set; }
        public double NewVarianceUai { get; set; }
        public double NewVarCulDistance { get; set; }
        public double NewAvgCulDispersion { get; set; }
    }
}
