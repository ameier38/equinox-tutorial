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
RUN dotnet tool install -g fake-cli && \
    dotnet tool install -g paket

# add tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# copy paket dependencies
COPY paket.dependencies .
COPY paket.lock .

# copy build script
COPY build.fsx .

# install dependencies
RUN fake build

# copy everything else and build
COPY src src
COPY VehicleApi.sln .
RUN fake build -t Publish

# download grpc-health-probe
RUN curl -sL -o grpc-health-probe \
    'https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/v0.3.2/grpc_health_probe-linux-amd64'

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine as runner

# ref: https://github.com/sgerrand/alpine-pkg-glibc
RUN apk add --no-cache --virtual .build-deps wget && \
    wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub && \
    wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.31-r0/glibc-2.31-r0.apk && \
    apk add glibc-2.31-r0.apk && \
    apk del .build-deps

COPY --from=builder /app/src/Vehicle/out/vehicle-api /usr/local/bin/vehicle-api
RUN chmod +x /usr/local/bin/vehicle-api

COPY --from=builder /app/grpc-health-probe /usr/local/bin/grpc-health-probe
RUN chmod +x /usr/local/bin/grpc-health-probe

ENTRYPOINT [ "/usr/local/bin/vehicle-api" ]
