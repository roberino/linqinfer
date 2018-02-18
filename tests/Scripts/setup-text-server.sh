#!/bin/sh

PORT="8083"

curl -X DELETE -H "Content-Type: application/json; charset=UTF-8" http://localhost:$PORT/text/indexes/i1/

curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "{}" http://localhost:$PORT/text/indexes/i1/

curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-m1.json" http://localhost:$PORT/text/indexes/i1/documents/m1
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-m2.json" http://localhost:$PORT/text/indexes/i1/documents/m2
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-m3.json" http://localhost:$PORT/text/indexes/i1/documents/m3
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-m4.json" http://localhost:$PORT/text/indexes/i1/documents/m4
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-s1.json" http://localhost:$PORT/text/indexes/i1/documents/s1
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-s2.json" http://localhost:$PORT/text/indexes/i1/documents/s2
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-s3.json" http://localhost:$PORT/text/indexes/i1/documents/s3
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./doc-s4.json" http://localhost:$PORT/text/indexes/i1/documents/s4


curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier1.json" http://localhost:$PORT/text/indexes/i1/classifiers/c1 -v
curl -X POST -H "Content-Type: application/json; charset=UTF-8" --data "@./classifier2.json" http://localhost:$PORT/text/indexes/i1/classifiers/c2 -v


