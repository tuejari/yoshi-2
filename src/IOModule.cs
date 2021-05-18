using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS.Geocoding;

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
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Please enter the absolute directory of the input file, including filename and " +
                        "its extension.");
                    Console.ResetColor();
                    inFile = Console.ReadLine();
                    validInFile = File.Exists(inFile);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(validInFile ? "File exists." : "File does not exist, try again:");
                    Console.ResetColor();
                }

                // Take the output directory
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Please enter the absolute directory of the output file.");
                Console.ResetColor();
                string outDir = Console.ReadLine();
                Directory.CreateDirectory(outDir);

                // Take and validate the input specifying the output file 
                while (!validOutFile)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Please enter the filename of the output file. Do not include an extension, " +
                        "as its extension will be \".csv\"");
                    Console.ResetColor();
                    string outFilename = Console.ReadLine();

                    OutDirFile = outDir + '\\' + outFilename + ".csv";

                    // Create the output file and write the headers
                    using FileStream stream = File.Open(OutDirFile, FileMode.CreateNew);
                    using StreamWriter writer = new StreamWriter(stream);
                    using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<CommunityMap>();
                    csv.WriteHeader<Community>();
                    csv.NextRecord();
                    validOutFile = true;
                }

                // https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/understanding-bing-maps-transactions?redirectedfrom=MSDN
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Windows App, Non-profit, and Education keys can make 50,000 requests per 24 hour period.");
                Console.WriteLine("Please enter the number of Bing Maps requests left.");
                Console.ResetColor();
                int bingRequestsLeft = Convert.ToInt32(Console.ReadLine());
                GeoService.BingRequestsLeft = bingRequestsLeft;

                return ReadFile(inFile);
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
                    Community community = new Community(csv.GetField("RepoOwner"), csv.GetField("RepoName"));
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
            // Append to the file.
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using FileStream stream = File.Open(OutDirFile, FileMode.Append);
            using StreamWriter writer = new StreamWriter(stream);
            using CsvWriter csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<CommunityMap>();
            csv.WriteRecord(community);
            csv.NextRecord();
        }

        /// <summary>
        /// This class maps the structure of the output, i.e., all community data that will be written to a CSV format.
        /// Each Map function represents a field in the CSV-file.
        /// </summary>
        public sealed class CommunityMap : ClassMap<Community>
        {
            public CommunityMap()
            {
                this.Map(m => m.RepoName).Index(0);
                this.Map(m => m.RepoOwner).Index(1);

                this.Map(m => m.Metrics.Structure.CommonProjects).Index(2);
                this.Map(m => m.Metrics.Structure.Followers).Index(4);
                this.Map(m => m.Metrics.Structure.PullReqInteraction).Index(3);

                this.Map(m => m.Metrics.Dispersion.VarianceGeographicalDistance).Index(5);
                this.Map(m => m.Metrics.Dispersion.VarianceHofstedeCulturalDistance).Index(6);

                this.Map(m => m.Metrics.Formality.MeanMembershipType).Index(7);
                this.Map(m => m.Metrics.Formality.Milestones).Index(8);
                this.Map(m => m.Metrics.Formality.Lifetime).Index(9);

                this.Map(m => m.Metrics.Engagement.MedianActiveMember).Index(10);
                this.Map(m => m.Metrics.Engagement.MedianWatcher).Index(11);
                this.Map(m => m.Metrics.Engagement.MedianStargazer).Index(12);
                this.Map(m => m.Metrics.Engagement.MedianNrCommentsPerPullReq).Index(13);
                this.Map(m => m.Metrics.Engagement.MedianFileCollabDistribution).Index(14);
                this.Map(m => m.Metrics.Engagement.MedianCommitDistribution).Index(15);
                this.Map(m => m.Metrics.Engagement.MedianMonthlyPullCommitCommentsDistribution).Index(16);

                this.Map(m => m.Metrics.Longevity.MeanCommitterLongevity).Index(17);

                //this.Map(m => m.Metrics.Cohesion.Followers).Index(18);

                this.Map(m => m.Characteristics.Structure).Index(19);
                this.Map(m => m.Characteristics.Dispersion).Index(20);
                this.Map(m => m.Characteristics.Formality).Index(21);
                this.Map(m => m.Characteristics.Engagement).Index(22);
                this.Map(m => m.Characteristics.Longevity).Index(23);
                //this.Map(m => m.Characteristics.Cohesion).Index(24);

                this.Map(m => m.Pattern.SN).Index(25);
                this.Map(m => m.Pattern.FG).Index(26);
                this.Map(m => m.Pattern.PT).Index(27);
                //this.Map(m => m.Pattern.WorkGroup).Index(28);
                this.Map(m => m.Pattern.NoP).Index(29);
                this.Map(m => m.Pattern.IC).Index(30);
                this.Map(m => m.Pattern.FN).Index(31);
                this.Map(m => m.Pattern.IN).Index(32);
                this.Map(m => m.Pattern.CoP).Index(33);
            }
        }
    }
}
