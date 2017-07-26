#!/bin/sh

docker build -t linqinferweb -f Dockerfile.web .

# docker stop <already-running-container>

docker run -i -p 8091:8083  linqinferweb
