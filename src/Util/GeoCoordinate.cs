using System;
using System.Text.RegularExpressions;

namespace YOSHI.Util
{

    /**
     * This immutable class represents a geographic coordinate with a latitude and longitude value.
     * Note: We cannot access the native .NET GeoCoordinate class since the project is a Universal Windows APP. Instead
     * we use a converted (from Java to C#) GeoCoordinate class from the previous YOSHI code.
     */
    public class GeoCoordinate : IComparable<GeoCoordinate>
    {
        /**
	     * Equatorial radius of earth is required for distance computation.
	     */
        public static readonly double EQUATORIALRADIUS = 6378137.0;

        /**
	     * Polar radius of earth is required for distance computation.
	     */
        public static readonly double POLARRADIUS = 6356752.3142;

        /**
	     * The flattening factor of the earth's ellipsoid is required for distance computation.
	     */
        public static readonly double INVERSEFLATTENING = 298.257223563;

        /**
	     * The multiplication factor to convert from double to int.
	     */
        public static readonly double FACTOR_DOUBLE_TO_INT = 1000000;

        /**
	     * The largest possible latitude value.
	     */
        public static readonly double LATITUDE_MAX = 90;

        /**
	     * The smallest possible latitude value.
	     */
        public static readonly double LATITUDE_MIN = -90;

        /**
	     * The largest possible longitude value.
	     */
        public static readonly double LONGITUDE_MAX = 180;

        /**
	     * The smallest possible longitude value.
	     */
        public static readonly double LONGITUDE_MIN = -180;

        /**
	     * The internal latitude value.
	     */
        private readonly double latitude;

        /**
	     * The internal longitude value.
	     */
        private readonly double longitude;

        /**
	     * The RegEx pattern to read WKT points
	     */
        private static readonly Regex wktPointPattern = new Regex(".*POINT\\s?\\(([\\d\\.]+)\\s([\\d\\.]+)\\).*");

        /**
	     * Constructs a new GeoCoordinate with the given latitude and longitude values, measured in
	     * degrees.
	     * 
	     * @param latitude
	     *            the latitude value in degrees.
	     * @param longitude
	     *            the longitude value in degrees.
	     * @throws ArgumentException
	     *             if the latitude or longitude value is invalid.
	     */
        public GeoCoordinate(double latitude, double longitude)
        {
            this.latitude = ValidateLatitude(latitude);
            this.longitude = ValidateLongitude(longitude);
        }

        /**
	     * Constructs a new GeoCoordinate with the given latitude and longitude values, measured in
	     * microdegrees.
	     * 
	     * @param latitudeE6
	     *            the latitude value in microdegrees.
	     * @param longitudeE6
	     *            the longitude value in microdegrees.
	     * @throws ArgumentException
	     *             if the latitude or longitude value is invalid.
	     */
        public GeoCoordinate(int latitudeE6, int longitudeE6)
        {
            this.latitude = ValidateLatitude(IntToDouble(latitudeE6));
            this.longitude = ValidateLongitude(IntToDouble(longitudeE6));
        }

        /**
	     * Constructs a new GeoCoordinate from a Well-Known-Text (WKT) representation of a point For
	     * example: POINT(13.4125 52.52235)
	     * 
	     * WKT is used in PostGIS and other spatial databases
	     * 
	     * @param wellKnownText
	     *            is the WKT point which describes the new GeoCoordinate, this needs to be in
	     *            degrees using a WGS84 representation. The coordinate order in the POINT is
	     *            defined as POINT(long lat)
	     */
        public GeoCoordinate(string wellKnownText)
        {
            Match m = wktPointPattern.Match(wellKnownText);
            this.longitude = ValidateLongitude(Convert.ToDouble(m.Groups[1]));
            this.latitude = ValidateLatitude(Convert.ToDouble(m.Groups[2]));
        }

        /**
	     * Constructs a new GeoCoordinate from a comma-separated String containing latitude and
	     * longitude values (also ';', ':' and whitespace work as separator). First latitude and
	     * longitude are interpreted as measured in degrees. If the coordinate is invalid, it is
	     * tried to interpret values as measured in microdegrees.
	     * 
	     * @param latLonString
	     *            the String containing the latitude and longitude values
	     * @return the GeoCoordinate
	     * @throws ArgumentException
	     *             if the latLonString could not be interpreted as a coordinate
	     */
        public static GeoCoordinate FromString(string latLonString)
        {
            string[] splitted = Regex.Split(latLonString, "[,;:\\s]");
            if (splitted.Length != 2)
            {
                throw new ArgumentException("cannot read coordinate, not a valid format");
            }

            double latitude = Convert.ToDouble(splitted[0]);
            double longitude = Convert.ToDouble(splitted[1]);
            try
            {
                return new GeoCoordinate(latitude, longitude);
            }
            catch (ArgumentException)
            {
                return new GeoCoordinate(DoubleToInt(latitude),
                        DoubleToInt(longitude));
            }
        }

        /**
	     * Checks the given latitude value and throws an exception if the value is out of range.
	     * 
	     * @param lat
	     *            the latitude value that should be checked.
	     * @return the latitude value.
	     * @throws ArgumentException
	     *             if the latitude value is < LATITUDE_MIN or > LATITUDE_MAX.
	     */
        public static double ValidateLatitude(double lat)
        {
            if (lat < LATITUDE_MIN)
            {
                throw new ArgumentException("invalid latitude value: " + lat);
            }
            else if (lat > LATITUDE_MAX)
            {
                throw new ArgumentException("invalid latitude value: " + lat);
            }
            else
            {
                return lat;
            }
        }

        /**
	     * Checks the given longitude value and throws an exception if the value is out of range.
	     * 
	     * @param lon
	     *            the longitude value that should be checked.
	     * @return the longitude value.
	     * @throws ArgumentException
	     *             if the longitude value is < LONGITUDE_MIN or > LONGITUDE_MAX.
	     */
        public static double ValidateLongitude(double lon)
        {
            if (lon < LONGITUDE_MIN)
            {
                throw new ArgumentException("invalid longitude value: " + lon);
            }
            else if (lon > LONGITUDE_MAX)
            {
                throw new ArgumentException("invalid longitude value: " + lon);
            }
            else
            {
                return lon;
            }
        }

        /**
	     * Returns the latitude value of this coordinate.
	     * 
	     * @return the latitude value of this coordinate.
	     */
        public double GetLatitude()
        {
            return this.latitude;
        }

        /**
	     * Returns the longitude value of this coordinate.
	     * 
	     * @return the longitude value of this coordinate.
	     */
        public double GetLongitude()
        {
            return this.longitude;
        }

        /**
	     * Returns the latitude value in microdegrees of this coordinate.
	     * 
	     * @return the latitude value in microdegrees of this coordinate.
	     */
        public int GetLatitudeE6()
        {
            return DoubleToInt(this.latitude);
        }

        /**
	     * Returns the longitude value in microdegrees of this coordinate.
	     * 
	     * @return the longitude value in microdegrees of this coordinate.
	     */
        public int GetLongitudeE6()
        {
            return DoubleToInt(this.longitude);
        }

        /**
	     * Calculate the spherical distance from this GeoCoordinate to another
	     * 
	     * Use vincentyDistance for more accuracy but less performance
	     * 
	     * @param other
	     *            The GeoCoordinate to calculate the distance to
	     * @return the distance in meters as a double
	     */
        public double SphericalDistance(GeoCoordinate other)
        {
            return SphericalDistance(this, other);
        }

        /**
	     * Calculate the spherical distance between two GeoCoordinates in meters using the Haversine
	     * formula
	     * 
	     * This calculation is done using the assumption, that the earth is a sphere, it is not
	     * though. If you need a higher precision and can afford a longer execution time you might
	     * want to use vincentyDistance
	     * 
	     * @param gc1
	     *            first GeoCoordinate
	     * @param gc2
	     *            second GeoCoordinate
	     * @return distance in meters as a double
	     * @throws ArgumentException
	     *             if one of the arguments is null
	     */
        public static double SphericalDistance(GeoCoordinate gc1, GeoCoordinate gc2)
        {
            if (gc1 == null || gc2 == null)
            {
                throw new ArgumentException(
                        "The GeoCoordinates for distance calculations may not be null.");
            }

            return SphericalDistance(gc1.GetLongitude(), gc1.GetLatitude(), gc2.GetLongitude(), gc2.GetLatitude());
        }

        /**
         * Calculate the spherical distance between two GeoCoordinates in meters using the Haversine
         * formula.
         * 
         * This calculation is done using the assumption, that the earth is a sphere, it is not
         * though. If you need a higher precision and can afford a longer execution time you might
         * want to use vincentyDistance
         * 
         * @param lon1
         *            longitude of first coordinate
         * @param lat1
         *            latitude of first coordinate
         * @param lon2
         *            longitude of second coordinate
         * @param lat2
         *            latitude of second coordinate
         * 
         * @return distance in meters as a double
         * @throws ArgumentException
         *             if one of the arguments is null
         */
        public static double SphericalDistance(double lon1, double lat1, double lon2, double lat2)
        {
            double dLat = ConvertToRadians(lat2 - lat1);
            double dLon = ConvertToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ConvertToRadians(lat1))
                    * Math.Cos(ConvertToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return c * EQUATORIALRADIUS;
        }

        /**
         * Distance based on the assumption that the earth is a sphere.
         * 
         * @param lon1
         *            longitude of 1st coordinate.
         * @param lat1
         *            latitude of 1st coordinate.
         * @param lon2
         *            longitude of 2nd coordinate.
         * @param lat2
         *            latitude of 2nd coordinate.
         * @return distance in meters.
         */
        public static double SphericalDistance(int lon1, int lat1, int lon2, int lat2)
        {
            return SphericalDistance(IntToDouble(lon1), IntToDouble(lat1), IntToDouble(lon2),
                    IntToDouble(lat2));
        }

        /**
         * Calculate the spherical distance from this GeoCoordinate to another
         * 
         * Use "distance" for faster computation with less accuracy
         * 
         * @param other
         *            The GeoCoordinate to calculate the distance to
         * @return the distance in meters as a double
         */
        public double VincentyDistance(GeoCoordinate other)
        {
            return VincentyDistance(this, other);
        }

        /**
         * Calculates geodetic distance between two GeoCoordinates using Vincenty inverse formula
         * for ellipsoids. This is very accurate but consumes more resources and time than the
         * sphericalDistance method
         * 
         * Adaptation of Chriss Veness' JavaScript Code on
         * http://www.movable-type.co.uk/scripts/latlong-vincenty.html
         * 
         * Paper: Vincenty inverse formula - T Vincenty, "Direct and Inverse Solutions of Geodesics
         * on the Ellipsoid with application of nested equations", Survey Review, vol XXII no 176,
         * 1975 (http://www.ngs.noaa.gov/PUBS_LIB/inverse.pdf)
         * 
         * @param gc1
         *            first GeoCoordinate
         * @param gc2
         *            second GeoCoordinate
         * 
         * @return distance in meters between points as a double
         */
        public static double VincentyDistance(GeoCoordinate gc1, GeoCoordinate gc2)
        {
            double f = 1 / INVERSEFLATTENING;
            double L = ConvertToRadians(gc2.GetLongitude() - gc1.GetLongitude());
            double U1 = Math.Atan((1 - f) * Math.Tan(ConvertToRadians(gc1.GetLatitude())));
            double U2 = Math.Atan((1 - f) * Math.Tan(ConvertToRadians(gc2.GetLatitude())));
            double sinU1 = Math.Sin(U1), cosU1 = Math.Cos(U1);
            double sinU2 = Math.Sin(U2), cosU2 = Math.Cos(U2);

            double lambda = L, lambdaP, iterLimit = 100;
            double cosSqAlpha, sinSigma, cosSigma, cos2SigmaM, sigma;
            do
            {
                double sinLambda = Math.Sin(lambda);
                double cosLambda = Math.Cos(lambda);
                sinSigma = Math.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda)
                        + (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda)
                        * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));
                if (sinSigma == 0)
                {
                    return 0; // co-incident points
                }

                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
                sigma = Math.Atan2(sinSigma, cosSigma);
                double sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSqAlpha = 1 - sinAlpha * sinAlpha;
                cos2SigmaM = cosSqAlpha != 0 ? cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha : 0;
                double C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
                lambdaP = lambda;
                lambda = L
                        + (1 - C)
                        * f
                        * sinAlpha
                        * (sigma + C * sinSigma
                                * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
            } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

            if (iterLimit == 0)
            {
                return 0; // formula failed to converge
            }

            double uSq = cosSqAlpha
                    * (Math.Pow(EQUATORIALRADIUS, 2) - Math.Pow(POLARRADIUS, 2))
                    / Math.Pow(POLARRADIUS, 2);
            double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
            double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
            double deltaSigma = B
                    * sinSigma
                    * (cos2SigmaM + B
                            / 4
                            * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) - B / 6 * cos2SigmaM
                                    * (-3 + 4 * sinSigma * sinSigma)
                                    * (-3 + 4 * cos2SigmaM * cos2SigmaM)));
            double s = POLARRADIUS * A * (sigma - deltaSigma);
            return s;
        }

        /**
         * Calculate the amount of degrees of latitude for a given distance in meters.
         * 
         * @param meters
         *            distance in meters
         * @return latitude degrees
         */
        public static double LatitudeDistance(int meters)
        {
            return (meters * 360) / (2 * Math.PI * EQUATORIALRADIUS);
        }

        /**
         * Calculate the amount of degrees of longitude for a given distance in meters.
         * 
         * @param meters
         *            distance in meters
         * @param latitude
         *            the latitude at which the calculation should be performed
         * @return longitude degrees
         */
        public static double LongitudeDistance(int meters, double latitude)
        {
            return (meters * 360)
                    / (2 * Math.PI * EQUATORIALRADIUS * Math.Cos(ConvertToRadians(latitude)));
        }

        /**
         * Calculate the amount of degrees of longitude for a given distance in meters.
         * 
         * @param meters
         *            distance in meters
         * @param latitude
         *            the latitude at which the calculation should be performed
         * @return longitude degrees
         */
        public static double LongitudeDistance(int meters, int latitude)
        {
            return (meters * 360)
                    / (2 * Math.PI * EQUATORIALRADIUS * Math.Cos(ConvertToRadians(IntToDouble(latitude))));
        }

        /**
         * Converts a coordinate from degrees to microdegrees.
         * 
         * @param coordinate
         *            the coordinate in degrees.
         * @return the coordinate in microdegrees.
         */
        public static int DoubleToInt(double coordinate)
        {
            return (int)(coordinate * FACTOR_DOUBLE_TO_INT);
        }

        /**
         * Converts a coordinate from microdegrees to degrees.
         * 
         * @param coordinate
         *            the coordinate in microdegrees.
         * @return the coordinate in degrees.
         */
        public static double IntToDouble(int coordinate)
        {
            return coordinate / FACTOR_DOUBLE_TO_INT;
        }

        /**
         * Converts an angle in degrees to radians. (Added since C# misses this in their Math class)
         * 
         * @param angle
         *          the angle in degrees.
         * @return the angle in radians.
         */
        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            else if (!(obj is GeoCoordinate coordinate))
            {
                return false;
            }
            else
            {
                GeoCoordinate other = coordinate;
                if (this.latitude != other.latitude)
                {
                    return false;
                }
                else if (this.longitude != other.longitude)
                {
                    return false;
                }
                return true;
            }
        }

        /**
	     * This method is necessary for inserting GeoCoordinates into tree data structures.
	     */
        public int CompareTo(GeoCoordinate geoCoordinate)
        {
            if (this.latitude > geoCoordinate.latitude || this.longitude > geoCoordinate.longitude)
            {
                return 1;
            }
            else if (this.latitude < geoCoordinate.latitude
                  || this.longitude < geoCoordinate.longitude)
            {
                return -1;
            }
            return 0;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            long temp;
            temp = BitConverter.DoubleToInt64Bits(this.latitude);
            result = prime * result + (int)(temp ^ ((uint)temp >> 32));
            temp = BitConverter.DoubleToInt64Bits(this.longitude);
            result = prime * result + (int)(temp ^ ((uint)temp >> 32));
            return result;
        }

        public override string ToString()
        {
            return "latitude: " + this.latitude + ", longitude: " + this.longitude;
        }
    }
}
