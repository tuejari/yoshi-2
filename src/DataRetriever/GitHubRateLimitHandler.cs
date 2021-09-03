﻿using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace YOSHI.DataRetrieverNS
{
    public static class GitHubRateLimitHandler
    {

        // AUXILIARY: Methods used to delegate GitHub API calls and handling of rate limits. 
        /// <param name="maxBatchSize">Setting API options to retrieve max batch sizes, reducing the number of requests.</param>
        public async static Task<T> Delegate<T>(
            Func<string, string, CommitRequest, ApiOptions, Task<T>> func,
            string repoOwner,
            string repoName,
            CommitRequest commitRequest,
            ApiOptions maxBatchSize)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Task<T> task = func(repoOwner, repoName, commitRequest, maxBatchSize);
                    return await task;
                }
                catch (RateLimitExceededException)
                {
                    // When we exceed the rate limit we check when the limit resets and wait until that time before we try 2 more times.
                    WaitUntilReset();
                }
            }
            throw new Exception("Failed too many times to retrieve GitHub data.");
        }

        /// <summary>
        /// This method is used to delegate the GitHub API requests. It handles the rate limit. 
        /// </summary>
        /// <typeparam name="T">The type that func will return.</typeparam>
        /// <param name="func">The function that we want to call.</param>
        /// <param name="username">The username, whose data we want to retrieve.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        /// <exception cref="Exception">Throws an exception if after 3 times of trying to retrieve data, 
        /// the data RateLimitExceededException still occurs, or if another exception is thrown.</exception>
        public async static Task<T> Delegate<T>(
            Func<string, Task<T>> func,
            string username)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Task<T> task = func(username);
                    return await task;
                }
                catch (RateLimitExceededException)
                {
                    // When we exceed the rate limit we check when the limit resets and wait until that time before we try 2 more times.
                    WaitUntilReset();
                }
            }
            throw new Exception("Failed too many times to retrieve GitHub data.");
        }

        /// <summary>
        /// A method to take care of the waiting until the GitHub rate reset. 
        /// </summary>
        private static void WaitUntilReset()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            // Set the default wait time to one hour
            TimeSpan timespan = TimeSpan.FromHours(1);

            ApiInfo apiInfo = DataRetriever.Client.GetLastApiInfo();
            RateLimit rateLimit = apiInfo?.RateLimit;
            DateTimeOffset? whenDoesTheLimitReset = rateLimit?.Reset;
            if (whenDoesTheLimitReset != null)
            {
                DateTimeOffset limitReset = (DateTimeOffset)whenDoesTheLimitReset;
                timespan = (DateTimeOffset)whenDoesTheLimitReset - DateTimeOffset.Now;
                timespan = timespan.Add(TimeSpan.FromSeconds(30)); // Add 30 seconds to the timespan

                Console.WriteLine("GitHub Rate Limit reached.");
                Console.WriteLine("Waiting until: " + limitReset.AddSeconds(30).DateTime.ToLocalTime().ToString());
            }
            else
            {
                // If we don't know the reset time, we wait the default time of 1 hour
                Console.WriteLine("Waiting until: " + DateTimeOffset.Now.DateTime.ToLocalTime().AddHours(1));
            }
            Console.ResetColor(); // Reset before sleep, otherwise color remains even when application is closed during the sleep.
            Thread.Sleep(timespan); // Wait until the rate limit resets
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Done waiting for the rate limit reset, continuing now: " + DateTimeOffset.Now.DateTime.ToLocalTime().ToString());
            Console.ResetColor();
        }
    }
}