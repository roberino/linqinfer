docker build -t linqinferweb -f Dockerfile.web .
docker run -i -p 8091:8083  linqinferweb
