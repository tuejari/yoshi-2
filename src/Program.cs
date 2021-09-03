using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS;

namespace YOSHI
{
    /// <summary>
    /// This is the main class for extracting the stats of repositories. 
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Retrieve the communities through user input handled by the IOModule.
            List<Community> communities = IOModule.TakeInput();

            Console.WriteLine("id;owner;name;q3_devs;releases;commits;" +
                "contributors;milestones;language;LOC;ALOC;stargazers;watchers;" +
                "forks;size (KB);Domain;latestCommitDate;url;mailing list;" +
                "description");

            foreach (Community community in communities)
            {
                await DataRetriever.ExtractStats(community);
            }

            // Prevent the console window from automatically closing after the main
            // process is done running
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
