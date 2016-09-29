using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Data.Remoting
{
    internal static class Util
    {
        public static string GenerateId()
        {
            return Environment.MachineName + '-' + Guid.NewGuid().ToString("N");
        }

        internal static Uri ConvertProtocol(Uri uri, TransportProtocol protocol)
        {
            Contract.Ensures(protocol != TransportProtocol.None);
            return new Uri(protocol.ToString() + Uri.SchemeDelimiter + uri.Host + ':' + uri.Port + uri.PathAndQuery);
        }

        public static void ValidateUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (uri.Scheme != "tcp")
            {
                throw new ArgumentException("Only TCP scheme supported e.g. tcp://host:3211");
            }
        }
    }
}
