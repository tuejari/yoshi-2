using YOSHI.CommunityData;

namespace YOSHI.CharacteristicProcessorNS
{
    /// <summary>
    /// This class is responsible for using the retrieved GitHub data and computing several metrics and then values for
    /// the corresponding characteristics. This partial class is specifically responsible for the miscellaneous 
    /// characteristics.
    /// </summary>
    public static partial class CharacteristicProcessor
    {
        /// <summary>
        /// A method that calls all specific ComputeCharacteristic methods other than ComputeStructure
        /// </summary>
        /// <param name="community">The community for which we need to compute the characteristics.</param>
        public static void ComputeMiscellaneousCharacteristics(Community community)
        {
            ComputeDispersion(community);
            ComputeFormality(community);
            ComputeEngagement(community);
            ComputeLongevity(community);
            //CohesionProcessor.ComputeCohesion(community); // Not yet implemented
        }
    }
}
