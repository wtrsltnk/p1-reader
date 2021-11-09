using System;

namespace P1Reader.Domain
{
    public class StorageWriteException : 
        Exception
    {
        public StorageWriteException(
            string message)
            : base(message)
        { }

        public StorageWriteException(
            string message,
            Exception innerException)
            : base(message, 
                  innerException)
        { }
    }
}
