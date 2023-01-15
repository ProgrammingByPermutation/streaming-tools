#!/bin/bash

dotnet publish -r win-x64 -c Release streaming-tools/streaming-tools.sln /p:AssemblyVersion=$1

cd streaming-tools/StreamingTools/bin/Release/net7.0/win-x64/publish/
zip -r streaming-tools-$1.zip ./*

gh release create $1 streaming-tools-$1.zip --latest --title $1