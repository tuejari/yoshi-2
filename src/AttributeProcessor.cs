using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YOSHI.CommunityData;
using YOSHI.CommunityData.MetricData;
using yoshi_revision.src.Util;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for using the retrieved GitHub data and computing several metrics and then values for
    /// the corresponding characteristics.
    /// </summary>
    public static class AttributeProcessor
    {
        /// <summary>
        /// A method that calls all specific ComputeAttribute methods other than ComputeStructure
        /// </summary>
        /// <param name="community">The community for which we need to compute the attributes.</param>
        public static void ComputeMiscellaneousAttributes(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community structure and then decides whether a
        /// community exhibits a structure or not.
        /// </summary>
        /// <param name="community">The community for which we need to compute the structure.</param>
        /// <returns>A boolean whether the community exhibits a structure.</returns>
        public static bool ComputeStructure(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community dispersion. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the dispersion.</param>
        private static void ComputeDispersion(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community cohesion. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the cohesion.</param>
        private static void ComputeCohesion(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community engagement. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the engagement.</param>
        private static void ComputeEngagement(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community formality. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the formality.</param>
        private static void ComputeFormality(Community community)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A method that computes several metrics used to measure community longevity. It modifies the given community. 
        /// </summary>
        /// <param name="community">The community for which we need to compute the longevity.</param>
        private static void ComputeLongevity(Community community)
        {
            throw new NotImplementedException();
        }

        // ------------------------------------------------- STRUCTURE -------------------------------------------------

        /// <summary>
        /// We compute the average common projects between all users. 
        /// </summary>
        /// <param name="mapUserRepositories">A mapping from usernames to the repositories that they worked on.</param>
        /// <returns>The average number of common projects between users.</returns>
        private static float AvgCommonProjects(Dictionary<string, IReadOnlyList<Repository>> mapUserRepositories, 
            string repoName)
        {
            // Obtain a mapping from all users (usernames) to the names of the repositories they worked on
            Dictionary<string, List<string>> mapUserRepoName = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, IReadOnlyList<Repository>> mapUserRepos in mapUserRepositories)
            {
                foreach (Repository repo in mapUserRepos.Value)
                {
                    if (repo.Name != repoName) // Exclude the repository we are currently analyzing
                    {
                        mapUserRepoName.Add(mapUserRepos.Key, new List<string>());
                        mapUserRepoName[mapUserRepos.Key].Add(repo.Name);
                    }
                }
            }

            // Find common projects by comparing the names of repositories they worked on
            float totalCollabProjects = 0F;
            foreach (KeyValuePair<string, List<string>> firstUser in mapUserRepoName)
            {
                int nrCommonProjects = 0;
                foreach (KeyValuePair<string, List<string>> secondUser in mapUserRepoName)
                {
                    if (firstUser.Key != secondUser.Key)
                    {
                        // We compute the intersections of the list of repositories and then count the number of items
                        IEnumerable<string> commonProjects = firstUser.Value.Intersect(secondUser.Value);
                        nrCommonProjects += commonProjects.Count();
                    }
                }
                totalCollabProjects += nrCommonProjects;
            }
            float avgCollabProjects = (float)totalCollabProjects / mapUserRepoName.Count;
            return avgCollabProjects;
        }

        /// <summary>
        /// This method computes the follower/following connections between each of the contributors and collaborators,
        /// but its result does not distinguish between followers and following. 
        /// </summary>
        /// <param name="mapUserFollowers">A mapping for each username to a list of the users followers.</param>
        /// <param name="mapUserFollowing">A mapping for each username to a list of the users that they themselves 
        /// follow.</param>
        /// <returns>A mapping for each username to a combined set of followers and following from which the names 
        /// have been extracted.</returns>
        private static Dictionary<string, HashSet<string>> FollowConnections(
            Dictionary<string, IReadOnlyList<User>> mapUserFollowers,
            Dictionary<string, IReadOnlyList<User>> mapUserFollowing)
        {
            Dictionary<string, HashSet<string>> followConnections = new Dictionary<string, HashSet<string>>();

            // Obtain a mapping from all users (usernames) to the names of the followers and following
            foreach (KeyValuePair<string, IReadOnlyList<User>> mapUserFollower in mapUserFollowers)
            {
                foreach (User follower in mapUserFollower.Value)
                {
                    followConnections.Add(mapUserFollower.Key, new HashSet<string>());
                    followConnections[mapUserFollower.Key].Add(follower.Name);
                }
            }

            foreach (KeyValuePair<string, IReadOnlyList<User>> userFollowing in mapUserFollowing)
            {
                foreach (User following in userFollowing.Value)
                {
                    followConnections.Add(userFollowing.Key, new HashSet<string>());
                    followConnections[userFollowing.Key].Add(following.Name);
                }
            }
            return followConnections;
        }

        // ------------------------------------------------- LONGEVITY -------------------------------------------------

        /// <summary>
        /// This method is used to compute the average committer longevity. 
        /// </summary>
        /// <param name="commits">A list of commits for the current repository.</param>
        /// <returns>The average committer longevity.</returns>
        private static float AvgCommitterLongevity(IReadOnlyList<GitHubCommit> commits)
        {
            // We group the list of commits per committer
            Dictionary<string, List<DateTimeOffset>> mapUserCommitDate = new Dictionary<string, List<DateTimeOffset>>();
            foreach(GitHubCommit commit in commits)
            {
                string committer = commit.Committer.Login;
                if (committer != null)
                {
                    mapUserCommitDate.Add(committer, new List<DateTimeOffset>());
                    mapUserCommitDate[committer].Add(commit.Commit.Committer.Date);
                }
            }

            int totalCommitterLongevityInDays = 0;
            // For each committer, we compute the dates of their first- and last commit.
            foreach(KeyValuePair<string,List<DateTimeOffset>> userCommitDate in mapUserCommitDate)
            {
                // TODO: Check whether we want to use author date or committer date.
                DateTimeOffset dateFirstCommit = DateTimeOffset.MaxValue;
                DateTimeOffset dateLastCommit = DateTimeOffset.MinValue;
                foreach (DateTimeOffset commitDate in userCommitDate.Value)
                {
                    // If current evaluated commit is earlier than previous earliest commit
                    if (dateFirstCommit.CompareTo(commitDate) < 0) 
                    {
                        dateFirstCommit = commitDate;
                    } // If current evaluated commit is later than previous last commit 
                    else if (dateLastCommit.CompareTo(commitDate) > 0)
                    {
                        dateLastCommit = commitDate;
                    }
                }
                // Add the difference between committers first and last commits to the total commit longevity
                totalCommitterLongevityInDays += (dateLastCommit - dateFirstCommit).Days;
            }
            float avgCommitterLongevity = (float)totalCommitterLongevityInDays / mapUserCommitDate.Count;
            return avgCommitterLongevity;
        }

        // ------------------------------------------------- FORMALITY -------------------------------------------------

        /// <summary>
        /// This method computes the average membership type from a list of contributors and a list of collaborators. 
        /// The levels of control assigned to repository members are +1 for contributors and +2 for collaborators (which
        /// have more privileges). 
        /// </summary>
        /// <param name="contributors">A list of contributors from a repository.</param>
        /// <param name="collaborators">A list of collaborators from a repository.</param>
        /// <returns>A float denoting the average membership type of the contributors and collaborators.</returns>
        private static float AvgMembershipType(IReadOnlyList<RepositoryContributor> contributors, IReadOnlyList<User> collaborators)
        {
            // We transform the lists of contributors and collaborators to only the usernames, so it becomes easier
            // to compute the difference of two lists. 
            List<string> contributorNames = new List<string>();
            List<string> collaboratorNames = new List<string>();

            foreach (RepositoryContributor contributor in contributors)
            {
                if (contributor.Login != null)
                {
                    contributorNames.Add(contributor.Login);
                }
            }

            foreach (User collaborator in collaborators)
            {
                if (collaborator.Login != null)
                {
                    collaboratorNames.Add(collaborator.Login);
                }
            }

            // We remove collaborators that are also marked as contributors from the list of contributors
            // (collaborators count stronger due to more permissions)
            List<string> cleanedContributorNames = contributorNames.Except(collaboratorNames).ToList();
            float avgMembershipType = (float)(cleanedContributorNames.Count + collaboratorNames.Count * 2) /
                (cleanedContributorNames.Count + collaboratorNames.Count);

            return avgMembershipType;
        }

        /// <summary>
        /// This method is used to compute the number of project milestones.
        /// </summary>
        /// <param name="milestones">A list of milestones from a project.</param>
        /// <returns>The number of milestones in the given list.</returns>
        private static int NrProjectMilestones(IReadOnlyList<Milestone> milestones)
        {
            return milestones.Count;
        }

        /// <summary>
        /// This method is used to compute the project lifetime in number of days, using the first commit and last 
        /// commit.
        /// </summary>
        /// <param name="commits">A list of commits from a repository.</param>
        /// <returns>The project lifetime in number of days.</returns>
        private static int ProjectLifetimeInDays(IReadOnlyList<GitHubCommit> commits)
        {
            // TODO: Check whether we want to use author date or committer date.
            // TODO: Move part computing first and last commit of this method to an auxilliary method. (currently
            // code is duplicated)
            DateTimeOffset dateFirstCommit = DateTimeOffset.MaxValue;
            DateTimeOffset dateLastCommit = DateTimeOffset.MinValue;
            foreach (GitHubCommit commit in commits)
            {
                DateTimeOffset commitDate = commit.Commit.Committer.Date;
                // If current evaluated commit is earlier than previous earliest commit
                if (dateFirstCommit.CompareTo(commitDate) < 0)
                {
                    dateFirstCommit = commitDate;
                } // If current evaluated commit is later than previous last commit 
                else if (dateLastCommit.CompareTo(commitDate) > 0)
                {
                    dateLastCommit = commitDate;
                }
            }
            TimeSpan timespan = dateLastCommit - dateFirstCommit;
            return timespan.Days;
        }

        // ------------------------------------------------ DISPERSION -------------------------------------------------

        /// <summary>
        /// Given a list of coordinates, this method computes the average geographical (spherical) distance by first 
        /// computing the medium spherical distance for each coordinate to all other coordinates and then taking its 
        /// average. 
        /// </summary>
        /// <param name="coordinates">A list of coordinates for which we want to compute the average geographical
        /// distance.</param>
        /// <returns>The average geographical distance between the given list of coordinates.</returns>
        private static double AvgGeographicalDistance(List<GeoCoordinate> coordinates)
        {
            // sum of medium distances in km as a double
            double sumDistances = 0.0;

            // Compute the medium distance for each distinct pair of coordinates in the given list of coordinates
            for (int i = 0; i < coordinates.Count; i++)
            {
                GeoCoordinate coordinateA = coordinates[i];
                double mediumDistance = 0;
                for (int j = 0; j < coordinates.Count; j++)
                {
                    if (i != j)
                    {
                        GeoCoordinate coordinateB = coordinates[j];
                        mediumDistance += coordinateA.SphericalDistance(coordinateB);
                    }
                }
                mediumDistance /= (coordinates.Count - 1);
                // converted to km
                sumDistances += mediumDistance / 1000;
            }

            return (double) sumDistances / coordinates.Count;
        }
    }
}
