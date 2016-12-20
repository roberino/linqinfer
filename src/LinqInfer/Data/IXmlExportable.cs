using System.Xml.Linq;

namespace LinqInfer.Data
{
    public interface IXmlExportable
    {
        XDocument ExportAsXml();
    }
}