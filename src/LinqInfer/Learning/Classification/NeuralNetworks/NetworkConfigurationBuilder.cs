namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkConfigurationBuilder
    {
        readonly NetworkSpecification _specification;

        public NetworkConfigurationBuilder(NetworkSpecification specification)
        {
            _specification = specification;
        }

        public ILayer CreateConfiguration()
        {
            _specification.Initialise();

            ILayer root = null;
            NetworkLayer next = null;
            NetworkLayer lastLayer = null;

            for (int i = 0; i < _specification.Layers.Count; i++)
            {
                var layer = _specification.Layers[i];

                if (i == 0)
                {
                    next = new NetworkLayer(_specification.InputVectorSize, layer);
                    root = next;
                }
                else
                {
                    next = new NetworkLayer(lastLayer.Size, layer);
                    lastLayer.Successor = next;
                }

                lastLayer = next;
            }

            return root;
        }
    }
}