# Async pipelines

Async pipelines differ from synchronous in that they retrieve data
asynchronously in batches from a supplied data source. 
Each pipeline can be configured with numerous sinks, meaning the data 
batches can be processed by numerous processes
in parallel.

## General examples

```cs

// Conversion of a enumerable to an async equivalent

var arraySource = new[] { 'a', 'b', 'c', 'd' }.AsAsyncEnumerator().CreatePipe();

// Retrieving data from a function

var funcSource = From.Func(LoadBatch);

private static AsyncBatch<MyDataType> LoadBatch(int batchNum)
{
	const int maxBatches = 10;

    var items = LoadData(batchNum);

    return new AsyncBatch<MyDataType>(items, batchNum == maxBatches - 1, batchNum);
}

```

## Text example

The text namespace implements asynchronous pipelines to extract
data from a HTTP source.

```cs

var myHttpUrl = new Uri("http://my-doc-source/root-path/");

var docServices = new HttpDocumentServices();

var docSource = docServices.CreateDocumentSource(myHttpUrl);

var pipe = docSource.CreatePipe();

```