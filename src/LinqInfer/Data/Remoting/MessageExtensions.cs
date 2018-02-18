using System.IO;
using System.Xml.Linq;

namespace LinqInfer.Data.Remoting
{
    public static class MessageExtensions
    {
        public static Message AsMessage<T>(this T body, object id = null)
            where T : IExportableAsVectorDocument, IImportableAsVectorDocument
        {
            var message = new Message(id?.ToString());

            message.Properties["_Type"] = typeof(T).AssemblyQualifiedName;

            var xml = body.ToVectorDocument().ExportAsXml();

            using(var ms = new MemoryStream())
            {
                xml.Save(ms);
                ms.Flush();

                message.Body = ms.ToArray();
            }

            return message;
        }

        public static T GetBody<T>(this Message message)
            where T : IImportableAsVectorDocument, new()
        {
            if (message.Body == null) return default(T);

            using (var ms = new MemoryStream(message.Body))
            {
                var xml = XDocument.Load(ms);

                var doc = new BinaryVectorDocument(xml);

                var body = new T();

                body.FromVectorDocument(doc);

                return body;
            }
        }
    }
}