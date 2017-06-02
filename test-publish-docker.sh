set -ex

docker build -t nuget-publish-test . 
docker run --rm --network=host -e NUGET_TEST_USE_DEFAULT_ZIP=$NUGET_TEST_USE_DEFAULT_ZIP nuget-publish-test