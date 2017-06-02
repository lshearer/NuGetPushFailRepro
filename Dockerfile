FROM microsoft/dotnet:1.1.1-sdk

RUN apt-get update \
    && apt-get install -y p7zip-full zip unzip \
    && rm -rf /var/lib/apt/lists/*

COPY . /app
WORKDIR /app

CMD bash ./test-publish.sh