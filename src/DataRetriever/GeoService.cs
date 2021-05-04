using Geocoding.Microsoft;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YOSHI.Util;

namespace YOSHI.DataRetrieverNS
{
    public static class GeoService
    {
        public static int BingRequestsLeft { get; set; } = 50000;
        private static readonly BingMapsGeocoder Geocoder =
            new BingMapsGeocoder(Environment.GetEnvironmentVariable("YOSHI_BingMapsKey"));

        /// <summary>
        /// A method that takes a list of users and computes the coordinates for all members. Users that have not 
        /// specified their locations or cause exceptions are skipped.
        /// </summary>
        /// <param name="members">A list of members to compute the coordinates from</param>
        /// <param name="repoName">The repository name, used in exception handling</param>
        /// <returns>A list of coordinates for the passed list of members</returns>
        /// <exception cref="YOSHI.Util.GeocoderRateLimitException">Thrown when the Bing Rate Limit is exceeded.</exception>
        public static async Task<List<GeoCoordinate>> RetrieveMemberCoordinates(List<User> members, string repoName)
        {
            List<GeoCoordinate> coordinates = new List<GeoCoordinate>();

            // NOTE: We loop over all user objects instead of usernames to access location data
            foreach (User member in members)
            {
                // Retrieve the member's coordinates
                try
                {
                    if (member.Location != null)
                    {
                        GeoCoordinate coordinate = await GetLongitudeLatitude(member.Location);
                        if (coordinate != null)
                        {
                            coordinates.Add(coordinate);
                        }
                    }
                }
                catch (BingGeocodingException e)
                {
                    // Continue with the next user if this user was causing an exception
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Could not retrieve the location from {0} in repo {1}", member.Login, repoName);
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                    continue;
                }
                catch (GeocoderRateLimitException)
                {
                    throw;
                }
            }
            return coordinates;
        }

        /// <summary>
        /// This method uses a Geocoding API to perform forward geocoding, i.e., enter an address and obtain coordinates.
        /// 
        /// Bing Maps TOU: https://www.microsoft.com/en-us/maps/product/terms-april-2011
        /// </summary>
        /// <param name="address">The address of which we want the coordinates.</param>
        /// <returns>A GeoCoordinate containing the longitude and latitude found from the given address.</returns>
        /// <exception cref="BingGeocodingException">Thrown when the returned status in MapLocationFinderResult is 
        /// anything but "Success".</exception>
        /// <exception cref="GeocoderRateLimitException">Thrown when the rate limit has been reached.</exception>
        private static async Task<GeoCoordinate> GetLongitudeLatitude(string address)
        {
            if (BingRequestsLeft > 50) // Give ourselves a small buffer to not go over the limit.
            {
                BingRequestsLeft--;
                // Note: MapLocationFinder does not throw exceptions, instead it returns a status. 
                try
                {
                    IEnumerable<BingAddress> resultAddresses = await Geocoder.GeocodeAsync(address);
                    BingAddress result = resultAddresses.FirstOrDefault();
                    if (result != null)
                    {
                        GeoCoordinate coordinate = new GeoCoordinate(result.Coordinates.Latitude, result.Coordinates.Longitude);
                        return coordinate;
                    }
                    else
                    {
                        throw new BingGeocodingException(new Exception("Result for address \"" + address + "\" is null"));
                    }
                }
                catch (BingGeocodingException)
                {
                    throw;
                }
            }
            else
            {
                throw new GeocoderRateLimitException("No more Bing Requests left.");
            }
        }
    }
}