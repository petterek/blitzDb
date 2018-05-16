using System;
using System.Runtime.Serialization;

namespace blitzdb
{
    [Serializable]
    internal class MultipleConstructorsNotSupportedException : Exception
    {
        public MultipleConstructorsNotSupportedException()
        {
        }

        public MultipleConstructorsNotSupportedException(string message) : base(message)
        {
        }

        public MultipleConstructorsNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MultipleConstructorsNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}