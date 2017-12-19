using LinqInfer.Learning.Features;

namespace LinqInfer.Learning
{
    public static class SofmExtensions
    {
        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// If an initial node radius is supplied, a Kohonen SOM implementation will be used, otherwise a simpler
        /// k-means centroid calculation will be used.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="outputNodeCount">The maximum number of output nodes</param>
        /// <param name="learningRate">The learning rate</param>
        /// <param name="initialNodeRadius">When supplied, this is used used to determine the radius of each cluster node 
        /// which is used to calculate the influence a node has on neighbouring nodes when updating weights</param>
        /// <returns>An execution pipeline for creating a SOFM</returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this IFeatureProcessingPipeline<TInput> pipeline, int outputNodeCount = 10, float learningRate = 0.5f, float? initialNodeRadius = null, int trainingEpochs = 1000) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapperV3<TInput>(outputNodeCount, learningRate, trainingEpochs, initialNodeRadius);

                pipeline.NormaliseData();

                return fm.Map(p);
            });
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// If an initial node radius is supplied, a Kohonen SOM implementation will be used, otherwise a simpler
        /// k-means centroid calculation will be used.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="parameters">The parameters</param>
        /// <returns></returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this IFeatureProcessingPipeline<TInput> pipeline, ClusteringParameters parameters)
             where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapperV3<TInput>(parameters);

                pipeline.NormaliseData();

                return fm.Map(p);
            });
        }
    }
}