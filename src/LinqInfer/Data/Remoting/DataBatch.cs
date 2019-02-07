using System;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Remoting
{
    public class DataBatch : PortableDataDocument
    {
        internal DataBatch()
        {
        }

        public string Id
        {
            get => Properties[nameof(Id)];
            set => Properties[nameof(Id)] = value;
        }
        public string ClientId
        {
            get => Properties[nameof(ClientId)];
            set => Properties[nameof(ClientId)] = value;
        }
        public string Path
        {
            get => Properties[nameof(Path)];
            set => Properties[nameof(Path)] = value;
        }
        public Verb Verb
        {
            get => PropertyOrDefault(nameof(Verb), Verb.Default);
            set => Properties[nameof(Verb)] = value.ToString();
        }
        public int BatchNum
        {
            get => PropertyOrDefault("Batch", 0);
            set => Properties["Batch"] = value.ToString();
        }
        public bool KeepAlive
        {
            get => PropertyOrDefault(nameof(KeepAlive), false);
            set => Properties[nameof(KeepAlive)] = value.ToString();
        }
        public bool SendResponse
        {
            get => PropertyOrDefault(nameof(SendResponse), false);
            set => Properties[nameof(SendResponse)] = value.ToString();
        }
        public Uri ForwardingEndpoint
        {
            get => Parse(PropertyOrDefault("ForwardTo", null as string));
            set => Properties["ForwardTo"] = value.ToString();
        }

        Uri Parse(string value)
        {
            if (value == null) return null;

            return new Uri(value);
        }
    }
}