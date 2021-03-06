﻿using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public static class VectorFunctions
    {
        public static IVector ToVector(this double[] values)
        {
            return new ColumnVector1D(values);
        }

        /// <summary>
        /// Creates a labelled matrix from a dictionary of string / vector pairs
        /// </summary>
        public static LabelledMatrix<string> ToMatrix(this IDictionary<string, IVector> vectorDictionary)
        {
            int i = 0;
            return new LabelledMatrix<string>(new Matrix(vectorDictionary.Values.Select(v => v.ToColumnVector())), vectorDictionary.Keys.ToDictionary(k => k, v => i++));
        }

        /// <summary>
        /// Creates a transformation to centre and scale a vector
        /// so that values are centred around a mean and scaled between a
        /// min and a max
        /// </summary>
        /// <param name="minMaxAndMean">The min max and mean</param>
        /// <param name="range">The range that values should fall between</param>
        /// <returns></returns>
        public static SerialisableDataTransformation CreateCentreAndScaleTransformation(this MinMaxMeanVector minMaxAndMean, Range? range = null)
        {
            if (!range.HasValue) range = Range.MinusOneToOne;

            var mean = minMaxAndMean.Mean.ToColumnVector();
            var adjustedMin = minMaxAndMean.Min.ToColumnVector() - mean;
            var adjustedMax = minMaxAndMean.Max.ToColumnVector() - mean - adjustedMin;
            var scaleValue = adjustedMax / range.Value.Size;
            var targetMin = Vector.UniformVector(adjustedMax.Size, -range.Value.Min);

            var centre = new DataOperation(VectorOperationType.Subtract, mean + adjustedMin);
            var scale = new DataOperation(VectorOperationType.SafeDivide, scaleValue);
            var rangeTranspose = new DataOperation(VectorOperationType.Subtract, targetMin);

            var transform = new SerialisableDataTransformation(centre, scale, rangeTranspose);

            return transform;
        }

        /// <summary>
        /// Creates a transformation to scale a vector 
        /// so that values fall between a min max range
        /// </summary>
        public static SerialisableDataTransformation CreateScaleTransformation(this MinMaxVector minMax, Range? range = null)
        {
            if (!range.HasValue) range = Range.MinusOneToOne;

            // subtract actual min
            // divide by actual range
            // multiply by desired range
            // add desired min

            var scaleVect = (minMax.Max.ToColumnVector() - minMax.Min.ToColumnVector()) / range.Value.Size;
            var rangeMin = Vector.UniformVector(scaleVect.Size, -range.Value.Min);

            var minTranspose = new DataOperation(VectorOperationType.Subtract, minMax.Min.ToColumnVector());
            var scale = new DataOperation(VectorOperationType.SafeDivide, scaleVect);
            var rangeTranspose = new DataOperation(VectorOperationType.Subtract, rangeMin);

            var transform = new SerialisableDataTransformation(minTranspose, scale, rangeTranspose);

            return transform;
        }

        internal static bool GreaterThanOrEqualElements<T>(this T a, T b) where T : IVector
        {
            ArgAssert.AssertEquals(a.Size, b.Size, nameof(a.Size));

            return a.ToColumnVector().Zip(b.ToColumnVector(), (ax, bx) => ax >= bx).All(x => x);
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
