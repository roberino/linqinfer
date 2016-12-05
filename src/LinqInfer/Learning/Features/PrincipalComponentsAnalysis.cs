using LinqInfer.Maths;
using System;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class PrincipalComponentsAnalysis
    {
        private readonly IFeatureDataSource _sampleFeatureSet;

        public PrincipalComponentsAnalysis(IFeatureDataSource sampleFeatureSet)
        {
            _sampleFeatureSet = sampleFeatureSet;
        }

        public Func<double[], double[]> CreatePrincipalComponentTransformation(int numberOfDimensions, int sampleSize = 100)
        {
            var matrix = new Matrix(_sampleFeatureSet.ExtractVectors().Take(sampleSize));
            var mean = matrix.MeanVector;
            var meanAdjMatrix = matrix.MeanAdjust();
            var covarianceMatrix = meanAdjMatrix.CovarianceMatrix;
            var eigenvectors = new EigenvalueDecomposition(covarianceMatrix);

            int i = 0;

            var orderedVectors = eigenvectors.RealEigenvalues.ToDictionary(_ => i++, v => v).OrderByDescending(v => v.Value).Take(numberOfDimensions).ToList();
            var eigenMatrix = eigenvectors.GetV();
            var featureSet = eigenMatrix.SelectRows(orderedVectors.Select(v => v.Key).OrderBy(v => v).ToArray());

            return v => (featureSet * new ColumnVector1D((new ColumnVector1D(v) - mean))).GetUnderlyingArray();
        }
    }
}