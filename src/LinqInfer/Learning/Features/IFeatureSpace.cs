using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureSpace
    {
        ColumnVector1D Weights { get; }
    }
}
