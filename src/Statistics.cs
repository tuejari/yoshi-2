using System;
using System.Collections.Generic;
using System.Linq;

namespace YOSHI
{
    /// <summary>
    /// Class that implements statistics computations. Cannot implement a generic 
    /// method that takes numerics in c#:
    /// See https://stackoverflow.com/q/22261510/
    /// Therefore we repeat code.
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Easy access method to compute the variance of a list.
        /// </summary>
        /// <param name="list">Non-empty List to compute the variance of.</param>
        /// <returns>The variance of the list.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when list is empty.</exception>
        public static double ComputeVariance(List<double> list)
        {
            if (list.Count > 0)
            {
                double mean = list.Average();
                double temp = 0;
                foreach (double value in list)
                {
                    temp += (value - mean) * (value - mean);
                }
                return temp / list.Count;
            }
            else
            {
                throw new InvalidOperationException("List contains no elements");
            }
        }

        /// <summary>
        /// Compute the standard deviation of a list of doubles.
        /// </summary>
        /// <param name="list">Non-empty list to compute the standard deviation of.
        /// </param>
        /// <returns>The standard deviation of the list.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when list is empty.</exception>
        public static double ComputeStandardDeviation(List<double> list)
        {
            return list.Count > 0 ? 
                Math.Sqrt(ComputeVariance(list)) 
                : throw new InvalidOperationException("List contains no elements");
        }
    }
}