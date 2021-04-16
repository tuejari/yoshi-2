using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Services.Maps;
using yoshi_revision.src.Util;

namespace yoshi_revision.src
{
    public class GeoService
    {

        /// <summary>
        /// This method uses a Geocoding API to perform forward geocoding, i.e., enter an address and obtain coordinates.
        /// 
        /// Bing Maps TOU: https://www.microsoft.com/en-us/maps/product/terms-april-2011
        /// </summary>
        /// <param name="address">The address of which we want the coordinates.</param>
        /// <returns>A GeoCoordinate containing the longitude and latitude found from the given address.</returns>
        public static async Task<GeoCoordinate> GetLongitudeLatitude(string address)
        {
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAsync(address, null, 1);
            GeoCoordinate coordinate = new GeoCoordinate(
                result.Locations[0].Point.Position.Latitude, 
                result.Locations[0].Point.Position.Longitude
                );
            return coordinate;
        }
    }
}
