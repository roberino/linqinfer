using LinqInfer.Data;
using LinqInfer.Genetics;
using LinqInfer.Maths;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Used to specify the parameters that are used to create a new neural network architecture.
    /// The parameters define the input, output and hidden layer sizes as well as the activator
    /// and learning rate.
    /// </summary>
    public class NetworkParameters : IChromosome<NetworkParameters>, ICloneableObject<NetworkParameters>
    {
        /// <summary>
        /// Creates new NetworkParameters instance
        /// </summary>
        /// <param name="inputVectorSize">The input vector size</param>
        /// <param name="outputSize">The output vector size</param>
        /// <param name="activator">A optional activator function (default is Sigmoid)</param>
        public NetworkParameters(int inputVectorSize, int outputSize, ActivatorFunc activator = null)
        {
            Activator = activator ?? Activators.Sigmoid();
            InitialWeightRange = new Range(0.7, -0.7);
            LayerSizes = new[] { inputVectorSize, inputVectorSize * 2, outputSize };
            LearningRate = 0.1;
        }

        /// <summary>
        /// Creates new NetworkParameters instance
        /// </summary>
        /// <param name="layerSizes">The layer sizes including the input and output size</param>
        /// <param name="activator">A optional activator function (default is Sigmoid)</param>
        public NetworkParameters(int[] layerSizes, ActivatorFunc activator = null)
        {
            Contract.Requires(layerSizes != null && layerSizes.Length > 1);

            Activator = activator ?? Activators.Sigmoid();
            InitialWeightRange = new Range(0.7, -0.7);
            LayerSizes = layerSizes;
            LearningRate = 0.1;
        }

        private NetworkParameters()
        {
        }

        public static NetworkParameters Sigmoidal(params int[] layerSizes)
        {
            return new NetworkParameters(layerSizes, Activators.Sigmoid());
        }

        /// <summary>
        /// Returns the input vector size
        /// </summary>
        public int InputVectorSize
        {
            get
            {
                return LayerSizes == null || LayerSizes.Length == 0 ? 0 : LayerSizes[0];
            }
            internal set
            {
                if (LayerSizes == null || LayerSizes.Length == 0)
                {
                    throw new InvalidOperationException();
                }
                LayerSizes[0] = value;
            }
        }

        /// <summary>
        /// Returns the output vector size
        /// </summary>
        public int OutputVectorSize
        {
            get
            {
                return LayerSizes == null || LayerSizes.Length <= 1 ? 0 : LayerSizes[LayerSizes.Length - 1];
            }
        }

        /// <summary>
        /// Gets the activator function
        /// </summary>
        public ActivatorFunc Activator { get; internal set; }


        /// <summary>
        /// Gets the Layer size including the input and output layers
        /// </summary>
        public int[] LayerSizes {  get; internal set; }

        /// <summary>
        /// Gets or sets the initial weight range used to initialise neurons
        /// </summary>
        public Range InitialWeightRange { get; set; }

        /// <summary>
        /// Gets or sets the learning rate
        /// </summary>
        public double LearningRate { get; set; }

        /// <summary>
        /// Creates a new set of parameters combining the parameters of this instance and another
        /// </summary>
        public virtual NetworkParameters Breed(NetworkParameters other)
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
                return a.Create(Functions.Mutate(a.Parameter, b.Parameter, Math.Min(a.Parameter, b.Parameter) / 7d));
            }
            else
            {
                return Functions.AorB(a, b);
            }
        }

        private int[] Breed(int[] a, int[] b)
        {
            if (Functions.Random() > 20) return a; 

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
            if (LearningRate <= 0 && LearningRate > 1) throw new ArgumentException("Invalid learning rate");
            if (InitialWeightRange.Size == 0) throw new ArgumentException("Invalid weight range");
            if (Activator == null) throw new ArgumentException("Missing activator function");
            if (LayerSizes == null || LayerSizes.Length < 2) throw new ArgumentException("Missing or invalid layer sizes");
            if (InputVectorSize <= 0) throw new ArgumentException("Invalid input size");
            if (OutputVectorSize <= 0) throw new ArgumentException("Invalid output size");
        }

        public override string ToString()
        {
            return string.Format("weight initialiser:{0}, layers:{1}, activator:{2}, learning rate:{3}", InitialWeightRange, FormatLayers(), Activator, LearningRate);
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

        /// <summary>
        /// Creates a new clone of these parameters
        /// </summary>
        /// <param name="deep">N/A</param>
        /// <returns>A new <see cref="NetworkParameters"/></returns>
        public NetworkParameters Clone(bool deep)
        {
            return new NetworkParameters(LayerSizes, Activator)
            {
                InitialWeightRange = InitialWeightRange,
                LearningRate = LearningRate
            };
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public object Clone()
        {
            return Clone(true);
        }
    }
}