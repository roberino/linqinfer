# Neural Networks

## Overview

There are a number of methods for creating multi-layer neural network classifiers
from a pipeline of feature data.

The two basic prerequisites for creating and training
a neural network are 1) a set of training data and 2) a classifying (linq) expression
which can classify each item from the training set.

The example below uses the default training strategy to create a network
which aims to find the best solution by trialing a number of configurations
and choosing the one with the lowest error.

```cs

// Multi-layer Neural Network Classifier

// must import LinqInfer.Learning.PipelineExtensions

var classifier = pipeline.ToMultilayerNetworkClassifier(p => p.ClassificationGroup, errorTolerance: 0.3f).Execute();

```

### Custom configuration

You can customise the architecture of a network by either providing the number of hidden layers
or by providing an implementation of IMultilayerNetworkTrainingStrategy.

```cs

// Create a network with hidden layers of 6 neurons and 4 neurons.

// We have a vector input size of 4 and and two possible classes, therefore
// the network will be configured as follows:

//	input			layer1			layer2		output

//					o
//	o				o				o
//	o				o				o			o
//	o				o				o			o
//	o				o				o
//					o

var classifier = pipeline
                .ToMultilayerNetworkClassifier(p => p.Age > 25 ? "x" : "y", 6, 4)
                .Execute();

```

### Remoting

Because training can be CPU intensive, it might be useful to delegate the work. 
The remoting extensions can create multilayer network training servers and clients to
delegate the work over a number of processes.