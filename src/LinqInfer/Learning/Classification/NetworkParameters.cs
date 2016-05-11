using LinqInfer.Maths;
using System;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    [Serializable]
    public class NetworkParameters : IEquatable<NetworkParameters>
    {
        public NetworkParameters(int inputVectorSize, int[] neuronSizes = null, ActivatorFunc activator = null)
        {
            Activator = activator ?? Activators.Sigmoid();
            InitialWeightRange = new Range(0.7, -0.7);
            LayerSizes = neuronSizes ?? new[] { inputVectorSize, inputVectorSize };
            LearningRate = 0.1;
        }

        private NetworkParameters()
        {
        }

        public ActivatorFunc Activator { get; set; }
        public int[] LayerSizes {  get; set; }
        public Range InitialWeightRange { get; set; }
        public double LearningRate { get; set; }

        public NetworkParameters Breed(NetworkParameters other)
        {
            var newParams = new NetworkParameters()
            {
                Activator = Breed(other.Activator, Activator),
                InitialWeightRange = new Range(Functions.Mutate(other.InitialWeightRange.Max, this.InitialWeightRange.Max, 0.1d), Functions.Mutate(other.InitialWeightRange.Min, this.InitialWeightRange.Min, 0.1d)),
                LayerSizes = Breed(LayerSizes, other.LayerSizes),
                LearningRate = Math.Max(Math.Min(Functions.Mutate(LearningRate, other.LearningRate, 0.05), 1), 0.01)
            };

            newParams.Validate();

            return newParams;
        }

        private ActivatorFunc Breed(ActivatorFunc a, ActivatorFunc b)
        {
            if (string.Equals(a.Name, b.Name))
            {
                return a.Create(Functions.Mutate(a.Parameter, b.Parameter, Math.Min(a.Parameter, b.Parameter) / 3d));
            }
            else
            {
                return Functions.AorB(a, b);
            }
        }

        private int[] Breed(int[] a, int[] b)
        {
            if(a[0] != b[0] || a[a.Length - 1] != b[b.Length - 1])
            {
                throw new ArgumentException("Invalid network sizes - input / output count mismatch");
            }

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

        private string FormatLayers()
        {
            if (LayerSizes == null) return null;

            return "[" + string.Join(",", LayerSizes.Select(x => x.ToString())) + "]";
        }

        public void Validate()
        {
            if (LearningRate <= 0 && LearningRate > 1) throw new System.ArgumentException("Invalid learning rate");
            if (InitialWeightRange.Size == 0) throw new System.ArgumentException("Invalid weight range");
            if (Activator == null) throw new System.ArgumentException("Missing activator function");
            if (LayerSizes == null) throw new System.ArgumentException("Missing layer sizes");
        }

        public override string ToString()
        {
            return string.Format("weight initialiser:{0}, layers:{1}, activator:{2}", InitialWeightRange, FormatLayers(), Activator);
        }

        public bool Equals(NetworkParameters other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;
            
            if (Activator == null || other.Activator == null) return false;
            if (LayerSizes == null || other.LayerSizes == null) return false;

            return Activator.Equals(other.Activator)
                && InitialWeightRange.Equals(other.InitialWeightRange)
                && LayerSizes.Length == other.LayerSizes.Length
                && LearningRate == other.LearningRate
                && LayerSizes.Zip(other.LayerSizes, (s1, s2) => s1 == s2).All(x => x);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NetworkParameters);
        }

        public override int GetHashCode()
        {
            return new Tuple<double, int, int, int>(LearningRate, InitialWeightRange.GetHashCode(), Activator.GetHashCode(), LayerSizes.Sum(s => s * 7)).GetHashCode();
        }
    }
}