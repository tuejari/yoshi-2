using System;

namespace YOSHI.DataRetrieverNS
{
    /// <summary>
    /// Class responsible for filtering the GitHub data. It checks that everything is within the given time window 
    /// (default 90 days + today). It filters out all data about GitHub users that are not considered members.
    /// </summary>
    public static class Filters
    {
        public static DateTimeOffset EndDateTimeWindow { get; private set; }
        public static DateTimeOffset StartDateTimeWindow { get; private set; }

        public static void SetTimeWindow(DateTimeOffset endDateTimeWindow)
        {
            int days = 90; // snapshot period of 3 months (approximated using 90 days)
            // Note: Currently other length periods are not supported.
            // Engagementprocessor uses hardcoded month thresholds of 30 and 60
            EndDateTimeWindow = endDateTimeWindow;
            StartDateTimeWindow = EndDateTimeWindow.AddDays(-days);
        }
    }
}
