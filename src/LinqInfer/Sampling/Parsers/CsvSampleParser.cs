using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace LinqInfer.Sampling.Parsers
{
    public class CsvSampleParser
    {
        private readonly ParserSettings _settings;

        public CsvSampleParser(ParserSettings settings = null)
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
                while (true)
                {
                    var nextLine = reader.ReadLine();

                    if (nextLine == null) break;

                    if (sample.Metadata.Fields.Count == 0)
                    {
                        string[] header;

                        if (_settings.FirstRowIsHeader)
                        {
                            header = GetRow(nextLine).ToArray();
                        }
                        else
                        {
                            int n = 1;
                            header = GetRow(nextLine).Select(r => "Field " + n++).ToArray();
                        }

                        int i = 0;

                        sample.Metadata.Fields = header.Select(h => new FieldDescriptor()
                        {
                            Index = i++,
                            Label = h,
                            Name = Qname(h),
                            FieldUsage = FieldUsageType.Feature,
                            DataType = System.TypeCode.Double
                        }).ToList();

                        if (_settings.FirstRowIsHeader) continue;
                    }

                    if (sample.Metadata.Fields.Count == 0) continue;

                    var nextItem = GetDataItem(nextLine, sample.Metadata);

                    if (nextItem == null) break;

                    sample.SampleData.Add(nextItem);
                }
            }

            sample.Recalculate();

            return sample;
        }

        private DataItem GetDataItem(string rowData, DataSampleMetadata metadata)
        {
            var row = GetRow(rowData).ToList();

            if (row.Count == 0) return null;

            string label = null;
            double x;
            var firstFld = metadata.Fields.First();

            if (firstFld.FieldUsage == FieldUsageType.Category || !double.TryParse(row[0], out x))
            {
                firstFld.FieldUsage = FieldUsageType.Category;
                firstFld.DataType = System.TypeCode.String;
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
                foreach(var h in metadata.Fields.Select(f => f.Label))
                {
                    data[h] = dataItem.FeatureVector[i++];
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
