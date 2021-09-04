using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YOSHI.CommunityData;
using YOSHI.DispersionProcessorNew;
using YOSHI.DispersionProcessorOld;

namespace YOSHI
{
    /// <summary>
    /// This class is the main class for the comparison between the old and new Hofstede metrics.  
    /// </summary>
    class Program
    {
        static async Task Main()
        {

            // Used the statement below to test the old Hofstede Countries' compatibility with Bing Maps Geocoding
            //await Geocoding.GeoService.TestOldHICountries(OldHI.Hofstede.Keys.ToList());

            // Retrieve the communities through user input handled by the IOModule.
            List<Community> communities = IOModule.TakeInput();
            Dictionary<string, string> failedCommunities = new Dictionary<string, string>();

            foreach (Community community in communities)
            {
                try
                {
                    // Compute dispersion using the new Hofstede metrics
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

                    // Compute dispersion using the old Hofstede metrics
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
