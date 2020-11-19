FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

# install locales
RUN DEBIAN_FRONTEND=noninteractive && \
    apt-get update && \
    apt-get install -y locales curl

RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    dpkg-reconfigure --frontend=noninteractive locales && \
    update-locale LANG=en_US.UTF-8

# set locales
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LC_ALL en_US.UTF-8

WORKDIR /app

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# install dotnet tools
COPY .config .config
RUN dotnet tool restore

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN dotnet paket install

# copy everything else and build
COPY build.fsx .
COPY src src
RUN dotnet fake build -t PublishProcessor

# download grpc-health-probe
# ref: https://github.com/grpc-ecosystem/grpc-health-probe
RUN curl -sL -o grpc_health_probe \
    'https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/v0.3.2/grpc_health_probe-linux-amd64'

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as runner

WORKDIR /app

COPY --from=builder /app/src/Processor/out .

COPY --from=builder /app/grpc_health_probe /bin/grpc_health_probe
RUN chmod +x /bin/grpc_health_probe

ENTRYPOINT [ "dotnet", "Processor.dll" ]
