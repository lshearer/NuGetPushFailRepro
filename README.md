# NuGetPushFailRepro
Reproducing an issue with NuGet push failures

This repo has branches with various tweaks to a C# project, some of which result in NuGet push failure. It demonstrates building, packing, and pushing these packages to a local NuGet server running in a Docker container. The same success/fail behavior is seen when pushing to http://nuget.org, http://myget.org, and a private [Nexus installation (OSS v3.3.1-01)](https://github.com/sonatype/nexus-public).

## Running
Running the test publish script requires Docker and Bash (optional if running the commands by hand instead of using the script).

To test building and pushing a NuGet package for each branch:
- From the repo root, run `./start-server.sh` to spin up a NuGet server in a local Docker container.
- Run `./test-publish.sh` to build, pack, and push the package.


## Results

`fail-*` branches fail for me when run locally on macOS v10.12.4, with v1.0.4 of the dotnet SDK (which includes v4.0.0.0 of NuGet Command Line). 

The failure is seen as an eventual timeout by the NuGet client (or a 500 from the server, in the Nexus case). Successful pushes typically finish on the order of a few seconds. 

I have noticed slight differences between my local machine and our linux build agent in the point at which the pushes fail, hence the reason for including multiple cases that should fail.

## Workaround

A dockerized repro and a workaround has been added to [this branch](https://github.com/lshearer/NuGetPushFailRepro/tree/fails-docker-repro-and-workaround). [Instructions for running the dockerized repro](https://github.com/lshearer/NuGetPushFailRepro/tree/fails-docker-repro-and-workaround#running-in-docker) have been added, as well as the [output of the test runs](https://github.com/lshearer/NuGetPushFailRepro/blob/fails-docker-repro-and-workaround/DockerTestResults.md).
