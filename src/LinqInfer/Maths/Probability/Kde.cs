using LinqInfer.Data;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths.Probability
{
    internal class Kde<T> : IExportableAsDataDocument, IImportableFromDataDocument
    {
        private readonly Func<IDictionary<string, double>, Func<T, Fraction>> _functionFactory;

        private Lazy<Func<T, Fraction>> _function;

        public Kde(Func<IDictionary<string, double>, Func<T, Fraction>> functionFactory)
        {
            Parameters = new ConstrainableDictionary<string, double>(x =>
            {
                Refresh();

                return true;
            });

            _functionFactory = functionFactory;

            Refresh();
        }

        public IDictionary<string, double> Parameters { get; }

        public Func<T, Fraction> Function { get; }

        public void ImportData(PortableDataDocument doc)
        {
            foreach (var item in doc.Properties)
            {
                if (double.TryParse(item.Value, out double x))
                {
                    Parameters[item.Key] = x;
                }
            }
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            foreach (var item in Parameters)
            {
                doc.Properties[item.Key] = item.Value.ToString();
            }

            return doc;
        }

        private void Refresh()
        {
            _function = new Lazy<Func<T, Fraction>>(() => _functionFactory(Parameters));
        }
    }
}