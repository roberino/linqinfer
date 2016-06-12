# linqinfer

## A lightweight and slightly experimental inference library for C# / LINQ

This library reflects my interest over the last few years in Bayesian probability, 
probabilistic reasoning and other means of inference.

It is both a learning exercise for myself and an attempt to create a useful
library which uses a fluent and LINQ-like approach to this type of problem solving.

### Basic library layout

#### Learning

This is a collection of machine learning algorithms which are available through extention functions and operate on IQueryable sets of data.

* Self organising feature maps
* Simple statistical classifier
* Multi-layer neural network classifier

##### Examples

```cs

var pipeline = queryableSampleDataset.CreatePipeline();

// Self Organising Feature Map

var featureMap = = pipeline.ToSofm().Execute();

// Naive Bayes Classifier

var classifier1 = pipeline.ToNaiveBayesClassifier(p => p.ClassificationGroup).Execute();

// Multi-layer Neural Network Classifier

var classifier2 = pipeline.ToMultilayerNetworkClassifier(p => p.ClassificationGroup, 0.3f).Execute();

```

#### Maths

The Maths namespace consists of some basic numerical utilities including vector manipulation classes and fractions which can sometimes offer a nicer way of working with probabilities.

#### Maths.Probability 

This is a collection of functions and probability "objects" to help solve simple probability problems.

* Sample spaces
* Hypotheses

##### Examples

```cs

// Sample space

queryableSampleDataset.AsSampleSpace()

// Hypotheses

var die = new[] { 4, 6, 8, 12, 20 };
var hypos = die.Select(n => P.Of(n).Is(1).OutOf(die.Length)).AsHypotheses();

hypos.Update(x => x < 6 ? Fraction.Zero : (1).OutOf(x));

hypos.ProbabilityOf(4);

```

#### Text

Utilities for working with text and text documents.

### Examples

See tests for usage examples.

It is still a work in progress.

