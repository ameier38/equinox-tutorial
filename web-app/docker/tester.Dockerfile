FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

ENV DEBIAN_FRONTEND=noninteractive \
    # NB: must match nuget package version
    CHROMEDRIVER_VERSION=87.0.4280.20

# install packages
RUN apt-get update \
    && apt-get -y install --no-install-recommends \
        locales \
        curl \
        unzip \
    # clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# configure locales
RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    dpkg-reconfigure --frontend=noninteractive locales && \
    update-locale LANG=en_US.UTF-8

# download chromedriver
RUN mkdir /tmp/chromedriver && \
    curl -sSL \
        https://chromedriver.storage.googleapis.com/${CHROMEDRIVER_VERSION}/chromedriver_linux64.zip \
        -o /tmp/chromedriver/chromedriver.zip && \
    cd /tmp/chromedriver && \
    unzip chromedriver.zip && \
    mv /tmp/chromedriver/chromedriver /usr/local/bin/chromedriver && \
    rm -rf /tmp/chromedriver

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# set locales
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LC_ALL en_US.UTF-8

WORKDIR /app

# install tools
COPY .config .config
RUN dotnet tool restore

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN dotnet paket install

# copy everything else and build
COPY build.fsx .
COPY src src
RUN dotnet fake build -t PublishIntegrationTests

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as runner

WORKDIR /app

COPY --from=builder /app/src/IntegrationTests/out .
COPY --from=builder /usr/local/bin/chromedriver /usr/local/bin/chromedriver
RUN chmod +x /usr/local/bin/chromedriver

ENTRYPOINT [ "dotnet", "IntegrationTests.dll" ]
