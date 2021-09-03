using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YOSHI.CommunityData;
using Geocoding;

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

                string outDir;
                do
                {
                    // Take the output directory
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Please enter an existing absolute directory for the output file.");
                    Console.ResetColor();
                    outDir = @"" + Console.ReadLine();
                }
                while (!Directory.Exists(outDir));

                // Take and validate the input specifying the output file 
                do
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Please enter the filename of the output file. Do not include an extension, " +
                        "as its extension will be \".csv\"");
                    Console.ResetColor();
                    string outFilename = Console.ReadLine();

                    OutDirFile = outDir + '\\' + outFilename + ".csv";
                }
                while (File.Exists(OutDirFile));

                // Create the output file and write the headers
                using FileStream stream = File.Open(OutDirFile, FileMode.CreateNew);
                using StreamWriter writer = new StreamWriter(stream);
                using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<CommunityMap>();
                csv.WriteHeader<Community>();
                csv.NextRecord();

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
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "\t" };
                using CsvReader csv = new CsvReader(reader, config);
                csv.Read();
                csv.ReadHeader();
                string name = "";
                Community community = null;
                HashSet<string> countriesMissingHI = new HashSet<string>();
                HashSet<string> countriesMissingOldHI = new HashSet<string>();
                while (csv.Read())
                {
                    if (name != csv.GetField("RepoName"))
                    {
                        name = csv.GetField("RepoName");
                        community = new Community(csv.GetField("RepoOwner"), csv.GetField("RepoName"));
                        community.Data.Coordinates = new List<Location>();
                        community.Data.OldCountries = new List<string>();
                        community.Data.NewCountries = new List<string>();
                        communities.Add(community);
                    }                   

                    double lat = Convert.ToDouble(csv.GetField("Latitude"));
                    double lng = Convert.ToDouble(csv.GetField("Longitude"));
                    community.Data.Coordinates.Add(new Location(lat, lng));

                    string country = csv.GetField("CountryRegion");
                    
                    if (HI.Hofstede.ContainsKey(country))
                    {
                        community.Data.NewCountries.Add(country);
                    }
                    else
                    {
                        countriesMissingHI.Add(country);
                    }

                    if (OldHI.Hofstede.ContainsKey(country))
                    {
                        community.Data.OldCountries.Add(country);
                    } 
                    else
                    {
                        countriesMissingOldHI.Add(country);
                    }
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("HI does not contain:");
                foreach (string country in countriesMissingHI)
                {
                    Console.WriteLine(country);
                }
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OldHI does not contain:");
                foreach (string country in countriesMissingOldHI)
                {
                    Console.WriteLine(country);
                }
                Console.ResetColor();
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
                this.Map(m => m.RepoOwner).Index(0);
                this.Map(m => m.RepoName).Index(1);

                this.Map(m => m.Data.Coordinates.Count).Name("NrLocations").Index(15);
                this.Map(m => m.Data.OldCountries.Count).Name("NrOldHiCountries").Index(17);
                this.Map(m => m.Data.NewCountries.Count).Name("NrNewHiCountries").Index(18);

                this.Map(m => m.Metrics.Dispersion.VarGeoDistance).Index(50);

                this.Map(m => m.Metrics.Dispersion.OldVariancePdi).Index(60);
                this.Map(m => m.Metrics.Dispersion.OldVarianceIdv).Index(61);
                this.Map(m => m.Metrics.Dispersion.OldVarianceMas).Index(62);
                this.Map(m => m.Metrics.Dispersion.OldVarianceUai).Index(63);
                this.Map(m => m.Metrics.Dispersion.OldVarCulDistance).Index(64);

                this.Map(m => m.Metrics.Dispersion.NewVariancePdi).Index(65);
                this.Map(m => m.Metrics.Dispersion.NewVarianceIdv).Index(66);
                this.Map(m => m.Metrics.Dispersion.NewVarianceMas).Index(67);
                this.Map(m => m.Metrics.Dispersion.NewVarianceUai).Index(68);
                this.Map(m => m.Metrics.Dispersion.NewVarCulDistance).Index(69);

                this.Map(m => m.Characteristics.OldDispersion).Index(200);
                this.Map(m => m.Characteristics.NewDispersion).Index(201);

                // EXTRA VARIABLES FOR COMPARIONS BETWEEN YOSHI AND YOSHI 2
                this.Map(m => m.Metrics.Dispersion.AvgGeoDistance).Index(340);
                this.Map(m => m.Metrics.Dispersion.OldAvgCulDispersion).Index(350);
                this.Map(m => m.Metrics.Dispersion.NewAvgCulDispersion).Index(355);
            }
        }
    }
}
