using LinqInfer.Utility;

namespace LinqInfer.Text
{
    public enum TextMimeType
    {
        [Description("text")]
        Default,
        [Description("text/plain")]
        Plain,
        [Description("text/xml")]
        Xml,
        [Description("text/xml")]
        Html,
        [Description("application/json")]
        Json
    }
}
