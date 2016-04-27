using System;

namespace LinqInfer.Learning.Nn
{
    public class ActivatorFunc
    {
        public string Name { get; set; }
        public double Parameter { get; set; }
        public Func<double, double> Activator { get; set; }
        public Func<double, double> Derivative { get; set; }
        public Func<double, ActivatorFunc> Create { get; set; }
    }
}