# NuGetPushFailRepro
Reproducing an issue with NuGet push failures

This repo has branches with various tweaks to a C# project, some of which result in NuGet push failure. It demonstrates building, packing, and pushing these packages to a local NuGet server running in a Docker container. The same success/fail behavior is seen when pushing to http://nuget.org, http://myget.org, and a private [Nexus installation (OSS v3.3.1-01)](https://github.com/sonatype/nexus-public).

## Running
Running the test publish script requires Docker and Bash (optional if running the commands by hand instead of using the script).

### Running locally
To test building and pushing a NuGet package for each branch on your local OS:
- From the repo root, run `./start-server.sh` to spin up a NuGet server in a local Docker container.
- Run `./test-publish.sh` to build, pack, and push the package.

### Running in Docker
To test the publish within a debian Docker container, run:
- From the repo root, run `./start-server.sh` to spin up a NuGet server in a local Docker container.
- Run `./test-publish-docker.sh` to build, pack, and push the package. It will try to fix the package if push fails.
    - To use the default zip/unzip commands for fixing the package instead of 7z, first set the `NUGET_TEST_USE_DEFAULT_ZIP` environment variable to anything. E.g. `NUGET_TEST_USE_DEFAULT_ZIP=1 ./test-publish-docker.sh`

# Results

`fail-*` branches fail for me when run locally on macOS v10.12.4, with v1.0.4 of the dotnet SDK (which includes v4.0.0.0 of NuGet Command Line). 

The failure is seen as an eventual timeout by the NuGet client (or a 500 from the server, in the Nexus case). Successful pushes typically finish on the order of a few seconds. 

I have noticed slight differences between my local machine and our linux build agent in the point at which the pushes fail, hence the reason for including multiple cases that should fail.

## Docker test results

You can see the full output from using the dockerized test run and 7z workaround on the [Docker test results](DockerTestResults.md) page.

