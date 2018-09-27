namespace LinqInfer.Text.Http
{
    static class TextMimeTypeParser
    {
        public static TextMimeType Parse(string mimeType)
        {
            switch (mimeType)
            {
                case "text/html":
                    return TextMimeType.Html;
                case "text/plain":
                    return TextMimeType.Plain;
                case "text/xml":
                    return TextMimeType.Xml;
                case "text/json":
                case "application/json":
                    return TextMimeType.Json;
                default:
                    return TextMimeType.Default;
            }
        }
    }
}
