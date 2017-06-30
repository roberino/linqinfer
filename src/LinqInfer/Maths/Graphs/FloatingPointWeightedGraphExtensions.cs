using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public static class FloatingPointWeightedGraphExtensions
    {
        public static async Task<LabelledMatrix<T>> GetAdjacencyMatrix<T>(this WeightedGraph<T, double> graph)
            where T : IEquatable<T>
        {
            var indexes = new Dictionary<T, int>();
            var rows = new List<double[]>();
            var i = 0;

            foreach (var vertex in await graph.FindAllVertexesAsync())
            {
                indexes[vertex.Label] = i++;
            }

            foreach (var vertex in await graph.FindAllVertexesAsync())
            {
                var row = new double[indexes.Count];

                foreach (var edge in await vertex.GetEdgesAsync())
                {
                    row[indexes[edge.Value.Label]] = edge.Weight;
                }

                rows.Add(row);
            }

            return new LabelledMatrix<T>(new Matrix(rows), indexes);
        }

        public static double VertexCosineSimilarity<T>(this LabelledMatrix<T> adjacencyMatrix, T vertexA, T vertexB)
            where T : IEquatable<T>
        {
            var a = adjacencyMatrix.LabelIndexes[vertexA];
            var b = adjacencyMatrix.LabelIndexes[vertexB];
            var av = adjacencyMatrix.Rows[a];
            var bv = adjacencyMatrix.Rows[b];

            return 1 - new ColumnVector1D(av).CosineDistance(new ColumnVector1D(bv));
        }

        public static double VertexCosineSimilarity<T>(this LabelledMatrix<T> adjacencyMatrix, WeightedGraphNode<T, double> vertexA, WeightedGraphNode<T, double> vertexB)
            where T : IEquatable<T>
        {
            return VertexCosineSimilarity(adjacencyMatrix, vertexA.Label, vertexB.Label);
        }

        public static async Task<double> VertexCosineSimilarityAsync<T>(this WeightedGraphNode<T, double> vertexA, WeightedGraphNode<T, double> vertexB)
            where T : IEquatable<T>
        {
            var graph = vertexA.Owner;

            if (!graph.Equals(vertexB.Owner))
            {
                throw new ArgumentException("Vertexes belong to different graphs");
            }

            object adjacencyMatrixO = null;
            LabelledMatrix<T> adjacencyMatrix = null;

            if (!graph.Cache.TryGetValue("$AdjacencyMatrix", out adjacencyMatrixO) || !(adjacencyMatrixO is LabelledMatrix<T>))
            {
                adjacencyMatrix = await graph.GetAdjacencyMatrix();

                graph.Cache["$AdjacencyMatrix"] = adjacencyMatrix;
            }
            else
            {
                adjacencyMatrix = (LabelledMatrix<T>)adjacencyMatrixO;
            }

            return VertexCosineSimilarity(adjacencyMatrix, vertexA.Label, vertexB.Label);
        }
    }
}