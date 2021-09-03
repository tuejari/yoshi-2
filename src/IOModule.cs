using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YOSHI.CommunityData;
using YOSHI.DataRetrieverNS;

namespace YOSHI
{
    /// <summary>
    /// This class is responsible for the IO-operations of YOSHI. 
    /// </summary>
    public static class IOModule
    {
        /// <summary>
        /// This method is used to guide the user in inputting the input directory, input filename, outfput directory 
        /// and the output filename.
        /// </summary>
        /// <exception cref="IOException">Thrown when something goes wrong while reading the input or when 
        /// writing to the output file.</exception>
        public static List<Community> TakeInput()
        {
            try
            {
                // Take and validate the input file
                string inFile;
                do
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Please enter the absolute directory of the input file, including filename and " +
                        "its extension.");
                    Console.ResetColor();
                    inFile = Console.ReadLine();
                }
                while (!File.Exists(inFile));

                // Set the enddate of the time window, it defaults to use midnight UTC time.
                // It is possible to enter a specific time, but this has not been tested.
                DateTimeOffset endDate;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Enter end date of time window (YYYY-MM-DD) in UTC");
                Console.ResetColor();
                while (!DateTimeOffset.TryParse(Console.ReadLine(), out endDate))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Invalid date");
                    Console.WriteLine("Enter end date of time window (YYYY-MM-DD) in UTC");
                    Console.ResetColor();
                }
                // Make sure that it is counted as UTC datetime and not as a local time
                Filters.SetTimeWindow(endDate);

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
        /// <exception cref="IOException">Thrown when something goes wrong while reading the input file.</exception>
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
    }
}
