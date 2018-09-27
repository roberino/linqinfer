using System.Xml.Linq;

namespace LinqInfer.Data.Serialisation
{
    public interface IXmlImportable
    {
        void ImportXml(XDocument xml);
    }
}