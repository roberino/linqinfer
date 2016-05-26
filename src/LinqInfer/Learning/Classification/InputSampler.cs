using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    internal class InputAggregator
    {
        private readonly List<double> sample;

        private Lazy<Tuple<double, double>> parameters;
        private Lazy<Func<double, double>> pdf;

        public InputAggregator()
        {
            sample = new List<double>();
            Refresh();
        }

        public Func<double, double> Pdf
        {
            get
            {
                return pdf.Value;
            }
        }

        public double Theta
        {
            get
            {
                return parameters.Value.Item2;
            }
        }

        public double Mu
        {
            get
            {
                return parameters.Value.Item1;
            }
        }

        public void AddSample(double data)
        {
            sample.Add(data);
            Refresh();
        }

        public void Refresh()
        {
            parameters = new Lazy<Tuple<double, double>>(() => sample.MeanStdDev());
            pdf = new Lazy<Func<double, double>>(() => x => Functions.AutoPdf(Theta, Mu)(x));
        }
    }
}
