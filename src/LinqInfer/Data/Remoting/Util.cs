using System;

namespace LinqInfer.Data.Remoting
{
    internal static class Util
    {
        public static string GenerateId()
        {
            return Environment.MachineName + '/' + Guid.NewGuid().ToString("N");
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
