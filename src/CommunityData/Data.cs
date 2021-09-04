using Geocoding;
using System.Collections.Generic;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data that 
    /// was retrieved from GitHub. 
    /// </summary>
    public class Data
    {
        public List<Location> Coordinates { get; set; }
        // This variables stores the set of countries from members that are also
        // included in the *old* set of Hofstede indices
        public List<string> OldCountries { get; set; }
        // This variables stores the set of countries from members that are also
        // included in the *new* set of Hofstede indices
        public List<string> NewCountries { get; set; }
    }
}