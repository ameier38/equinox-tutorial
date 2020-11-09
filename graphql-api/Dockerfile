FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as base

ARG FAKE_VERSION=5.20.4-alpha.1642
ARG PAKET_VERSION=5.251.0

# install locales
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y locales

RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    dpkg-reconfigure --frontend=noninteractive locales && \
    update-locale LANG=en_US.UTF-8

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# set locales
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LC_ALL en_US.UTF-8

WORKDIR /app

# install tools
RUN dotnet tool install fake-cli -g --version ${FAKE_VERSION} \
    && dotnet tool install paket -g --version ${PAKET_VERSION}

# add tools to path
ENV PATH="$PATH:/root/.dotnet/tools"

FROM base as builder

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN paket install

# copy everything else and build
COPY build.fsx .
COPY src src
RUN fake build -t Publish
RUN fake build -t PublishIntegrationTests

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as tester

WORKDIR /app

COPY --from=builder /app/src/IntegrationTests/out .

ENTRYPOINT [ "dotnet", "IntegrationTests.dll" ]

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as runner

WORKDIR /app

COPY --from=builder /app/src/GraphqlApi/out .
COPY graphiql graphiql

ENTRYPOINT [ "dotnet", "GraphqlApi.dll" ]
