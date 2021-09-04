using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace YOSHI
{
    /// <summary>
    /// Class responsible for the Old Hofstede Indices. 
    /// </summary>
    public static class OldHI
    {
        // The old Hofstede indices used by YOSHI missed values for:
        //Canada
        //Russia
        //Vietnam
        //Bulgaria
        //Ukraine
        //Belarus
        //Slovakia
        //Sri Lanka
        //Eswatini
        //Croatia
        //Morocco
        //Namibia
        //Macao SAR
        //Romania
        //Iceland

        public readonly static Dictionary<
            string, (int Pdi, int Idv, int Mas, int Uai)> Hofstede
        = new Dictionary<string, (int Pdi, int Idv, int Mas, int Uai)
            >(new CaseAccentInsensitiveEqualityComparer())
        {
            { "argentina", (49, 46, 56, 86) },
            { "australia", (36, 90, 61, 51) },
            { "austria", (11, 55, 79, 70) },
            { "belgium", (65, 75, 54, 94) },
            { "brazil", (69, 38, 49, 76) },
            { "chile", (63, 23, 28, 86) },
            { "china", (80, 20, 66, 40) },
            { "colombia", (67, 13, 64, 80) },
            { "costa rica", (35, 15, 21, 86) },
            { "czechia", (57, 58, 57, 74) }, // Updated from czech republic
            { "denmark", (18, 74, 16, 23) },
            { "ecuador", (78, 8, 63, 67) },
            { "egypt", (80, 38, 52, 68) },
            { "el salvador", (66, 19, 40, 94) },
            { "ethiopia", (64, 27, 41, 52) },
            { "finland", (33, 63, 26, 59) },
            { "france", (68, 71, 43, 86) },
            { "germany", (35, 67, 66, 65) },
            { "ghana", (77, 20, 46, 54) },
            { "greece", (60, 35, 57, 112) },
            { "guatemala", (95, 6, 37, 101) },
            { "hong kong sar", (68, 25, 57, 29) }, // Updated from hong kong
            { "hungary", (46, 55, 88, 82) },
            { "india", (77, 48, 56, 40) },
            { "indonesia", (78, 14, 46, 48) },
            { "iran", (58, 41, 43, 59) },
            { "iraq", (80, 38, 52, 68) },
            { "ireland", (28, 70, 68, 35) },
            { "israel", (13, 54, 47, 81) },
            { "italy", (50, 76, 70, 75) },
            { "jamaica", (45, 39, 68, 13) },
            { "japan", (54, 46, 95, 92) },
            { "kenya", (64, 27, 41, 52) },
            { "kuwait", (80, 38, 52, 68) },
            { "lebanon", (80, 38, 52, 68) },
            { "libya", (80, 38, 52, 68) },
            { "malaysia", (104, 26, 50, 36) },
            { "mexico", (81, 30, 69, 82) },
            { "netherlands", (38, 80, 14, 53) },
            { "new zealand", (22, 79, 58, 49) },
            { "nigeria", (77, 20, 46, 54) },
            { "norway", (31, 69, 8, 50) },
            { "pakistan", (55, 14, 50, 70) },
            { "panama", (95, 11, 44, 86) },
            { "peru", (64, 16, 42, 87) },
            { "philippines", (94, 32, 64, 44) },
            { "poland", (68, 60, 64, 93) },
            { "portugal", (63, 27, 31, 104) },
            { "saudi arabia", (80, 38, 52, 68) },
            { "sierra leone", (77, 20, 46, 54) },
            { "singapore", (74, 20, 48, 8) },
            { "south africa", (49, 65, 63, 49) },
            { "south korea", (60, 18, 39, 85) },
            { "spain", (57, 51, 42, 86) },
            { "sweden", (31, 71, 5, 29) },
            { "switzerland", (34, 68, 70, 58) },
            { "taiwan", (58, 17, 45, 69) },
            { "tanzania", (64, 27, 41, 52) },
            { "thailand", (64, 20, 34, 64) },
            { "turkey", (66, 37, 45, 85) },
            { "united arab emirates", (80, 38, 52, 68) },
            { "united kingdom", (35, 89, 66, 35) },
            { "united states", (40, 91, 62, 46) },
            { "uruguay", (61, 36, 38, 100) },
            { "venezuela", (81, 12, 73, 76) },
            { "zambia", (64, 27, 41, 52) },
            // Below are all variations of "united states" (except vancouver)
            //{ "san francisco", (40, 91, 62, 46) },
            //{ "usa", (40, 91, 62, 46) },
            //{ "california", (40, 91, 62, 46) },
            //{ "boston", (40, 91, 62, 46) },
            //{ "texas", (40, 91, 62, 46) },
            //{ "atlanta", (40, 91, 62, 46) },
            // City in Canada, our tool finds Canada, hence does not use these
            // values once. We cannot simply replace "vancouver" with Canada.
            //{ "vancouver", (40, 91, 62, 46) } 
            //{ "mountain view", (40, 91, 62, 46) },
            //{ "chicago", (40, 91, 62, 46) },
            //{ "seattle", (40, 91, 62, 46) },
            //{ "menlo park", (40, 91, 62, 46) },
        };

        /// <summary>
        /// Equality comparer of strings that ignores lower/uppercase and accents
        /// (diacritics). Note that if the Hofstede dictionary was not initialized
        /// with this equality comparer, it would likely fail to identify "são tomé
        /// and princípe" or inconsistencies.
        /// </summary>
        public class CaseAccentInsensitiveEqualityComparer
            : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return string.Compare(x, y,
                    CultureInfo.InvariantCulture,
                    CompareOptions.IgnoreNonSpace 
                    | CompareOptions.IgnoreCase) == 0;
            }

            public int GetHashCode(string obj)
            {
                return obj != null ?
                    this.RemoveDiacritics(obj).ToUpperInvariant().GetHashCode()
                    : 0;
            }

            private string RemoveDiacritics(string text)
            {
                return string.Concat(
                    text.Normalize(NormalizationForm.FormD)
                    .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                                  UnicodeCategory.NonSpacingMark)
                  ).Normalize(NormalizationForm.FormC);
            }
        }
    }
}