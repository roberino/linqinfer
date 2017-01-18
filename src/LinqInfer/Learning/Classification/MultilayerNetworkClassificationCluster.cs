using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkClassificationCluster
    {
        private readonly IDictionary<string, MultilayerNetwork> _networks;

        public MultilayerNetworkClassificationCluster()
        {
            _networks = new Dictionary<string, MultilayerNetwork>();

        }
    }
}
