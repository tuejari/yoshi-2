using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using YOSHI.CommunityData;
using YOSHI.DispersionProcessorNew;
using YOSHI.DispersionProcessorOld;

namespace YOSHI
{
    /// <summary>
    /// This class is the main file of the revised YOSHI. This tool will use GitHub data to identify community patterns.
    /// It is based on YOSHI from the paper below. To achieve its purposes it will take input from a file, which can 
    /// contain multiple lines of "owner, repository" pairs. Then it uses this input to extract GitHub data using the 
    /// GitHub API: https://docs.github.com/en/rest
    /// Using the extracted data, YOSHI computes several metrics that are used to obtain numerical values for several 
    /// community characteristics. These characteristics are then used to identify a community's pattern.
    /// 
    /// The following paper provides a detailed explanation of community patterns, characteristics, and YOSHI:
    /// Authors:    D.A. Tamburri, F. Palomba, A. Serebrenik, and A. Zaidman
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

            // Used the statement below to test the old Hofstede Countries' compatibility with Bing Maps Geocoding
            //await Geocoding.GeoService.TestOldHICountries(OldHI.Hofstede.Keys.ToList());

            // Retrieve the communities through console input handled by the IOModule.
            List<Community> communities = IOModule.TakeInput();
            Dictionary<string, string> failedCommunities = new Dictionary<string, string>();

            foreach (Community community in communities)
            {
                try
                {

                    if (!(community.Data.Coordinates.Count < 2 || community.Data.NewCountries.Count < 2))
                    {
                        DispersionProcessorN.ComputeDispersion(community);
                    } 
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Not enough coordinates ({0}) or countries (N) ({1})", community.Data.Coordinates.Count, community.Data.NewCountries.Count);
                        Console.ResetColor();
                    }

                    if (!(community.Data.Coordinates.Count < 2 || community.Data.OldCountries.Count < 2))
                    {
                        DispersionProcessorO.ComputeDispersion(community);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Not enough coordinates ({0}) or countries (O) ({1})", community.Data.Coordinates.Count, community.Data.OldCountries.Count);
                        Console.ResetColor();
                    }

                    IOModule.WriteToFile(community);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception: {0}. {1}", e.GetType(), e.Message);
                    Console.ResetColor();
                    failedCommunities.Add(community.RepoName, e.Message);
                    continue;
                }
            }

            // Make sure to output the communities that failed at the end to make them easily identifiable
            if (failedCommunities.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The following communities failed due to exceptions:");
                foreach (KeyValuePair<string, string> failedCommunity in failedCommunities)
                {
                    Console.WriteLine("{0}, {1}", failedCommunity.Key, failedCommunity.Value);
                }
                Console.ResetColor();
            }

            // Prevent the console window from automatically closing after the main process is done running
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
