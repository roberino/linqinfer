using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace LinqInfer.Storage.Parsers
{
    public class CsvParser
    {
        private readonly ParserSettings _settings;

        public CsvParser(ParserSettings settings = null)
        {
            _settings = settings ?? ParserSettings.Default;
        }

        public DataSample ReadFromStream(Stream data)
        {
            var sample = new DataSample()
            {
                 SampleData = new List<DataItem>()
            };

            using (var reader = new StreamReader(data, _settings.Encoding ?? ParserSettings.Default.Encoding))
            {
                string[] header = null;

                while (true)
                {
                    var nextLine = reader.ReadLine();

                    if (nextLine == null) break;

                    if (header == null)
                    {
                        if (_settings.FirstRowIsHeader)
                        {
                            header = GetRow(nextLine).ToArray();

                            continue;
                        }
                        else
                        {
                            int n = 1;
                            header = GetRow(nextLine).Select(r => "Field " + n++).ToArray();
                        }
                    }

                    var nextItem = GetDataItem(nextLine, header, false);

                    if (nextItem == null) break;

                    sample.SampleData.Add(nextItem);
                }
            }

            return sample;
        }

        private DataItem GetDataItem(string rowData, string[] headers, bool hasLabelCol)
        {
            var row = GetRow(rowData).ToList();

            if (row.Count == 0) return null;

            string label = null;
            double x;

            if (hasLabelCol || !double.TryParse(row[0], out x))
            {
                label = row[0];
            }

            var dataItem = new DataItem()
            {
                FeatureVector = ((label == null) ? row : row.Skip(1)).Select(c => SafeParse(c)).ToArray(),
                Label = label
            };

            if (_settings.FirstRowIsHeader)
            {
                var data = (IDictionary<string, object>)new ExpandoObject();

                int i = 0;
                foreach(var h in headers)
                {
                    data[Qname(h)] = dataItem.FeatureVector[i++];
                }

                dataItem.Item = data;
            }

            return dataItem;
        }

        private string Qname(string name)
        {
            string qn = null;

            if (!char.IsLetter(name[0]))
            {
                qn = "f_";
            }

            var clean = new string(name.Select(
                c => (char.IsLetterOrDigit(c) || c == '_') ? c : '_').ToArray());

            return qn + clean;
        }

        private double SafeParse(string value)
        {
            double d;

            if (double.TryParse(value, out d))
            {
                return d;
            }
            return double.NaN;
        }

        private IEnumerable<string> GetRow(string rowData)
        {
            return rowData.Split(_settings.ColumnDelimiters);
        }
    }
}
