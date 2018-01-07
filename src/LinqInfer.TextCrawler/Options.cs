using CommandLine;

namespace LinqInfer.TextCrawler
{
    public class Options
    {
        [Option('u', "url", HelpText = "The root URL")]
        public string Url { get; set; }
    }
}