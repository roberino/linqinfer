#!/bin/sh

PORT="8091"

curl -X DELETE -H "Content-Type: application/json; charset=UTF-8" http://localhost:$PORT/text/indexes/i1/

curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "{}" http://localhost:$PORT/text/indexes/i1/

for i in {1..5}
do
	curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-m$i.json" http://localhost:$PORT/text/indexes/i1/documents/m$i
done

for i in {1..5}
do
	curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-s$i.json" http://localhost:$PORT/text/indexes/i1/documents/s$i
done

curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier1.json" http://localhost:$PORT/text/indexes/i1/classifiers/c1 -v
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier2.json" http://localhost:$PORT/text/indexes/i1/classifiers/c2 -v
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier3.json" http://localhost:$PORT/text/indexes/i1/classifiers/c3 -v
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier4.json" http://localhost:$PORT/text/indexes/i1/classifiers/c4 -v


