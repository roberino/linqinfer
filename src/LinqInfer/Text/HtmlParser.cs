using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    /// <summary>
    /// Crude HTML to Xml document parser.
    /// </summary>
    internal class HtmlParser
    {
        private readonly HtmlEntityMap _entityMap;

        public HtmlParser()
        {
            _entityMap = new HtmlEntityMap();
        }

        public IEnumerable<XNode> Parse(string text)
        {
            using(var reader = new StringReader(text))
            {
                return Parse(reader);
            }
        }

        public IEnumerable<XNode> Parse(TextReader reader)
        {
            return new ParserImpl(_entityMap).Parse(reader);
        }

        private class ParserImpl
        {
            private XNode _rootNode;
            private XmlNodeType _state;
            private XNode _currentNode;
            private StringBuilder _currentText;
            private char _lastChar;
            private int _depth;
            private bool _quotationOpen;
            private bool _aposOpen;
            private HtmlEntityMap _entityMap;

            public ParserImpl(HtmlEntityMap entityMap)
            {
                _entityMap = entityMap;
            }

            public IEnumerable<XNode> Parse(TextReader reader)
            {
                _currentNode = _rootNode = new XElement("x");
                _state = XmlNodeType.None;
                _depth = 0;
                _currentText = new StringBuilder();

                while (true)
                {
                    var nextLine = reader.ReadLine();

                    if (nextLine == null) break;

                    foreach (var c in nextLine)
                    {
                        if (!XmlConvert.IsXmlChar(c)) continue;

                        switch (c)
                        {
                            case '<':
                                if (_state == XmlNodeType.Text)
                                {
                                    ReadNode();
                                }
                                _currentText.Append(c);

                                if (!(_quotationOpen || _aposOpen))
                                    if (_state == XmlNodeType.Element)
                                        _state = XmlNodeType.Text;
                                    else
                                        _state = XmlNodeType.Element;

                                break;
                            case '>':
                                if (_state == XmlNodeType.ProcessingInstruction || _state == XmlNodeType.Comment)
                                {
                                    _state = XmlNodeType.None;
                                    _currentText.Clear();
                                }
                                else
                                {
                                    _currentText.Append(c);
                                    ReadNode();
                                }
                                break;
                            case '?':
                                _currentText.Append(c);
                                // <!
                                if (_state == XmlNodeType.Element && _lastChar == '<') _state = XmlNodeType.ProcessingInstruction;
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case '!':
                                _currentText.Append(c);
                                // <!
                                if (_state == XmlNodeType.Element && _lastChar == '<') _state = XmlNodeType.Comment;
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case '/':
                                // </end>
                                _currentText.Append(c);
                                if (_lastChar == '<' && !IsAttributeRead)
                                {
                                    _state = XmlNodeType.EndElement;
                                }
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case ' ':
                            case '\n':
                                _currentText.Append(c);
                                if (_state == XmlNodeType.None || (_state == XmlNodeType.Element && _lastChar == '<' && !IsAttributeRead))
                                {
                                    _state = XmlNodeType.Text;
                                }
                                break;
                            case '"':
                                _currentText.Append(c);
                                if (_state == XmlNodeType.Element && !_aposOpen)
                                {
                                    _quotationOpen = !_quotationOpen;
                                }
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case '\'':
                                _currentText.Append(c);
                                if (_state == XmlNodeType.Element && !_quotationOpen)
                                {
                                    _aposOpen = !_aposOpen;
                                }
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case ';':
                                _currentText.Append(c);

                                if (_state == XmlNodeType.EntityReference)
                                {
                                    ReadNode();
                                }
                                else
                                {
                                    if (_state == XmlNodeType.None)
                                    {
                                        _state = XmlNodeType.Text;
                                    }
                                }
                                break;
                            case '&':

                                if (_state == XmlNodeType.Element && !IsAttributeRead)
                                {
                                    _state = XmlNodeType.Text;
                                    ReadNode();
                                }
                                else
                                {
                                    if (_state == XmlNodeType.Text) _state = XmlNodeType.EntityReference;
                                }

                                _currentText.Append(c);
                                break;
                            default:

                                if (_state == XmlNodeType.None)
                                {
                                    _state = XmlNodeType.Text;
                                }

                                _currentText.Append(c);

                                break;
                        }

                        _lastChar = c;
                    }
                }

                ReadNode();

                return ((XElement)_rootNode).Nodes();
            }

            private bool IsAttributeRead
            {
                get
                {
                    return _state == XmlNodeType.Element && (_aposOpen || _quotationOpen);
                }
            }

            private bool ReadNode()
            {
                if (_currentText.Length == 0) return false;

                XNode nextNode = null;
                bool isClosed = false;

                switch (_state)
                {
                    case XmlNodeType.Text:
                        nextNode = new XText(_currentText.ToString());
                        break;
                    case XmlNodeType.Element:
                        {
                            try
                            {
                                var name = GetCurrentName();
                                var attrs = ReadAttributes(name.Length + 2).ToArray();
                                var ns = attrs.FirstOrDefault(a => a.Name == "xmlns");
                                var qname = GetValidName(name, ns == null ? CurrentNamespace() : CurrentNamespace(ns.Value));

                                nextNode = new XElement(qname, attrs.Where(a => a != ns));
                                isClosed = _currentText.ToString().EndsWith("/>") || IsAutoClosed(name);
                            }
                            catch (Exception)
                            {
                                DefaultState();
                                return false;
                            }
                        }
                        break;
                    case XmlNodeType.EndElement:
                        {
                            try
                            {
                                var qname = GetValidName();

                                while (true)
                                {
                                    if (_currentNode.NodeType == XmlNodeType.Element && qname.LocalName == ((XElement)_currentNode).Name.LocalName)
                                    {
                                        MoveToParent();
                                        break;
                                    }

                                    if (!MoveToParent())
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                DefaultState();
                                return false;
                            }
                        }
                        break;
                    case XmlNodeType.EntityReference:
                        nextNode = _entityMap.TryDecodeEntityString(_currentText.ToString());
                        break;
                }

                _currentText.Clear();

                DefaultState();

                if (nextNode != null)
                {
                    if (_currentNode != null)
                    {
                        if (_currentNode.NodeType == XmlNodeType.Element)
                        {
                            ((XElement)_currentNode).Add(nextNode);
                            _depth++;
                        }
                        else
                        {
                            if (_currentNode.Parent != null)
                            {
                                _currentNode.Parent.Add(nextNode);
                            }
                        }
                    }

                    _currentNode = nextNode;

                    if (isClosed && nextNode != null)
                    {
                        MoveToParent();
                    }

                    return true;
                }

                return false;
            }

            private XNamespace CurrentNamespace(string newNs = null)
            {
                if (!string.IsNullOrEmpty(newNs))
                {
                    return XNamespace.Get(newNs);
                }

                var c = _currentNode;

                while (c != null)
                {
                    if(c.NodeType == XmlNodeType.Element)
                    {
                        return ((XElement)c).Name.Namespace;
                    }

                    c = c.Parent;
                }

                return XNamespace.None;
            }

            private void DefaultState()
            {
                _aposOpen = false;
                _quotationOpen = false;
                _state = XmlNodeType.Text;
            }

            private bool MoveToParent()
            {
                _currentNode = _currentNode.Parent;

                if (_currentNode == null)
                {
                    _currentNode = _rootNode;
                    _depth = 0;
                    return false;
                }

                return true;
            }

            private string GetCurrentEntity()
            {
                var ent = _currentText.ToString().Substring(1);

                if (ent.EndsWith(";"))
                {
                    ent = ent.Substring(0, ent.Length - 1);

                    int v;

                    if(int.TryParse(ent, out v))
                    {

                    }

                    return ent;
                }

                throw new ArgumentException();
            }

            private XName GetValidName(string name = null, XNamespace ns = null)
            {
                var ename = name ?? GetCurrentName();

                var parts = ename.Split(':');

                if (ns == null)
                    return XName.Get(parts.Last());
                else
                    return XName.Get(parts.Last(), ns.NamespaceName);
            }

            private string GetCurrentName()
            {
                var name = _currentText.ToString().Substring(1);

                if (name.StartsWith("/")) name = name.Substring(1);

                var n = new string(name.TakeWhile(c => char.IsLetterOrDigit(c) || c == ':').ToArray());

                return n;
            }

            private class AttributeReader
            {
                public AttributeReader()
                {
                    State = ReadState.None;
                    Text = new StringBuilder();
                    SplitIndex = 0;
                }

                public int SplitIndex;
                public ReadState State;
                public StringBuilder Text;

                public void BeginReadText()
                {
                    SplitIndex = Text.Length;
                    State = ReadState.ReadText;
                }

                public XAttribute ReadAttribute()
                {
                    var t = Text.ToString();
                    var n = t.Substring(0, SplitIndex).Trim().Split(':');
                    var v = t.Substring(SplitIndex).Trim();

                    if ((v.StartsWith("\"") && v.EndsWith("\"")) || (v.StartsWith("'") && v.EndsWith("'")))
                    {
                        v = v.Substring(1, v.Length - 2);
                    }

                    var attr = new XAttribute(XName.Get(n.Last()), v); // TODO: support for XMLNS

                    Text.Clear();
                    State = ReadState.None;
                    SplitIndex = 0;

                    return attr;
                }
            }

            private IEnumerable<XAttribute> ReadAttributes(int skip = 0)
            {
                var textBlock = _currentText.ToString().Substring(skip);
                
                var attrReader = new AttributeReader();

                foreach (var c in textBlock)
                {
                    switch (c)
                    {
                        case '=':
                            if (attrReader.State == ReadState.ReadName)
                            {
                                attrReader.BeginReadText();
                            }
                            else
                            {
                                attrReader.Text.Append(c);
                            }
                            break;
                        case '\'':
                            if (attrReader.State == ReadState.ReadText)
                            {
                                attrReader.State = ReadState.AposOpen;
                            }
                            else
                            {
                                if (attrReader.State == ReadState.AposOpen)
                                {
                                    attrReader.State = ReadState.None;

                                    if (attrReader.SplitIndex > 0)
                                    {
                                        yield return attrReader.ReadAttribute();
                                    }
                                }
                                else
                                {
                                    attrReader.Text.Append(c);
                                }
                            }
                            break;
                        case '"':

                            if (attrReader.State == ReadState.ReadText)
                            {
                                attrReader.State = ReadState.QuoteOpen;
                            }
                            else
                            {
                                if (attrReader.State == ReadState.QuoteOpen)
                                {
                                    attrReader.State = ReadState.None;

                                    if (attrReader.SplitIndex > 0)
                                    {
                                        yield return attrReader.ReadAttribute();
                                    }
                                }
                                else
                                {
                                    attrReader.Text.Append(c);
                                }
                            }

                            break;
                        case ' ':
                            if (attrReader.State == ReadState.QuoteOpen || attrReader.State == ReadState.AposOpen)
                            {
                                attrReader.Text.Append(c);
                            }
                            else
                            {
                                if (attrReader.SplitIndex > 0)
                                {
                                    yield return attrReader.ReadAttribute();
                                }
                            }
                            break;
                        case '>':
                            if (attrReader.State == ReadState.QuoteOpen || attrReader.State == ReadState.AposOpen)
                            {
                                attrReader.Text.Append(c);
                            }
                            else
                            {
                                if (attrReader.State == ReadState.ReadText)
                                {
                                    attrReader.State = ReadState.None;

                                    if (attrReader.SplitIndex > 0)
                                    {
                                        yield return attrReader.ReadAttribute();
                                    }
                                }
                            }
                            break;
                        default:
                            if (attrReader.State == ReadState.None) attrReader.State = ReadState.ReadName;
                            attrReader.Text.Append(c);
                            break;
                    }
                }

                yield break;
            }

            private static readonly HashSet<string> _autoClosedElements = new HashSet<string>(new[] { "br", "link", "meta", "hr", "img", "input" });

            private bool IsAutoClosed(string elementName)
            {
                return _autoClosedElements.Contains(elementName);
            }

            private enum TokenType : byte
            {
                None = 0,
                Quote = (byte)'"',
                Apos = (byte)'\'',
                Gt = (byte)'>',
                Lt = (byte)'<',
                Space = (byte)' ',
                NewLine = (byte)'\n',
                Eq = (byte)'=',
                Amp = (byte)'&',
                Semi = (byte)';',
            }

            private enum ReadState
            {
                None,
                ReadName,
                ReadText,
                QuoteOpen,
                AposOpen
            }
        }
    }
}