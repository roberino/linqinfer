using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LinqInfer.Text.Html
{
    class HtmlEntityMap
    {
        readonly static Regex _entRegex;
        readonly static IDictionary<string, int> _entityMap;
        readonly static List<string> _validXmlEntities;

        static HtmlEntityMap()
        {
            _entityMap = new Dictionary<string, int>();
            _entRegex = new Regex(@"\&(#?[a-z0-9]+);", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _validXmlEntities = new List<string>(new[] { "apos", "quot", "amp", "lt", "gt" });
            RegisterHtmlEntities();
        }

        public XText TryDecodeEntityString(string htmlEntity)
        {
            var m = _entRegex.Matches(htmlEntity).Cast<Match>().FirstOrDefault();

            if (m != null)
            {
                var name = m.Groups[1].Value;

                var xmlEnt = GetDecodedXmlEntity(name);

                if (xmlEnt != null)
                {
                    return new XText(xmlEnt);
                }

                var val = TryGetDecimalValue(name);

                if (val.HasValue)
                {
                    return (XText)XDocument.Parse("<x>" + string.Format("&#{0};", val) + "</x>").Root.Nodes().First();
                }
            }

            return new XText(htmlEntity);
        }

        public string TryGetXmlEntity(string htmlEntity)
        {
            var m = _entRegex.Matches(htmlEntity).Cast<Match>().FirstOrDefault();

            if (m != null)
            {
                var name = m.Groups[1].Value;

                if (_validXmlEntities.Contains(name)) return htmlEntity;

                var val = TryGetDecimalValue(name);

                if (val.HasValue)
                {
                    return string.Format("&#{0};", val);
                }
            }

            return string.Empty;
        }

        public int? TryGetDecimalValue(string entityNameOrValue)
        {
            int v;

            if(_entityMap.TryGetValue(entityNameOrValue, out v) || int.TryParse(entityNameOrValue, out v))
            {
                return v;
            }

            return null;
        }

        string GetDecodedXmlEntity(string name)
        {
            var xmlIndex = _validXmlEntities.IndexOf(name);

            switch (xmlIndex)
            {
                case -1:
                    break;
                case 0:
                    return "'";
                case 1:
                    return "\"";
                case 2:
                    return "&";
                case 3:
                    return "<";
                case 4:
                    return ">";
            }

            return null;
        }

        static void RegisterHtmlEntities()
        {
            Reg("OElig", 338);
            Reg("oelig", 339);
            Reg("Scaron", 352);
            Reg("scaron", 353);
            Reg("Yuml", 376);
            Reg("circ", 710);
            Reg("tilde", 732);
            Reg("ensp", 8194);
            Reg("emsp", 8195);
            Reg("thinsp", 8201);
            Reg("zwnj", 8204);
            Reg("zwj", 8205);
            Reg("lrm", 8206);
            Reg("rlm", 8207);
            Reg("ndash", 8211);
            Reg("mdash", 8212);
            Reg("lsquo", 8216);
            Reg("rsquo", 8217);
            Reg("sbquo", 8218);
            Reg("ldquo", 8220);
            Reg("rdquo", 8221);
            Reg("bdquo", 8222);
            Reg("dagger", 8224);
            Reg("Dagger", 8225);
            Reg("permil", 8240);
            Reg("lsaquo", 8249);
            Reg("rsaquo", 8250);
            Reg("euro", 8364);
            Reg("fnof", 402);
            Reg("Alpha", 913);
            Reg("Beta", 914);
            Reg("Gamma", 915);
            Reg("Delta", 916);
            Reg("Epsilon", 917);
            Reg("Zeta", 918);
            Reg("Eta", 919);
            Reg("Theta", 920);
            Reg("Iota", 921);
            Reg("Kappa", 922);
            Reg("Lambda", 923);
            Reg("Mu", 924);
            Reg("Nu", 925);
            Reg("Xi", 926);
            Reg("Omicron", 927);
            Reg("Pi", 928);
            Reg("Rho", 929);
            Reg("Sigma", 931);
            Reg("Tau", 932);
            Reg("Upsilon", 933);
            Reg("Phi", 934);
            Reg("Chi", 935);
            Reg("Psi", 936);
            Reg("Omega", 937);
            Reg("alpha", 945);
            Reg("beta", 946);
            Reg("gamma", 947);
            Reg("delta", 948);
            Reg("epsilon", 949);
            Reg("zeta", 950);
            Reg("eta", 951);
            Reg("theta", 952);
            Reg("iota", 953);
            Reg("kappa", 954);
            Reg("lambda", 955);
            Reg("mu", 956);
            Reg("nu", 957);
            Reg("xi", 958);
            Reg("omicron", 959);
            Reg("pi", 960);
            Reg("rho", 961);
            Reg("sigmaf", 962);
            Reg("sigma", 963);
            Reg("tau", 964);
            Reg("upsilon", 965);
            Reg("phi", 966);
            Reg("chi", 967);
            Reg("psi", 968);
            Reg("omega", 969);
            Reg("thetasym", 977);
            Reg("upsih", 978);
            Reg("piv", 982);
            Reg("bull", 8226);
            Reg("hellip", 8230);
            Reg("prime", 8242);
            Reg("Prime", 8243);
            Reg("oline", 8254);
            Reg("frasl", 8260);
            Reg("weierp", 8472);
            Reg("image", 8465);
            Reg("real", 8476);
            Reg("trade", 8482);
            Reg("alefsym", 8501);
            Reg("larr", 8592);
            Reg("uarr", 8593);
            Reg("rarr", 8594);
            Reg("darr", 8595);
            Reg("harr", 8596);
            Reg("crarr", 8629);
            Reg("lArr", 8656);
            Reg("uArr", 8657);
            Reg("rArr", 8658);
            Reg("dArr", 8659);
            Reg("hArr", 8660);
            Reg("forall", 8704);
            Reg("part", 8706);
            Reg("exist", 8707);
            Reg("empty", 8709);
            Reg("nabla", 8711);
            Reg("isin", 8712);
            Reg("notin", 8713);
            Reg("ni", 8715);
            Reg("prod", 8719);
            Reg("sum", 8721);
            Reg("minus", 8722);
            Reg("lowast", 8727);
            Reg("radic", 8730);
            Reg("prop", 8733);
            Reg("infin", 8734);
            Reg("ang", 8736);
            Reg("and", 8743);
            Reg("or", 8744);
            Reg("cap", 8745);
            Reg("cup", 8746);
            Reg("int", 8747);
            Reg("there4", 8756);
            Reg("sim", 8764);
            Reg("cong", 8773);
            Reg("asymp", 8776);
            Reg("ne", 8800);
            Reg("equiv", 8801);
            Reg("le", 8804);
            Reg("ge", 8805);
            Reg("sub", 8834);
            Reg("sup", 8835);
            Reg("nsub", 8836);
            Reg("sube", 8838);
            Reg("supe", 8839);
            Reg("oplus", 8853);
            Reg("otimes", 8855);
            Reg("perp", 8869);
            Reg("sdot", 8901);
            Reg("lceil", 8968);
            Reg("rceil", 8969);
            Reg("lfloor", 8970);
            Reg("rfloor", 8971);
            Reg("lang", 9001);
            Reg("rang", 9002);
            Reg("loz", 9674);
            Reg("spades", 9824);
            Reg("clubs", 9827);
            Reg("hearts", 9829);
            Reg("diams", 9830);
            Reg("nbsp", 160);
            Reg("iexcl", 161);
            Reg("cent", 162);
            Reg("pound", 163);
            Reg("curren", 164);
            Reg("yen", 165);
            Reg("brvbar", 166);
            Reg("sect", 167);
            Reg("uml", 168);
            Reg("copy", 169);
            Reg("ordf", 170);
            Reg("laquo", 171);
            Reg("not", 172);
            Reg("shy", 173);
            Reg("reg", 174);
            Reg("macr", 175);
            Reg("deg", 176);
            Reg("plusmn", 177);
            Reg("sup2", 178);
            Reg("sup3", 179);
            Reg("acute", 180);
            Reg("micro", 181);
            Reg("para", 182);
            Reg("middot", 183);
            Reg("cedil", 184);
            Reg("sup1", 185);
            Reg("ordm", 186);
            Reg("raquo", 187);
            Reg("frac14", 188);
            Reg("frac12", 189);
            Reg("frac34", 190);
            Reg("iquest", 191);
            Reg("Agrave", 192);
            Reg("Aacute", 193);
            Reg("Acirc", 194);
            Reg("Atilde", 195);
            Reg("Auml", 196);
            Reg("Aring", 197);
            Reg("AElig", 198);
            Reg("Ccedil", 199);
            Reg("Egrave", 200);
            Reg("Eacute", 201);
            Reg("Ecirc", 202);
            Reg("Euml", 203);
            Reg("Igrave", 204);
            Reg("Iacute", 205);
            Reg("Icirc", 206);
            Reg("Iuml", 207);
            Reg("ETH", 208);
            Reg("Ntilde", 209);
            Reg("Ograve", 210);
            Reg("Oacute", 211);
            Reg("Ocirc", 212);
            Reg("Otilde", 213);
            Reg("Ouml", 214);
            Reg("times", 215);
            Reg("Oslash", 216);
            Reg("Ugrave", 217);
            Reg("Uacute", 218);
            Reg("Ucirc", 219);
            Reg("Uuml", 220);
            Reg("Yacute", 221);
            Reg("THORN", 222);
            Reg("szlig", 223);
            Reg("agrave", 224);
            Reg("aacute", 225);
            Reg("acirc", 226);
            Reg("atilde", 227);
            Reg("auml", 228);
            Reg("aring", 229);
            Reg("aelig", 230);
            Reg("ccedil", 231);
            Reg("egrave", 232);
            Reg("eacute", 233);
            Reg("ecirc", 234);
            Reg("euml", 235);
            Reg("igrave", 236);
            Reg("iacute", 237);
            Reg("icirc", 238);
            Reg("iuml", 239);
            Reg("eth", 240);
            Reg("ntilde", 241);
            Reg("ograve", 242);
            Reg("oacute", 243);
            Reg("ocirc", 244);
            Reg("otilde", 245);
            Reg("ouml", 246);
            Reg("divide", 247);
            Reg("oslash", 248);
            Reg("ugrave", 249);
            Reg("uacute", 250);
            Reg("ucirc", 251);
            Reg("uuml", 252);
            Reg("yacute", 253);
            Reg("thorn", 254);
            Reg("yuml", 255);
        }

        static void Reg(string entityName, int decValue)
        {
            _entityMap[entityName] = decValue;
        }
    }
}
