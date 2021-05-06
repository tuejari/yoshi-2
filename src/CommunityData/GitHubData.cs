using Octokit;
using System.Collections.Generic;
using YOSHI.DataRetrieverNS.Geocoding;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data that was retrieved from GitHub. 
    /// </summary>
    public class GitHubData
    {
        public List<User> Members { get; set; }
        public HashSet<string> MemberUsernames { get; set; }
        // Followers and following are limited to users that also worked on this repository
        public Dictionary<string, HashSet<string>> MapUserFollowers { get; set; }
        public Dictionary<string, HashSet<string>> MapUserFollowing { get; set; }
        public Dictionary<string, IReadOnlyList<Repository>> MapUserRepositories { get; set; }

        public IReadOnlyList<Milestone> Milestones { get; set; }
        public IReadOnlyList<GitHubCommit> Commits { get; set; }
        public List<GitHubCommit> CommitsWithinTimeWindow { get; set; }
        public IReadOnlyList<CommitComment> CommitComments { get; set; }
        public Dictionary<PullRequest, List<PullRequestReviewComment>> MapPullReqsToComments { get; set; }
        // Regarding the difference between Watchers and Stargazers:
        // https://developer.github.com/changes/2012-09-05-watcher-api/
        // Watchers/Subscribers are users watching the repository. Watching a repository registers the user to receive
        // notifications on new discussions, as well as events in the user's activity feed.
        // Stargazers are users starring the repository. Repository starring is a feature that lets users bookmark
        // repositories. Stars are shown next to repositories to show an approximate level of interest. Stars have no
        // effect on notifications or the activity feed.
        public IReadOnlyList<User> Watchers { get; set; }
        public IReadOnlyList<User> Stargazers { get; set; }

        public List<GeoCoordinate> Coordinates { get; set; }
    }
}