using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            get { return int.Parse(Properties["Batch"] ?? "0"); }
            set { Properties["Batch"] = value.ToString(); }
        }
        public bool KeepAlive
        {
            get { return bool.Parse(Properties["KeepAlive"] ?? "False"); }
            set { Properties["KeepAlive"] = value.ToString(); }
        }
    }
}