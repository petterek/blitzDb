using System;
using System.Runtime.Serialization;

namespace blitzdb
{
    [Serializable]
    internal class ConstructorParamnameNotFoundException : Exception
    {
        public ConstructorParamnameNotFoundException()
        {
        }

        public ConstructorParamnameNotFoundException(string message) : base(message)
        {
        }

        public ConstructorParamnameNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConstructorParamnameNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}