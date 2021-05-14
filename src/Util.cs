using Octokit;
using System;
using System.Collections.Generic;

namespace YOSHI
{
    public static class Util
    {
        /// <summary>
        /// A method that takes a DateTimeOffset object and checks whether it is within the specified time window x number 
        /// of days (Default: 3 months,  i.e., x = 90 days). This window ends at today's midnight time and starts at 
        /// midnight x days prior.
        /// </summary>
        /// <param name="dateTime">A DateTimeOffset object</param>
        /// <returns>Whether the DateTimeOffset object falls within the time window.</returns>
        /// <exception cref="System.NullReferenceException">Thrown when the datetime parameter is null.</exception>
        public static bool CheckWithinTimeWindow(DateTimeOffset dateTime, int days = 90)
        {
            // We set the date time offset window for the 3 months earlier from now (approximated using 90 days)
            DateTime EndDate = new DateTimeOffset(DateTime.Today).Date;
            DateTime StartDate = EndDate.AddDays(-days).Date;
            try
            {
                DateTime date = dateTime.Date; // Extract the date from the datetime object
                return date >= StartDate && date < EndDate;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Given a list of users, extracts a set of usernames. Also checks whether users are considered members within 
        /// the time period.
        /// </summary>
        /// <param name="users">The list of users that we want to extract the usernames from.</param>
        /// <param name="members">The list of members within the time period.</param>
        /// <returns>A set of usernames</returns>
        public static HashSet<string> ConvertUsersToUsernames(IReadOnlyList<User> users, HashSet<string> members)
        {
            HashSet<string> names = new HashSet<string>();
            foreach (User user in users)
            {
                if (user.Login != null && members.Contains(user.Login))
                {
                    names.Add(user.Login);
                }
            }
            return names;
        }
    }
}
