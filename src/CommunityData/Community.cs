using System;
using System.Collections.Generic;
using System.Text;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data. I.e., we will use this class to store the 
    /// community data in separate objects. 
    /// </summary>
    public class Community
    {
        public string RepoName { get; }
        public string RepoOwner { get; }
        public GitHubData Data { get; }
        public Metrics Metrics { get; }
        public Characteristics Characteristics { get; }
        public Pattern Pattern { get; set; }

        public Community(string name, string owner)
        {
            this.RepoName = name;
            this.RepoOwner = owner;
        }

    }
}
