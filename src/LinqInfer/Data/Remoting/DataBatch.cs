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
            get { return Properties["Id"]; }
            set { Properties["Id"] = value; }
        }
        public string ClientId
        {
            get { return Properties["ClientId"]; }
            set { Properties["ClientId"] = value; }
        }
        public string Path
        {
            get { return Properties["Path"]; }
            set { Properties["Path"] = value; }
        }
        public Verb Verb
        {
            get { return PropertyOrDefault("Verb", Verb.Default); }
            set { Properties["Verb"] = value.ToString(); }
        }
        public int BatchNum
        {
            get { return PropertyOrDefault("Batch", 0); }
            set { Properties["Batch"] = value.ToString(); }
        }
        public bool KeepAlive
        {
            get { return PropertyOrDefault("KeepAlive", false); }
            set { Properties["KeepAlive"] = value.ToString(); }
        }
        public bool SendResponse
        {
            get { return PropertyOrDefault("SendResponse", false); }
            set { Properties["SendResponse"] = value.ToString(); }
        }
        public Uri ForwardingEndpoint
        {
            get { return Parse(PropertyOrDefault("ForwardTo", null as string)); }
            set { Properties["ForwardTo"] = value.ToString(); }
        }

        Uri Parse(string value)
        {
            if (value == null) return null;

            return new Uri(value);
        }
    }
}