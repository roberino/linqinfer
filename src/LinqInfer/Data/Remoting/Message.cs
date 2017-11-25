using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Remoting
{
    public class Message
    {
        public Message(string id = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
            Properties = new Dictionary<string, string>();
        }

        public string Id { get; }

        public DateTime Created { get; }

        public IDictionary<string, string> Properties { get; }

        public byte[] Body { get; set; }
    }
}