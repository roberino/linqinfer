﻿using LinqInfer.Maths;
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

            ActivatorFactory = new Factory<string, ActivatorFunc>(a =>
            {
                var args = ParseActivatorArgs(a);
                return Activators.Create(args.Item1, args.Item2);
            });

            ActivatorFormatter = new Factory<ActivatorFunc, string>(a => $"{a.Name}({a.Parameter})");

            LossFunctionFactory = new Factory<string, ILossFunction>(LossFunctions.Parse);

            TransformationFactory = new Factory<string, ISerialisableVectorTransformation>(s =>
            {
                if (s == nameof(Softmax)) return new Softmax();

                return new SerialisableVectorTransformation();
            });
        }

        public NetworkBuilderContext(
            IFactory<INeuron, NeuronParameters> neuronFactory, 
            IFactory<ActivatorFunc, string> activatorFactory, 
            IFactory<string, ActivatorFunc> activatorFormatter,
            IFactory<ISerialisableVectorTransformation, string> transformationFactory)
        {
            NeuronFactory = neuronFactory;
            ActivatorFactory = activatorFactory;
            ActivatorFormatter = activatorFormatter;
            TransformationFactory = transformationFactory;
        }

        public IFactory<INeuron, NeuronParameters> NeuronFactory { get; }
        public IFactory<ActivatorFunc, string> ActivatorFactory { get; }
        public IFactory<string, ActivatorFunc> ActivatorFormatter { get; }
        public IFactory<ILossFunction, string> LossFunctionFactory { get; }
        public IFactory<ISerialisableVectorTransformation, string> TransformationFactory { get; }

        private static Tuple<string, double> ParseActivatorArgs(string args)
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