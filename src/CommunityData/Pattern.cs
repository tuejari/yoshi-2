namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing community patterns. 
    /// </summary>
    public class Pattern
    {
        public bool SocialNetwork { get; set; } = false;
        public bool FormalGroup { get; set; } = false;
        public bool ProjectTeam { get; set; } = false;
        public bool WorkGroup { get; set; } = false;
        public bool NetworkOfPractice { get; set; } = false;
        public bool InformalCommunity { get; set; } = false;
        public bool FormalNetwork { get; set; } = false;
        public bool InformalNetwork { get; set; } = false;
        public bool CommunityOfPractice { get; set; } = false;
    }
}