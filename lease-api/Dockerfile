FROM mcr.microsoft.com/dotnet/core/sdk:3.0 as builder

# install locales
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y locales

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

# install FAKE
RUN dotnet tool install -g fake-cli

# install Paket
RUN dotnet tool install -g paket

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
COPY . .
RUN fake build -t PublishLease

FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine as runner

# ref: https://github.com/sgerrand/alpine-pkg-glibc
RUN apk --no-cache add ca-certificates wget && \
    wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub && \
    wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.30-r0/glibc-2.30-r0.apk && \
    apk add glibc-2.30-r0.apk

# download the grpc health probe
# ref: https://github.com/grpc-ecosystem/grpc-health-probe/
RUN GRPC_HEALTH_PROBE_VERSION=v0.2.0 && \
    wget -qO/bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe

WORKDIR /app

COPY --from=builder /app/src/Lease/out .

CMD ["dotnet", "Lease.dll"]
