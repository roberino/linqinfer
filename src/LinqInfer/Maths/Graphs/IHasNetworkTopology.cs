﻿using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public interface IHasNetworkTopology
    {
        Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            double width = 100,
            double height = 100, 
            IWeightedGraphStore<string, double> store = null);
    }
}