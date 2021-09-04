using YOSHI.CommunityData.MetricData;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data. 
    /// We will use this class to store the community data in separate objects. 
    /// </summary>
    public class Community
    {
        public string RepoOwner { get; }
        public string RepoName { get; }
        public Data Data { get; }
        public Dispersion Dispersion { get; }

        public Community(string owner, string name)
        {
            this.RepoOwner = owner;
            this.RepoName = name;
            this.Data = new Data();
            this.Dispersion = new Dispersion();
        }
    }
}
