using System;
namespace yoshi_revision.src.Util
{
    public class GeocoderRateLimitException : Exception
    {
        public GeocoderRateLimitException()
        {
        }
        public GeocoderRateLimitException(string message)
            : base(message)
        {
        }
        public GeocoderRateLimitException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
