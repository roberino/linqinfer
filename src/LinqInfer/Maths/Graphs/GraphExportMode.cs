namespace LinqInfer.Maths.Graphs
{
    public enum GraphExportMode
    {
        /// <summary>
        /// Exports nodes with a uniform spatial layout, with members uniformly distributed around each cluster 
        /// </summary>
        UniformSchematic,

        /// <summary>
        /// Exports nodes with a uniform spatial layout, with members uniformly distributed around each cluster.
        /// The euclidean distance of each member from the cluster is represented as a distance from the cluster node.
        /// </summary>
        RelativeSchematic,

        /// <summary> 
        /// Exports nodes with a spatial layout that represents each nodes position within a projected 3D space.
        /// </summary>
        Spatial3D
    }
}