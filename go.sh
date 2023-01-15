#!/bin/bash
export GIT_VERSION=$(git describe --tags `git rev-list --tags --max-count=1`)
export VERSION=$(./version.sh $GIT_VERSION revision)

echo $GIT_VERSION
echo $VERSION

git config --global user.email "dev.nullinside@gmail.com".
git config --global user.name "J█████"
git tag -s $VERSION -m $VERSION

dotnet publish -r win-x64 -c Release streaming-tools/streaming-tools.sln /p:AssemblyVersion=$VERSION

cd streaming-tools/StreamingTools/bin/Release/net7.0/win-x64/publish/
zip -r streaming-tools-$VERSION.zip ./*

gh release create $VERSION streaming-tools-$VERSION.zip --latest --title $VERSION