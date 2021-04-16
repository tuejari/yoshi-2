using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using YOSHI.CommunityData;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for the IO-operations of YOSHI. 
    /// </summary>
    public static class IOModule
    {
        private static string OutDirFile;      // The output directory including filename

        /// <summary>
        /// This method is used to guide the user in inputting the input directory, input filename, outfput directory 
        /// and the output filename.
        /// </summary>
        public static List<Community> TakeInput()
        {
            bool validInFile = false;
            bool validOutFile = false;

            try
            {
                string inFile = "";
                // Take and validate the input file
                while (!validInFile)
                {
                    Console.WriteLine("Please enter the absolute directory of the input file, including filename and " +
                        "its extension.");
                    inFile = Console.ReadLine();
                    validInFile = File.Exists(inFile);
                    Console.WriteLine(validInFile ? "File exists." : "File does not exist, try again:");
                }

                // Take the output directory
                Console.WriteLine("Please enter the absolute directory of the output file.");
                string outDir = Console.ReadLine();
                Directory.CreateDirectory(outDir);

                // Take and validate the input specifying the output file 
                while (!validOutFile)
                {
                    Console.WriteLine("Please enter the filename of the output file. Do not include an extension, " +
                        "as its extension will be \".csv\"");
                    string outFilename = Console.ReadLine();

                    OutDirFile = outDir + '\\' + outFilename + ".csv";

                    // Create the output file and write the headers
                    using FileStream stream = File.Open(OutDirFile, FileMode.CreateNew);
                    using StreamWriter writer = new StreamWriter(stream);
                    using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<CommunityMap>();
                    csv.WriteHeader<Community>();
                    validOutFile = true;
                }

                return ReadFile(inFile);
            } 
            catch (Exception e)
            {
                throw new Exception("Failed to read input", e);
            }
        }

        /// <summary>
        /// A method used to read the file named after the value stored with the input filename (InFilename) at the 
        /// specified input directory (InDir).
        /// </summary>
        /// <returns>A list of communities storing just the repo owner and repo name.</returns>
        private static List<Community> ReadFile(string inFile)
        {
            List<Community> communities = new List<Community>();
            try
            {
                using StreamReader reader = new StreamReader(inFile);
                using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    // The CSV file needs to have "RepoName" and "RepoOwner" as headers
                    Community community = new Community(csv.GetField("RepoName"), csv.GetField("RepoOwner"));
                    communities.Add(community);
                }
            } catch (IOException e)
            {
                throw new IOException("Something went wrong while reading the input file.", e);
            }
            
            return communities;
        }

        /// <summary>
        /// A method used to write community data to a file named after the value stored with the output filename 
        /// (OutFilename) at the specified output directory (OutDir).
        /// </summary>
        public static void WriteToFile(Community community)
        {
            List<Community> communities = new List<Community>();
            communities.Add(community);

            // Append to the file.
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = false,
            };
            using FileStream stream = File.Open(OutDirFile, FileMode.Append);
            using StreamWriter writer = new StreamWriter(stream);
            using CsvWriter csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<CommunityMap>();
            csv.WriteRecords(communities);
        }

        /// <summary>
        /// This class maps the structure of the output, i.e., all community data that will be written to a CSV format.
        /// Each Map function represents a field in the CSV-file.
        /// </summary>
        public sealed class CommunityMap : ClassMap<Community>
        {
            public CommunityMap()
            {
                // TODO: Order of header fields csvhelper
                Map(m => m.RepoName);
                Map(m => m.RepoOwner);

                Map(m => m.Metrics.Structure.CommonProjects);
                Map(m => m.Metrics.Structure.PullReqInteraction);
                Map(m => m.Metrics.Structure.Followers);

                Map(m => m.Metrics.Dispersion.GeographicalDistanceMap);
                Map(m => m.Metrics.Dispersion.HofstedeCulturalDistance);

                Map(m => m.Metrics.Formality.MembershipType);
                Map(m => m.Metrics.Formality.Milestones);
                Map(m => m.Metrics.Formality.Lifetime);

                Map(m => m.Metrics.Cohesion.Followers);

                Map(m => m.Metrics.Longevity.CommitterLongevity);

                Map(m => m.Metrics.Engagement.ActiveMembers);
                Map(m => m.Metrics.Engagement.Watchers);
                Map(m => m.Metrics.Engagement.Stargazers);
                Map(m => m.Metrics.Engagement.NrPullReqComments);
                Map(m => m.Metrics.Engagement.FileCollabDistribution);
                Map(m => m.Metrics.Engagement.CommitDistribution);
                Map(m => m.Metrics.Engagement.PullReqCommitDistribution);

                Map(m => m.Characteristics.Structure);
                Map(m => m.Characteristics.Dispersion);
                Map(m => m.Characteristics.Formality);
                Map(m => m.Characteristics.Cohesion);
                Map(m => m.Characteristics.Longevity);
                Map(m => m.Characteristics.Engagement);

                Map(m => m.Pattern.SocialNetwork);
                Map(m => m.Pattern.FormalGroup);
                Map(m => m.Pattern.ProjectTeam);
                Map(m => m.Pattern.WorkGroup);
                Map(m => m.Pattern.NetworkOfPractice);
                Map(m => m.Pattern.InformalCommunity);
                Map(m => m.Pattern.FormalNetwork);
                Map(m => m.Pattern.InformalNetwork);
                Map(m => m.Pattern.CommunityOfPractice);
            }
        }
    }
}
