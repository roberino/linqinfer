namespace LinqInfer.Maths
{
    public struct Range
    {
        public Range(double max = 1, double min = 0)
        {
            Min = min;
            Max = max;
        }

        public double Min { get; private set; }
        public double Max { get; private set; }
        public double Size { get { return Max - Min; } }
        public bool IsWithin(double value)
        {
            return value >= Min && value <= Max;
        }
    }
}