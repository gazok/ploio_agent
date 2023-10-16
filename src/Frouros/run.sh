#!/bin/sh

docker buildx build -t frouros ../ && \
  docker run -u=0 --network=host frouros
