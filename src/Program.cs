using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS;

namespace YOSHI
{
    /// <summary>
    /// This class is the main file to extract emails of communities.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Retrieve the communities through input handled by the IOModule.
            List<Community> communities = IOModule.TakeInput();

            Console.WriteLine("RepoOwner,RepoName,Username,NumCommits,Email");
            foreach (Community community in communities)
            {
                try
                {
                    await DataRetriever.ExtractMailsUsingThirdQuartile(community);
                    continue;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "Exception: {0}. {1}", 
                        e.GetType(), 
                        e.Message
                        );
                    Console.ResetColor();
                    continue;
                }
            }

            // Prevent the console window from automatically closing after the
            // main process is done running
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("The application has finished processing the " +
                "inputted communities.");
            Console.WriteLine("Press Enter to close this window . . .");
            Console.ResetColor();
            ConsoleKeyInfo key = Console.ReadKey();
            while (key.Key != ConsoleKey.Enter)
            {
                key = Console.ReadKey();
            };
        }
    }
}
