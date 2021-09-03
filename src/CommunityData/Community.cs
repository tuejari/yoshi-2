namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data. I.e., we will use this class to store the 
    /// community data in separate objects. 
    /// </summary>
    public class Community
    {
        public string RepoOwner { get; }
        public string RepoName { get; }

        public Community(string owner, string name)
        {
            this.RepoOwner = owner;
            this.RepoName = name;
        }
    }
}
