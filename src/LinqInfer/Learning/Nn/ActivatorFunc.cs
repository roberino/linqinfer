using System;

namespace LinqInfer.Learning.Nn
{
    public class ActivatorFunc
    {
        public Func<double, double> Activator { get; set; }
        public Func<double, double> Derivative { get; set; }
    }
}
