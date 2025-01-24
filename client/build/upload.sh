#!/usr/bin/env bash

echo "# Uploading build..."

CLIENT_CONTAINER=$(pwd)/build/client_container/
rm -rf ${BUILD_PATH}/node_modules/ &> /dev/null 
rm -rf ${CLIENT_CONTAINER}/node_modules/ &> /dev/null 

cp $(pwd)/build/client_container/* ${BUILD_PATH}

chmod -R 755 ${BUILD_PATH}/ChainOfAlliance/Build

docker rm -f ttt-webgl
docker build -t ttt-webgl .
docker run --name ttt-webgl -p 8081:80 -d coa-webgl