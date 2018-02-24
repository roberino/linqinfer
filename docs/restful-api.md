# Example requests

## Create an index

POST http://localhost:8234/text/index1

## Add a document

POST http://localhost:8234/text/index1/doc1

{
	"text": "hello world",
	"attributes": {
		"label1": "hi"
	}
}

## Get a document

GET http://localhost:8234/text/index1/doc1 

## Get features

GET http://localhost:8234/text/index1/$features?maxVectorSize=256&transform=pca(15)|filter(1,2,3)

## Create a classifier

POST http://localhost:8234/classifiers/mln/classifier1

{
	"sourceUri": "http://localhost:8234/text/index1/$features?vectorSize=256&transform=pca(15)|filter(1,2,3)&classLabel=label1"
}
