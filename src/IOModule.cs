using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        /// <exception cref="System.IO.IOException">Thrown when something goes wrong while reading the input or when 
        /// writing to the output file.</exception>
        public static (List<Community>, int) TakeInput()
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

                // https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/understanding-bing-maps-transactions?redirectedfrom=MSDN
                Console.WriteLine("Windows App, Non-profit, and Education keys can make 50,000 requests per 24 hour period.");
                Console.WriteLine("Please enter the number of Bing Maps requests left.");
                int bingRequestsLeft = Convert.ToInt32(Console.ReadLine());

                return (ReadFile(inFile), bingRequestsLeft);
            }
            catch (IOException e)
            {
                throw new IOException("Failed to read input or to write headers to output file", e);
            }
        }

        /// <summary>
        /// A method used to read the file named after the value stored with the input filename (InFilename) at the 
        /// specified input directory (InDir).
        /// </summary>
        /// <returns>A list of communities storing just the repo owner and repo name.</returns>
        /// <exception cref="System.IO.IOException">Thrown when something goes wrong while reading the input file.</exception>
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
            }
            catch (IOException e)
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
            List<Community> communities = new List<Community> { community };

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
                this.Map(m => m.RepoName);
                this.Map(m => m.RepoOwner);

                this.Map(m => m.Metrics.Structure.CommonProjects);
                this.Map(m => m.Metrics.Structure.PullReqInteraction);
                this.Map(m => m.Metrics.Structure.Followers);

                this.Map(m => m.Metrics.Dispersion.MeanGeographicalDistance);
                //this.Map(m => m.Metrics.Dispersion.HofstedeCulturalDistance);

                this.Map(m => m.Metrics.Formality.MeanMembershipType);
                this.Map(m => m.Metrics.Formality.Milestones);
                this.Map(m => m.Metrics.Formality.Lifetime);

                //this.Map(m => m.Metrics.Cohesion.Followers);

                this.Map(m => m.Metrics.Longevity.MeanCommitterLongevity);

                this.Map(m => m.Metrics.Engagement.MedianActiveMember);
                this.Map(m => m.Metrics.Engagement.MedianWatcher);
                this.Map(m => m.Metrics.Engagement.MedianStargazer);
                this.Map(m => m.Metrics.Engagement.MedianNrPullReqComments);
                this.Map(m => m.Metrics.Engagement.MedianFileCollabDistribution);
                this.Map(m => m.Metrics.Engagement.MedianCommitDistribution);
                this.Map(m => m.Metrics.Engagement.MedianMonthlyPullCommitCommentsDistribution);

                this.Map(m => m.Characteristics.Structure);
                this.Map(m => m.Characteristics.Dispersion);
                this.Map(m => m.Characteristics.Formality);
                this.Map(m => m.Characteristics.Cohesion);
                this.Map(m => m.Characteristics.Longevity);
                this.Map(m => m.Characteristics.Engagement);

                this.Map(m => m.Pattern.SocialNetwork);
                this.Map(m => m.Pattern.FormalGroup);
                this.Map(m => m.Pattern.ProjectTeam);
                this.Map(m => m.Pattern.WorkGroup);
                this.Map(m => m.Pattern.NetworkOfPractice);
                this.Map(m => m.Pattern.InformalCommunity);
                this.Map(m => m.Pattern.FormalNetwork);
                this.Map(m => m.Pattern.InformalNetwork);
                this.Map(m => m.Pattern.CommunityOfPractice);
            }
        }
    }
}
