using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class PrincipalComponentAnalysis
    {
        private readonly IEnumerable<IVector> _sampleFeatureSet;

        public PrincipalComponentAnalysis(IFeatureDataSource sampleFeatureSet)
        {
            _sampleFeatureSet = sampleFeatureSet.ExtractVectors();
        }

        public PrincipalComponentAnalysis(IEnumerable<IVector> sampleFeatureSet)
        {
            _sampleFeatureSet = sampleFeatureSet;
        }

        /// <summary>
        /// Returns the Eigenvalues and Eigenvectors as a <see cref="Tuple{T1, T2}"/> (in that order)
        /// </summary>
        /// <param name="sampleSize">The size of the sample to extract from the data</param>
        /// <returns>A <see cref="Tuple{T1, T2}"/> of values</returns>
        public Tuple<Vector, Matrix> GetEigenvalueDecomposition(int sampleSize = 100)
        {
            var matrix = new Matrix(_sampleFeatureSet.Select(v => v.ToColumnVector()).Take(sampleSize));

            return GetEigenvalueDecomposition(matrix);
        }

        /// <summary>
        /// Finds the pricipal components of a sample data set
        /// and reduces the dimension, providing a transformation
        /// function which transforms an original vector into the
        /// reduced space
        /// </summary>
        /// <param name="numberOfDimensions">The maximum number of dimensions to return</param>
        /// <param name="sampleSize">The sample size to use in the analysis</param>
        /// <returns>A transforming function</returns>
        public Func<double[], double[]> CreatePrincipalComponentTransformation(int numberOfDimensions, int sampleSize = 100)
        {
            var tx = CreatePrincipalComponentTransformer(numberOfDimensions, sampleSize);

            return v => tx.Apply(new ColumnVector1D(v)).ToColumnVector().GetUnderlyingArray();
        }

        /// <summary>
        /// Creates a PCA transformer
        /// </summary>
        /// <param name="numberOfDimensions">The maximum number of dimensions to return</param>
        /// <param name="sampleSize">The sample size to use in the analysis</param>
        /// <returns>A <see cref="ISerialisableVectorTransformation"/></returns>
        public ISerialisableVectorTransformation CreatePrincipalComponentTransformer(int numberOfDimensions, int sampleSize = 100)
        {
            var matrix = new Matrix(_sampleFeatureSet.Select(v => v.ToColumnVector()).Take(sampleSize));
            var mean = matrix.MeanVector;
            var eigenDecom = GetEigenvalueDecomposition(matrix);

            int i = 0;

            var orderedVectors = eigenDecom.Item1.ToDictionary(_ => i++, v => v).OrderByDescending(v => v.Value).Take(numberOfDimensions).ToList();
            var eigenMatrix = eigenDecom.Item2;
            var featureSet = eigenMatrix.SelectRows(orderedVectors.Select(v => v.Key).OrderBy(v => v).ToArray());

            return new SerialisableVectorTransformation(featureSet, mean);
        }

        private Tuple<Vector, Matrix> GetEigenvalueDecomposition(Matrix sampleMatrix)
        {
            var matrix = sampleMatrix;
            var meanAdjMatrix = matrix.MeanAdjust();
            var covarianceMatrix = meanAdjMatrix.CovarianceMatrix;
            var eigenvectors = new EigenvalueDecomposition(covarianceMatrix);

            return new Tuple<Vector, Matrix>(new Vector(eigenvectors.RealEigenvalues), eigenvectors.GetV());
        }
    }
}