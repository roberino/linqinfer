﻿using System;
using System.Linq;

namespace LinqInfer.Probability
{
    internal class MultiVariateHistogram : IDensityEstimationStrategy<ColumnVector1D>
    {
        public Func<ColumnVector1D, Fraction> Evaluate(IQueryable<ColumnVector1D> sample)
        {
            throw new NotImplementedException();
        }
    }
}
