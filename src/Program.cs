using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YOSHI.CommunityData;

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
            // Retrieve the communities through console input handled by the IOModule.
            (List<Community> communities, int bingRequestsLeft) = IOModule.TakeInput();
            DataRetriever.BingRequestsLeft = bingRequestsLeft;

            try
            {
                //foreach (Community community in communities)
                //{
                //    Console.WriteLine("Started processing community {0}, url: {1}", community.RepoOwner, community.RepoName);
                //    // Retrieving GitHub data needed to compute whether the community exhibits a structure
                //    Console.WriteLine("Retrieving GitHub data needed for computing structure...");
                //    DataRetriever.RetrieveStructureData(community);

                //    // If the community exhibits a structure then:
                //    if (AttributeProcessor.ComputeStructure(community))
                //    {
                //        // Miscellaneous characteristics are: dispersion, formality, cohesion, engagement, longevity
                //        Console.WriteLine("Retrieving GitHub data needed for miscellaneous characteristics...");
                //        DataRetriever.RetrieveMiscellaneousData(community);

                //        Console.WriteLine("Computing miscellaneous characteristics...");
                //        AttributeProcessor.ComputeMiscellaneousAttributes(community);

                //        Console.WriteLine("Determining community pattern...");
                //        PatternProcessor.ComputePattern(community);
                //    }
                //    else
                //    {
                //        // The community exhibits no structure, hence we cannot compute a pattern. Thus we skip computing 
                //        // all other characteristics.
                //        Console.WriteLine("This community does not exhibit a structure.");
                //    }

                //    Console.WriteLine("Writing community data to file...");
                //    IOModule.WriteToFile(community);

                //    Console.WriteLine("Finished processing community from {0}, url: {1}", community.RepoOwner, community.RepoName);
                //}
            }
            catch (Exception)
            {
                // We want to output the number of Bing Maps Requests left, since it can take hours for Bing Maps Requests to update
                Console.WriteLine("There are still {0} Bing Maps Requests left", DataRetriever.BingRequestsLeft);
                throw;
            }
        }
    }
}
