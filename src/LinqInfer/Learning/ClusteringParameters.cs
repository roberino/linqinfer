using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using System;

namespace LinqInfer.Learning
{
    /// <summary>
    /// Used to specify the parameters of a self-organising map (clusting algorithm)
    /// </summary>
    public sealed class ClusteringParameters
    {
        public ClusteringParameters()
        {
            LearningRateDecayFunction = InitialRadius.HasValue ? new Func<float, int, int, double>((r, i, t) => r * Math.Exp(-((double)i / t))) : (r, i, t) => r;
            WeightInitialiser = (i, s) => Functions.RandomVector(s, 0.1, 0.9);
            NeighbourhoodRadiusCalculator = CurrentNeighbourhoodRadius;
            LabelFormatter = GetLabelForMember;
            ExportMode = GraphExportMode.Spatial3D;
        }

        /// <summary>
        /// The number of output nodes
        /// </summary>
        public int NumberOfOutputNodes { get; set; } = 10;

        /// <summary>
        /// The number of training interations
        /// </summary>
        public int TrainingEpochs { get; set; } = 1000;

        /// <summary>
        /// The initial radius of each uninitialised output node
        /// </summary>
        public double? InitialRadius { get; set; }

        /// <summary>
        /// The initial learning rate
        /// </summary>
        public float InitialLearningRate { get; set; } = 0.2f;

        /// <summary>
        /// A function by which the learning rate will be adjusted (typically reduced)
        /// </summary>
        public Func<float, int, int, double> LearningRateDecayFunction { get; set; }

        /// <summary>
        /// An initialisation function used to determine the initial value of a output node's weights.
        /// The function takes a node index and the vector size as parameters (in that order).
        /// </summary>
        public Func<int, int, ColumnVector1D> WeightInitialiser { get; set; }

        /// <summary>
        /// A time based function which calculates the radius of a matched node (BMU), the function
        /// taking the initial radius, epoch index (t) and total epochs
        /// </summary>
        public Func<double, int, int, double> NeighbourhoodRadiusCalculator { get; set; }

        /// <summary>
        /// Used for generating a label for an object instance, node index and member index
        /// </summary>
        public Func<object, int, int, string> LabelFormatter { get; set; }

        /// <summary>
        /// Specifies how the map will be exported into a graph
        /// </summary>
        public GraphExportMode ExportMode { get; set; }

        internal void Validate()
        {
            if (NumberOfOutputNodes <= 0) throw new ArgumentException("OutputNodes missing or invalid");
            if (TrainingEpochs <= 0) throw new ArgumentException("TrainingEpochs missing or invalid");
            if (InitialLearningRate <= 0) throw new ArgumentException("InitialLearningRate missing or invalid");
            if (LearningRateDecayFunction == null) throw new ArgumentException("LearningRateDecayFunction missing");
            if (WeightInitialiser == null) throw new ArgumentException("WeightInitialiser missing");
            if (NeighbourhoodRadiusCalculator == null) throw new ArgumentException("LearningRateDecayFunction missing");
            if (LabelFormatter == null) throw new ArgumentException("LabelFormatter missing");
            if (InitialRadius.HasValue && !(InitialRadius.Value > 0 && InitialRadius.Value < 1)) throw new ArgumentException("Invalid InitialRadius");
        }

        private double CurrentNeighbourhoodRadius(double initialRadius, int iteration, int numberOfIterations)
        {
            var r = initialRadius + 1;
            var l = Math.Log(r);
            var t = numberOfIterations / l;
            var e = Math.Exp(-(double)iteration / t);
            return r * e - 1;
        }

        private string GetLabelForMember(object member, int i, int j)
        {
            if (member == null) return string.Empty;

            if (Type.GetTypeCode(member.GetType()) == TypeCode.Object)
            {
                return i + "." + j;
            }

            return member.ToString();
        }
    }
}