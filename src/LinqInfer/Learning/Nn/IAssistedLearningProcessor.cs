using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Nn
{
    public interface IAssistedLearningProcessor
    {
        double Train(ColumnVector1D inputVector, ColumnVector1D output);
    }
}
