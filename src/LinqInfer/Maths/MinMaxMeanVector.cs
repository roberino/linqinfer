﻿using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Maths
{
    public sealed class MinMaxMeanVector
    {
        public MinMaxMeanVector(IVector min, IVector max, IVector mean)
        {
            Min = ArgAssert.AssertNonNull(min, nameof(min));
            Max = ArgAssert.AssertNonNull(max, nameof(max));
            Mean = ArgAssert.AssertNonNull(mean, nameof(mean));

            if (min.Size != max.Size && max.Size != mean.Size)
            {
                throw new ArgumentException("Inconsistent sized vectors");
            }

            ArgAssert.Assert(() => max.GreaterThanOrEqualElements(min), nameof(max));
            ArgAssert.Assert(() => max.GreaterThanOrEqualElements(mean), nameof(max));
        }

        public MinMaxMeanVector(IVector min, IVector max) : this(min, max, Vector.UniformVector(min.Size, 0))
        {
        }

        public IVector Min { get; }

        public IVector Max { get; }

        public IVector Mean { get; }

        internal static async Task<MinMaxMeanVector> MinMaxAndMeanOfEachDimensionAsync<T>(IAsyncEnumerator<T> values)
            where T : IVector
        {
            ColumnVector1D min = null;
            ColumnVector1D max = null;
            ColumnVector1D mean = null;

            int counter = 0;

            await values.ProcessUsing(b =>
            {
                foreach (var val in b.Items)
                {
                    if (min == null)
                    {
                        min = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                        max = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                        mean = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                    }
                    else
                    {
                        min.Apply((x, n) => x > val[n] ? val[n] : x);
                        max.Apply((x, n) => x < val[n] ? val[n] : x);
                        mean.Apply((x, n) => x + val[n]);
                    }

                    counter++;
                }
            }, CancellationToken.None);

            if (min == null) throw new ArgumentException();

            mean = mean / counter;

            return new MinMaxMeanVector(min, max, mean);
        }

        internal static MinMaxMeanVector MinMaxAndMeanOfEachDimension<T>(IEnumerable<T> values)
            where T : IVector
        {
            ColumnVector1D min = null;
            ColumnVector1D max = null;
            ColumnVector1D mean = null;

            int counter = 0;

            foreach (var val in values)
            {
                if (counter == 0)
                {
                    min = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                    max = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                    mean = new ColumnVector1D(val.ToColumnVector().ToDoubleArray());
                }
                else
                {
                    min.Apply((x, n) => x > val[n] ? val[n] : x);
                    max.Apply((x, n) => x < val[n] ? val[n] : x);
                    mean.Apply((x, n) => x + val[n]);
                }

                counter++;
            }

            if (min == null) throw new ArgumentException();

            mean = mean / counter;

            return new MinMaxMeanVector(min, max, mean);
        }
    }
}