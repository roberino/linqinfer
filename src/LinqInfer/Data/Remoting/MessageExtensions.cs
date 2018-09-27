using System.IO;
using System.Xml.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Remoting
{
    public static class MessageExtensions
    {
        public static Message AsMessage<T>(this T body, object id = null)
            where T : IExportableAsDataDocument, IImportableFromDataDocument
        {
            var message = new Message(id?.ToString());

            message.Properties["_Type"] = typeof(T).AssemblyQualifiedName;

            var xml = body.ExportData().ExportAsXml();

            using(var ms = new MemoryStream())
            {
                xml.Save(ms);
                ms.Flush();

                message.Body = ms.ToArray();
            }

            return message;
        }

        public static T GetBody<T>(this Message message)
            where T : IImportableFromDataDocument, new()
        {
            if (message.Body == null) return default(T);

            using (var ms = new MemoryStream(message.Body))
            {
                var xml = XDocument.Load(ms);

                var doc = new PortableDataDocument(xml);

                var body = new T();

                body.ImportData(doc);

                return body;
            }
        }
    }
}