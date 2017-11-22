using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public static class VectorFunctions
    {
        /// <summary>
        /// Creates a transformation to centre and scale a vector
        /// so that values are centred around a mean and scaled between a
        /// min and a max
        /// </summary>
        /// <param name="minMaxAndMean">The min max and mean</param>
        /// <param name="range">The range that values should fall between</param>
        /// <returns></returns>
        public static SerialisableVectorTransformation CreateCentreAndScaleTransformation(this MinMaxMeanVector minMaxAndMean, Range? range = null)
        {
            if (!range.HasValue) range = new Range(1, -1);

            var mean = minMaxAndMean.Mean.ToColumnVector();
            var adjustedMax = minMaxAndMean.Max.ToColumnVector() - mean - minMaxAndMean.Min.ToColumnVector();
            var adjustedMin = minMaxAndMean.Min.ToColumnVector() - mean;
            var scaleValue = adjustedMax / (range.Value.Max - range.Value.Min);
            var rangeMin = Vector.UniformVector(adjustedMax.Size, range.Value.Min);

            var minTranspose = new VectorOperation(VectorOperationType.Subtract, adjustedMin);
            var scale = new VectorOperation(VectorOperationType.Divide, scaleValue);
            var rangeTranspose = new VectorOperation(VectorOperationType.Subtract, rangeMin);
            var centre = new VectorOperation(VectorOperationType.Subtract, mean);

            var transform = new SerialisableVectorTransformation(centre, minTranspose, scale, rangeTranspose);

            return transform;
        }

        /// <summary>
        /// Creates a transformation to scale a vector 
        /// so that values fall between a min max range
        /// </summary>
        public static SerialisableVectorTransformation CreateScaleTransformation(this MinMaxMeanVector minMax, Range? range = null)
        {
            if (!range.HasValue) range = new Range(1, -1);

            var adjustedMax = minMax.Max.ToColumnVector() - minMax.Min.ToColumnVector();
            var scaleValue = adjustedMax / (range.Value.Max - range.Value.Min);
            var rangeMin = Vector.UniformVector(adjustedMax.Size, range.Value.Min);

            var minTranspose = new VectorOperation(VectorOperationType.Subtract, minMax.Min.ToColumnVector());
            var scale = new VectorOperation(VectorOperationType.Divide, scaleValue);
            var rangeTranspose = new VectorOperation(VectorOperationType.Subtract, rangeMin);

            var transform = new SerialisableVectorTransformation(minTranspose, scale, rangeTranspose);

            return transform;
        }

        public static ColumnVector1D MaxOfEachDimension<T>(this IEnumerable<T> values) where T : IVector
        {
            return MinMaxAndMeanOfEachDimension(values).Max.ToColumnVector();
        }

        public static ColumnVector1D MeanOfEachDimension<T>(this IEnumerable<T> values) where T : IVector
        {
            return MinMaxAndMeanOfEachDimension(values).Mean.ToColumnVector();
        }

        public static ColumnVector1D MinOfEachDimension<T>(this IEnumerable<T> values) where T : IVector
        {
            return MinMaxAndMeanOfEachDimension(values).Min.ToColumnVector();
        }

        public static MinMaxMeanVector MinMaxAndMeanOfEachDimension<T>(this IEnumerable<T> values) where T : IVector
        {
            return MinMaxMeanVector.MinMaxAndMeanOfEachDimension(values);
        }

        public static ColumnVector1D Aggregate(this IEnumerable<ColumnVector1D> values, Func<double, double, double> func)
        {
            if (!values.Any()) throw new ArgumentException(nameof(values));

            double[] result = new double[values.First().Size];

            foreach (var value in values)
            {
                var x = value.GetUnderlyingArray();

                for (var i = 0; i < x.Length; i++)
                {
                    result[i] = func(result[i], x[i]);
                }
            }

            return new ColumnVector1D(result);
        }
    }
}
