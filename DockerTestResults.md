# Docker test results

Here are the results from running the Dockerized test, including the recompression workaround.

## Workaround using 7zip
This is the output of running 

```
`./test-publish-docker.sh`
```

It times out on the first push attempt, but then succeeds after fixing the package by extracting and re-archiving it using 7z.

```
++ docker build -t nuget-publish-test .
Sending build context to Docker daemon  1.77 MB

Step 1/5 : FROM microsoft/dotnet:1.1.1-sdk
 ---> 82ba68a32f38
Step 2/5 : RUN apt-get update     && apt-get install -y p7zip-full zip unzip     && rm -rf /var/lib/apt/lists/*
 ---> Using cache
 ---> eb125efbae23
Step 3/5 : COPY . /app
 ---> a47c73bd2fe3
Removing intermediate container 54213677616e
Step 4/5 : WORKDIR /app
 ---> dd800c3a91b4
Removing intermediate container 5deb22f6adb8
Step 5/5 : CMD bash ./test-publish.sh
 ---> Running in 1069d3ddb0ee
 ---> 70d7352a98d2
Removing intermediate container 1069d3ddb0ee
Successfully built 70d7352a98d2
++ docker run --rm --network=host -e NUGET_TEST_USE_DEFAULT_ZIP= nuget-publish-test
+++ dirname ./test-publish.sh
++ cd .
++ pwd
+ scriptDir=/app
+ cd src/NugetPushIssueRepro/
+ rm -rf ./build
+ rm -rf ./bin
+ rm -rf ./obj
+ dotnet restore
  Restoring packages for /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj...
  Installing Microsoft.NETCore.Jit 1.0.5.
  Installing AWSSDK.Core 3.3.13.2.
  Installing AWSSDK.Core 3.3.5.
  Installing Microsoft.NETCore.Runtime.CoreCLR 1.0.5.
  Installing Microsoft.Extensions.CommandLineUtils 1.1.0.
  Installing AWSSDK.S3 3.3.5.15.
  Installing System.ValueTuple 4.3.0.
  Installing YamlDotNet.NetCore 1.0.0.
  Installing System.Net.NetworkInformation 4.3.0.
  Installing AWSSDK.ECR 3.3.1.2.
  Installing Microsoft.NETCore.App 1.0.3.
  Generating MSBuild file /app/src/NugetPushIssueRepro/obj/NugetPushIssueRepro.csproj.nuget.g.props.
  Generating MSBuild file /app/src/NugetPushIssueRepro/obj/NugetPushIssueRepro.csproj.nuget.g.targets.
  Writing lock file to disk. Path: /app/src/NugetPushIssueRepro/obj/project.assets.json
  Restore completed in 2.42 sec for /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj.
  
  NuGet Config files used:
      /app/NuGet.Config
      /root/.nuget/NuGet/NuGet.Config
  
  Feeds used:
      https://api.nuget.org/v3/index.json
  
  Installed:
      11 package(s) to /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj
+ dotnet build --no-incremental -c Release
Microsoft (R) Build Engine version 15.1.548.43366
Copyright (C) Microsoft Corporation. All rights reserved.

  NugetPushIssueRepro -> /app/src/NugetPushIssueRepro/bin/Release/netcoreapp1.1/NugetPushIssueRepro.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.94
+ dotnet ./bin/Release/netcoreapp1.1/NugetPushIssueRepro.dll
Hello
++ echo 4134
+ dotnet pack --no-build -o build -c Release --version-suffix publishtest4134
Microsoft (R) Build Engine version 15.1.548.43366
Copyright (C) Microsoft Corporation. All rights reserved.

  Successfully created package '/app/src/NugetPushIssueRepro/build/NugetPushIssueRepro.3.0.0-publishtest4134.nupkg'.
+ push
+ dotnet nuget push ./build/NugetPushIssueRepro.3.0.0-publishtest4134.nupkg --api-key 1234 --source http://localhost:5000/api/v2/package --timeout 10
info : Pushing NugetPushIssueRepro.3.0.0-publishtest4134.nupkg to 'http://localhost:5000/api/v2/package'...
info :   PUT http://localhost:5000/api/v2/package/
error: A task was canceled.
error:   Pushing took too long. You can change the default timeout of 300 seconds by using the --timeout <seconds> option with the push command.
++ pwd
+ /app/fix-package.sh /app/src/NugetPushIssueRepro/build
++ echo 'Attempting to fix NuGet package archive'
++ buildDir=/app/src/NugetPushIssueRepro/build
Attempting to fix NuGet package archive
++ cd /app/src/NugetPushIssueRepro/build
+++ find NugetPushIssueRepro.3.0.0-publishtest4134.nupkg
+++ head -1
++ pkg=NugetPushIssueRepro.3.0.0-publishtest4134.nupkg
++ mv NugetPushIssueRepro.3.0.0-publishtest4134.nupkg NugetPushIssueRepro.3.0.0-publishtest4134.nupkg.bak
++ '[' -z '' ']'
++ fix7z
++ echo 'Recompressing package with 7z'
Recompressing package with 7z
++ 7z x NugetPushIssueRepro.3.0.0-publishtest4134.nupkg.bak -opackage

7-Zip [64] 9.20  Copyright (c) 1999-2010 Igor Pavlov  2010-11-18
p7zip Version 9.20 (locale=C,Utf16=off,HugeFiles=on,4 CPUs)

Processing archive: NugetPushIssueRepro.3.0.0-publishtest4134.nupkg.bak

Extracting  _rels/.rels
Extracting  NugetPushIssueRepro.nuspec
Extracting  lib/netcoreapp1.1/NugetPushIssueRepro.runtimeconfig.json
Extracting  lib/netcoreapp1.1/NugetPushIssueRepro.dll
Extracting  [Content_Types].xml
Extracting  package/services/metadata/core-properties/b3a800a3a21a4794ad8f13acf8c040ae.psmdcp

Everything is Ok

Files: 6
Size:       131893
Compressed: 53763
++ cd package
++ 7z a -tzip -r ../NugetPushIssueRepro.3.0.0-publishtest4134.nupkg .

7-Zip [64] 9.20  Copyright (c) 1999-2010 Igor Pavlov  2010-11-18
p7zip Version 9.20 (locale=C,Utf16=off,HugeFiles=on,4 CPUs)
Scanning

Creating archive ../NugetPushIssueRepro.3.0.0-publishtest4134.nupkg

Compressing  NugetPushIssueRepro.nuspec
Compressing  [Content_Types].xml
Compressing  _rels/.rels
Compressing  lib/netcoreapp1.1/NugetPushIssueRepro.dll
Compressing  lib/netcoreapp1.1/NugetPushIssueRepro.runtimeconfig.json
Compressing  package/services/metadata/core-properties/b3a800a3a21a4794ad8f13acf8c040ae.psmdcp

Everything is Ok
+ push
+ dotnet nuget push ./build/NugetPushIssueRepro.3.0.0-publishtest4134.nupkg --api-key 1234 --source http://localhost:5000/api/v2/package --timeout 10
info : Pushing NugetPushIssueRepro.3.0.0-publishtest4134.nupkg to 'http://localhost:5000/api/v2/package'...
info :   PUT http://localhost:5000/api/v2/package/
info :   Created http://localhost:5000/api/v2/package/ 182ms
info : Your package was pushed.
```

## Failed workaround using unzip/zip

This is the output of running 

```
NUGET_TEST_USE_DEFAULT_ZIP=1 ./test-publish-docker.sh
```

It still times out on the second push attempt.

```
++ docker build -t nuget-publish-test .
Sending build context to Docker daemon 1.777 MB

Step 1/5 : FROM microsoft/dotnet:1.1.1-sdk
 ---> 82ba68a32f38
Step 2/5 : RUN apt-get update     && apt-get install -y p7zip-full zip unzip     && rm -rf /var/lib/apt/lists/*
 ---> Using cache
 ---> eb125efbae23
Step 3/5 : COPY . /app
 ---> 52c75652ea01
Removing intermediate container 678f76a0eeed
Step 4/5 : WORKDIR /app
 ---> 4573d3e0d28e
Removing intermediate container 641ddd416d3d
Step 5/5 : CMD bash ./test-publish.sh
 ---> Running in 04563fb39b39
 ---> e4e222ef2567
Removing intermediate container 04563fb39b39
Successfully built e4e222ef2567
++ docker run --rm --network=host -e NUGET_TEST_USE_DEFAULT_ZIP=1 nuget-publish-test
+++ dirname ./test-publish.sh
++ cd .
++ pwd
+ scriptDir=/app
+ cd src/NugetPushIssueRepro/
+ rm -rf ./build
+ rm -rf ./bin
+ rm -rf ./obj
+ dotnet restore
  Restoring packages for /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj...
  Installing Microsoft.NETCore.Jit 1.0.5.
  Installing AWSSDK.Core 3.3.13.2.
  Installing AWSSDK.Core 3.3.5.
  Installing Microsoft.NETCore.Runtime.CoreCLR 1.0.5.
  Installing System.Net.NetworkInformation 4.3.0.
  Installing AWSSDK.S3 3.3.5.15.
  Installing AWSSDK.ECR 3.3.1.2.
  Installing Microsoft.NETCore.App 1.0.3.
  Installing System.ValueTuple 4.3.0.
  Installing YamlDotNet.NetCore 1.0.0.
  Installing Microsoft.Extensions.CommandLineUtils 1.1.0.
  Generating MSBuild file /app/src/NugetPushIssueRepro/obj/NugetPushIssueRepro.csproj.nuget.g.props.
  Generating MSBuild file /app/src/NugetPushIssueRepro/obj/NugetPushIssueRepro.csproj.nuget.g.targets.
  Writing lock file to disk. Path: /app/src/NugetPushIssueRepro/obj/project.assets.json
  Restore completed in 2.56 sec for /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj.
  
  NuGet Config files used:
      /app/NuGet.Config
      /root/.nuget/NuGet/NuGet.Config
  
  Feeds used:
      https://api.nuget.org/v3/index.json
  
  Installed:
      11 package(s) to /app/src/NugetPushIssueRepro/NugetPushIssueRepro.csproj
+ dotnet build --no-incremental -c Release
Microsoft (R) Build Engine version 15.1.548.43366
Copyright (C) Microsoft Corporation. All rights reserved.

  NugetPushIssueRepro -> /app/src/NugetPushIssueRepro/bin/Release/netcoreapp1.1/NugetPushIssueRepro.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.10
+ dotnet ./bin/Release/netcoreapp1.1/NugetPushIssueRepro.dll
Hello
++ echo 16060
+ dotnet pack --no-build -o build -c Release --version-suffix publishtest16060
Microsoft (R) Build Engine version 15.1.548.43366
Copyright (C) Microsoft Corporation. All rights reserved.

  Successfully created package '/app/src/NugetPushIssueRepro/build/NugetPushIssueRepro.3.0.0-publishtest16060.nupkg'.
+ push
+ dotnet nuget push ./build/NugetPushIssueRepro.3.0.0-publishtest16060.nupkg --api-key 1234 --source http://localhost:5000/api/v2/package --timeout 10
info : Pushing NugetPushIssueRepro.3.0.0-publishtest16060.nupkg to 'http://localhost:5000/api/v2/package'...
info :   PUT http://localhost:5000/api/v2/package/
error: A task was canceled.
error:   Pushing took too long. You can change the default timeout of 300 seconds by using the --timeout <seconds> option with the push command.
++ pwd
+ /app/fix-package.sh /app/src/NugetPushIssueRepro/build
++ echo 'Attempting to fix NuGet package archive'
++ buildDir=/app/src/NugetPushIssueRepro/build
++ cd /app/src/NugetPushIssueRepro/build
Attempting to fix NuGet package archive
+++ head -1
+++ find NugetPushIssueRepro.3.0.0-publishtest16060.nupkg
++ pkg=NugetPushIssueRepro.3.0.0-publishtest16060.nupkg
++ mv NugetPushIssueRepro.3.0.0-publishtest16060.nupkg NugetPushIssueRepro.3.0.0-publishtest16060.nupkg.bak
++ '[' -z 1 ']'
++ fixDefault
++ echo 'Recompressing package with unzip/zip'
Recompressing package with unzip/zip
++ unzip NugetPushIssueRepro.3.0.0-publishtest16060.nupkg.bak -d package
Archive:  NugetPushIssueRepro.3.0.0-publishtest16060.nupkg.bak
  inflating: package/_rels/.rels     
  inflating: package/NugetPushIssueRepro.nuspec  
  inflating: package/lib/netcoreapp1.1/NugetPushIssueRepro.runtimeconfig.json  
  inflating: package/lib/netcoreapp1.1/NugetPushIssueRepro.dll  
  inflating: package/[Content_Types].xml  
  inflating: package/package/services/metadata/core-properties/16165a7360044159b5c6a13fe2d8dfd6.psmdcp  
++ cd package
++ zip -r ../NugetPushIssueRepro.3.0.0-publishtest16060.nupkg .
  adding: _rels/ (stored 0%)
  adding: _rels/.rels (deflated 46%)
  adding: lib/ (stored 0%)
  adding: lib/netcoreapp1.1/ (stored 0%)
  adding: lib/netcoreapp1.1/NugetPushIssueRepro.runtimeconfig.json (deflated 22%)
  adding: lib/netcoreapp1.1/NugetPushIssueRepro.dll (deflated 60%)
  adding: NugetPushIssueRepro.nuspec (deflated 68%)
  adding: [Content_Types].xml (deflated 60%)
  adding: package/ (stored 0%)
  adding: package/services/ (stored 0%)
  adding: package/services/metadata/ (stored 0%)
  adding: package/services/metadata/core-properties/ (stored 0%)
  adding: package/services/metadata/core-properties/16165a7360044159b5c6a13fe2d8dfd6.psmdcp (deflated 43%)
+ push
+ dotnet nuget push ./build/NugetPushIssueRepro.3.0.0-publishtest16060.nupkg --api-key 1234 --source http://localhost:5000/api/v2/package --timeout 10
info : Pushing NugetPushIssueRepro.3.0.0-publishtest16060.nupkg to 'http://localhost:5000/api/v2/package'...
info :   PUT http://localhost:5000/api/v2/package/
error: A task was canceled.
error:   Pushing took too long. You can change the default timeout of 300 seconds by using the --timeout <seconds> option with the push command.
```
