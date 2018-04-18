# Text Processing

## Overview

The Text namespace contains a number of functions and utilities for converting text into vector representations.

The core features of this namespace are:

* Ability to Tokenise textual content
* Indexing of documents for the purpose of extracting key terms
* Extraction of vocubularies from text
* Extraction of word vectors from text
* Utilities for extracting tokenised content from HTTP sources
* Methods for converting streams of text into "continuous bags of words"

```cs

// Vector extraction example

var vectors = await new Uri("http://my-data-source/text-corpus/")
					.ExtractVectors(
						new CancellationTokenSource(3000).Token,
						c => c.MaxNumberOfDocuments = 150, new EnglishDictionary());

```