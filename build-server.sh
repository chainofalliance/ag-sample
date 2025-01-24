#!/bin/bash

rm -rf out
rm server.zip

# compile server code
dotnet publish --framework net8.0 \
    --runtime linux-musl-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:DebugType=None \
    -p:AssemblyName=GameServer \
    ./server/server.sln -o ./out

# create zip file of the server code
zip -j -r server.zip ./out/GameServer

