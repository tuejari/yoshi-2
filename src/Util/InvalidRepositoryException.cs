using System;

namespace yoshi_revision.src.Util
{

    public class InvalidRepositoryException : Exception
    {
        public InvalidRepositoryException()
        {
        }

        public InvalidRepositoryException(string message)
            : base(message)
        {
        }

        public InvalidRepositoryException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
