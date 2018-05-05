using System.Xml.Linq;

namespace LinqInfer.Data.Serialisation
{
    public interface IXmlExportable
    {
        XDocument ExportAsXml();
    }
}