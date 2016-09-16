﻿using System;

namespace LinqInfer.Data.Remoting
{
    public class DataBatch : BinaryVectorDocument
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
        public string OperationType
        {
            get { return Properties["OpType"]; }
            set { Properties["OpType"] = value; }
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

        private Uri Parse(string value)
        {
            if (value == null) return null;

            return new Uri(value);
        }
    }
}