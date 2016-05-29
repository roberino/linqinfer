﻿using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface ICategoricalOutputMapper<T> : IFloatingPointFeatureExtractor<T> where T : IEquatable<T>
    {
        void Initialise(IEnumerable<T> outputs);
        IEnumerable<ClassifyResult<T>> Map(ColumnVector1D output);
    }
}