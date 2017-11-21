using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Remoting
{
    public class Message<T>
    {
        public Message(T body, string id = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
            Body = body;
            Properties = new Dictionary<string, string>();
            TypeName = typeof(T).AssemblyQualifiedName;
        }

        public string Id { get; }

        public DateTime Created { get; }

        public T Body { get; }

        public string TypeName { get; }

        public IDictionary<string, string> Properties { get; }
    }
}