using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public class InputAggregator
    {
        private readonly List<float> sample;

        private Lazy<Tuple<double, double>> parameters;
        private Lazy<Func<float, double>> pdf;

        public InputAggregator()
        {
            sample = new List<float>();
            Refresh();
        }

        public Func<float, double> Pdf
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

        public void AddSample(float data)
        {
            sample.Add(data);
            Refresh();
        }

        public void Refresh()
        {
            parameters = new Lazy<Tuple<double, double>>(() => sample.MeanStdDev());
            pdf = new Lazy<Func<float, double>>(() => x => (float)Functions.AutoPdf(Theta, Mu)(x));
        }
    }
}
