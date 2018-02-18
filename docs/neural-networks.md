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

// 1: Create a pipeline from a set of data

var pipeline = sample.AsQueryable().CreatePipeline();

// 2: Define a training set with an expression which will be used to classify the data

var trainingSet = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? "x" : "y");

// 3: Create a classifier using the training set

var classifier = pipeline.ToMultilayerNetworkClassifier(errorTolerance: 0.3f).Execute();

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

var classifier = trainingSet
                .ToMultilayerNetworkClassifier(6, 4)
                .Execute();

```