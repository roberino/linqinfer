using LinqInfer.Maths;

namespace LinqInfer.Learning.Nn
{
    public class NetworkParameters
    {
        public ActivatorFunc Activator { get; set; }
        public int[] LayerSizes {  get; set; }
        public Range InitialWeightRange { get; set; }

        public NetworkParameters Breed(NetworkParameters other)
        {
            return new NetworkParameters()
            {
                Activator = Functions.Random() > 50 ? other.Activator : this.Activator,
                InitialWeightRange = new Range(Functions.Mutate(other.InitialWeightRange.Max, this.InitialWeightRange.Max, 0.1d), Functions.Mutate(other.InitialWeightRange.Min, this.InitialWeightRange.Min, 0.1d)),
                LayerSizes = Breed(LayerSizes, other.LayerSizes)
            };
        }

        private ActivatorFunc Breed(ActivatorFunc a, ActivatorFunc b)
        {
            if (string.Equals(a.Name, b.Name))
            {
                return a.Create(Functions.Mutate(a.Parameter, b.Parameter, 0.5));
            }
            else
            {
                return Functions.AorB(a, b);
            }
        }

        private int[] Breed(int[] a, int[] b)
        {
            var primary = Functions.AorB(a, b);
            var secondary = ReferenceEquals(primary, a) ? b : a;
            var newArr = new int[primary.Length];

            int i = 0;

            foreach(var v in primary)
            {
                if(secondary.Length > i)
                {
                    newArr[i] = (secondary[i] + v) / 2;
                }
                else
                {
                    newArr[i] = v;
                }
                i++;
            }

            return newArr;
        }
    }
}