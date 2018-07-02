namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public struct WeightUpdateParameters
    {
        public double CurrentLearningRate;
        public double CurrentWeightValue;
        public double Error;
        public double PreviousLayerOutput;
    }
}