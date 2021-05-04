using System.Collections.Generic;
using YOSHI.CommunityData;
using YOSHI.Util;

namespace YOSHI.CharacteristicProcessorNS
{
    public static partial class CharacteristicProcessor
    {
        /// <summary>
        /// A method that computes several metrics used to measure community dispersion. It modifies the given community.
        /// </summary>
        /// <param name="community">The community for which we need to compute the dispersion.</param>
        private static void ComputeDispersion(Community community)
        {
            community.Metrics.Dispersion.MeanGeographicalDistance = MeanGeographicalDistance(community.Data.Coordinates);
            community.Characteristics.Dispersion = community.Metrics.Dispersion.MeanGeographicalDistance;
        }

        /// <summary>
        /// Given a list of coordinates, this method computes the average geographical (spherical) distance by first 
        /// computing the medium spherical distance for each coordinate to all other coordinates and then taking its 
        /// average. 
        /// </summary>
        /// <param name="coordinates">A list of coordinates for which we want to compute the average geographical
        /// distance.</param>
        /// <returns>The average geographical distance between the given list of coordinates.</returns>
        private static double MeanGeographicalDistance(List<GeoCoordinate> coordinates)
        {
            // NOTE: threshold (percentage) for number of coordinates should be set in DataRetriever

            // sum of medium distances in km
            double sumDistances = 0.0;

            // Compute the medium distance for each distinct pair of coordinates in the given list of coordinates
            for (int i = 0; i < coordinates.Count; i++)
            {
                GeoCoordinate coordinateA = coordinates[i];
                double mediumDistance = 0;
                for (int j = 0; j < coordinates.Count; j++)
                {
                    if (i != j)
                    {
                        GeoCoordinate coordinateB = coordinates[j];
                        // NOTE: Vincenty is faster than spherical, but takes longer. Based on processing times, may
                        // want to swap the distance method
                        mediumDistance += coordinateA.VincentyDistance(coordinateB);
                    }
                }
                mediumDistance = (double)mediumDistance / (coordinates.Count - 1);
                // converted to km
                sumDistances += mediumDistance / 1000;
            }

            return (double)sumDistances / coordinates.Count;
        }
    }
}