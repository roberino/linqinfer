using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkBuilderContext
    {
        public NetworkBuilderContext()
        {
            NeuronFactory = new Factory<NeuronParameters, INeuron>(p => new NeuronBase(p.Size, p.InitialWeightRange)
            {
                Activator = p.Activator.Activator
            });

            ActivatorFactory = new Factory<string, ActivatorExpression>(ActivatorExpression.Parse);

            ActivatorFormatter = new Factory<ActivatorExpression, string>(a => a.Export());

            LossFunctionFactory = new Factory<string, ILossFunction>(LossFunctions.Parse);

            TransformationFactory = new Factory<string, ISerialisableDataTransformation>(s =>
            {
                if (s == nameof(Softmax)) return new Softmax();

                return new SerialisableDataTransformation();
            });

            WeightUpdateRuleFactory = 
                new Factory<string, WeightUpdateRule>(
                    e => WeightUpdateRule.Parse(e));
        }

        public NetworkBuilderContext(
            IFactory<INeuron, NeuronParameters> neuronFactory, 
            IFactory<ActivatorExpression, string> activatorFactory, 
            IFactory<string, ActivatorExpression> activatorFormatter,
            IFactory<ISerialisableDataTransformation, string> transformationFactory,
            IFactory<WeightUpdateRule, string> weightUpdateRuleFactory)
        {
            NeuronFactory = neuronFactory;
            ActivatorFactory = activatorFactory;
            ActivatorFormatter = activatorFormatter;
            TransformationFactory = transformationFactory;
            WeightUpdateRuleFactory = weightUpdateRuleFactory;
        }

        public IFactory<INeuron, NeuronParameters> NeuronFactory { get; }
        public IFactory<ActivatorExpression, string> ActivatorFactory { get; }
        public IFactory<string, ActivatorExpression> ActivatorFormatter { get; }
        public IFactory<ILossFunction, string> LossFunctionFactory { get; }
        public IFactory<WeightUpdateRule, string> WeightUpdateRuleFactory { get; }
        public IFactory<ISerialisableDataTransformation, string> TransformationFactory { get; }

        static Tuple<string, double> ParseActivatorArgs(string args)
        {
            var matches = Regex.Matches(args, @"(\w+)\(([\d\.]+)\)");

            if (matches.Count > 0)
            {
                var match = matches.Cast<Match>().Single();

                return new Tuple<string, double>(match.Groups[1].Value, double.Parse(match.Groups[2].Value));
            }

            throw new FormatException($"Invalid activator params: {args}");
        }
    }
}