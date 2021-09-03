using Geocoding;
using Octokit;
using System.Collections.Generic;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data that was retrieved from GitHub. 
    /// </summary>
    public class Data
    {
        public List<Location> Coordinates { get; set; }
        public List<string> OldCountries { get; set; }
        public List<string> NewCountries { get; set; }
    }
}