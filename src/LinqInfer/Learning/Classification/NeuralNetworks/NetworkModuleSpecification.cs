﻿using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public class NetworkConnectionSpecification
    {
        public NetworkConnectionSpecification()
        {
            Outputs = new List<int>();
            Inputs = new List<int>();
        }

        public IList<int> Inputs { get; }

        public IList<int> Outputs { get; }

        internal bool AreDefined => Inputs.Any() || Outputs.Any();
    }
    
    [DebuggerDisplay("{Id} ({InputOperator})")]
    public class NetworkModuleSpecification : IExportableAsDataDocument
    {
        public NetworkModuleSpecification(int id)
        {
            Id = id;
            Connections = new NetworkConnectionSpecification();
        }

        public int Id { get; }

        /// <summary>
        /// Determines how the output of input modules are combined
        /// </summary>
        public VectorAggregationType InputOperator { get; set; } = VectorAggregationType.Concatinate;
        
        public NetworkConnectionSpecification Connections { get; }

        public void ReceiveFrom(params NetworkModuleSpecification[] networkModuleSpecifications)
        {
            Connections.Inputs.Add(networkModuleSpecifications.Select(x => x.Id));
        }

        public void ConnectTo(params NetworkModuleSpecification[] networkModuleSpecifications)
        {
            Connections.Outputs.Add(networkModuleSpecifications.Select(x => x.Id));
        }

        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();
            
            doc.SetName(nameof(NetworkModuleSpecification));
            doc.SetPropertyFromExpression(() => InputOperator);
            doc.SetPropertyFromExpression(() => Id);

            doc.Properties[nameof(Connections.Outputs)] = Connections.Outputs.ToCsv();
            doc.Properties[nameof(Connections.Inputs)] = Connections.Inputs.ToCsv();

            return doc;
        }

        internal static NetworkModuleSpecification FromVectorDocument(PortableDataDocument doc,
            NetworkBuilderContext context)
        {
            var id = doc.PropertyOrDefault(nameof(Id), 0);
            var op = doc.PropertyOrDefault(nameof(InputOperator), VectorAggregationType.None);
            var outputs = doc.PropertyOrDefault(nameof(NetworkConnectionSpecification.Outputs), string.Empty);
            var inputs = doc.PropertyOrDefault(nameof(NetworkConnectionSpecification.Inputs), string.Empty);

            var spec = new NetworkModuleSpecification(id)
            {
                 InputOperator = op
            };

            var inputIds = inputs.FromCsv(int.Parse);
            var outputIds = outputs.FromCsv(int.Parse);

            spec.Connections.Inputs.Add(inputIds);
            spec.Connections.Outputs.Add(outputIds);

            return spec;
        }
    }
}