using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS;

namespace YOSHI
{
    /// <summary>
    /// This class is the main file of the revised YOSHI. This tool will use GitHub data to identify community patterns.
    /// It is based on YOSHI from the paper below. To achieve its purposes it will take input from a file; which can 
    /// contain multiple lines of "owner; repository" pairs. Then it uses this input to extract GitHub data using the 
    /// GitHub API: https://docs.github.com/en/rest
    /// Using the extracted data; YOSHI computes several metrics that are used to obtain numerical values for several 
    /// community characteristics. These characteristics are then used to identify a community's pattern.
    /// 
    /// The following paper provides a detailed explanation of community patterns; characteristics; and YOSHI:
    /// Authors:    D.A. Tamburri; F. Palomba; A. Serebrenik; and A. Zaidman
    /// Title:      Discovering community patterns in open - source: a systematic approach and its evaluation
    /// Journal:    Empir.Softw.Eng.
    /// Volume:     24
    /// Number:     3
    /// Pages:      1369--1417
    /// Year:       2019
    /// URL:        https://doi.org/10.1007/s10664-018-9659-9
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            // Retrieve the communities through console input handled by the IOModule.
            List<Community> communities = IOModule.TakeInput();
            //Console.WriteLine("id;owner;name;q3_devs;releases;commits;contributors;milestones;language;LOC;ALOC;stargazers;watchers;forks;size (KB);Domain;latestCommitDate;url;mailing list;description");
            foreach (Community community in communities)
            {
                await DataRetriever.ExtractStats(community);
            }

            // Prevent the console window from automatically closing after the main process is done running
            // TODO: Write the console log to a file
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("The application has finished processing the inputted communities.");
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
