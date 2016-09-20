# Neural Networks

## Overview

```cs

var pipeline = queryableSampleDataset.CreatePipeline();

// Self Organising Feature Map

var featureMap = = pipeline.ToSofm().Execute();

// Naive Bayes Classifier

var classifier1 = pipeline.ToNaiveBayesClassifier(p => p.ClassificationGroup).Execute();

// Multi-layer Neural Network Classifier

var classifier2 = pipeline.ToMultilayerNetworkClassifier(p => p.ClassificationGroup, 0.3f).Execute();

```