using Octokit;
using System.Collections.Generic;
using yoshi_revision.src.Util;

namespace YOSHI.CommunityData
{
    /// <summary>
    /// This class is responsible for storing all community related data that was retrieved from GitHub. 
    /// </summary>
    public class GitHubData
    {
        public Repository Repo { get; set; }
        public IReadOnlyList<RepositoryContributor> Contributors { get; set; }
        public IReadOnlyList<User> ContributorsAsUsers { get; set; } 
        public IReadOnlyList<User> Collaborators { get; set; }
        public Dictionary<string, IReadOnlyList<User>> MapUserFollowers { get; set; }
        public Dictionary<string, IReadOnlyList<User>> MapUserFollowing { get; set; }
        public Dictionary<string, IReadOnlyList<Repository>> MapUserRepositories { get; set; }

        public IReadOnlyList<Milestone> Milestones { get; set; }
        public IReadOnlyList<GitHubCommit> Commits { get; set; }
        public IReadOnlyList<CommitComment> CommitComments { get; set; }
        public IReadOnlyList<PullRequestReviewComment> PullReqComments { get; set; }
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