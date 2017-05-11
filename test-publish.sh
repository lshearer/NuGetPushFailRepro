#! /bin/bash

set -ex

cd src/NugetPushIssueRepro/
rimraf ./build
rimraf ./bin
rimraf ./obj
dotnet restore
dotnet build --no-incremental -c Release

dotnet ./bin/Release/netcoreapp1.1/NugetPushIssueRepro.dll

# add a random number to the prerelease string so we don't have to worry about the version already existing and killing the server every time
dotnet pack --no-build -o build -c Release --version-suffix publishtest`echo $RANDOM`

dotnet nuget push ./build/*.nupkg --api-key 1234 --source http://localhost:5000/api/v2/package

