using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class OutputValues : IPropagatedOutput
    {
        public OutputValues(IVector lastOutput)
        {
            Output = lastOutput;
        }

        public IVector Output {get;}
    }
}