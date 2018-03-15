using System.Xml.Linq;

namespace LinqInfer.Data
{
    public interface IXmlImportable
    {
        void ImportXml(XDocument xml);
    }
}