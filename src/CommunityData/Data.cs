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
        public List<User> Members { get; set; }
        public HashSet<string> MemberUsernames { get; set; }
    }
}