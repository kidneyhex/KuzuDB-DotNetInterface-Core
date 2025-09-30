using System;

namespace KuzuDB.OOWrapper
{
    /// <summary>
    /// Exception thrown when KuzuDB operations fail.
    /// </summary>
    public class KuzuException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the KuzuException class.
        /// </summary>
        public KuzuException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the KuzuException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public KuzuException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KuzuException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public KuzuException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}