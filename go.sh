#!/bin/bash

# Calculate the new version number
export GIT_VERSION=$(git describe --tags `git rev-list --tags --max-count=1`)
export VERSION=$(./version.sh $GIT_VERSION revision)

echo $GIT_VERSION
echo $VERSION

# Push the version tag
git config --global user.email "dev.nullinside@gmail.com".
git config --global user.name "J█████"
git tag -a $VERSION -m $VERSION
git push https://ProgrammingByPermutation:$TOKEN@github.com/ProgrammingByPermutation/streaming-tools.git --tags


# Build the software
dotnet publish -r win-x64 -c Release streaming-tools/streaming-tools.sln /p:AssemblyVersion=$VERSION


# Zip it up
cd streaming-tools/StreamingTools/bin/Release/net7.0/win-x64/publish/
zip -r streaming-tools-$VERSION.zip ./*

# Push the artifact to GitHub
gh release create $VERSION streaming-tools-$VERSION.zip --latest --title $VERSION