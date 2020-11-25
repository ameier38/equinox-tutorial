FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

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

ENTRYPOINT [ "dotnet", "IntegrationTests.dll" ]
