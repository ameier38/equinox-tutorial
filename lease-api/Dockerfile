# use build image to compile
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as builder

# install locales
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y locales

RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    dpkg-reconfigure --frontend=noninteractive locales && \
    update-locale LANG=en_US.UTF-8

# set locales
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LC_ALL en_US.UTF-8

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# install fake
RUN dotnet tool install fake-cli -g

# add fake to path
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /app

# copy paket dependencies
COPY paket.dependencies .
COPY paket.lock .

# copy build script
COPY build.fsx .

# copy Lease
COPY src/Lease/Lease.fsproj src/Lease/
COPY src/Lease/paket.references src/Lease/

# copy Proto
COPY src/Proto/Proto.csproj src/Proto
COPY src/Proto/lease.proto src/Proto

# copy Tests
COPY src/Tests/Tests.fsproj src/Tests/
COPY src/Tests/paket.references src/Tests/

# install dependencies
RUN fake build -t InstallDependencies

# copy everything else and build
COPY . .
RUN fake build -t Publish

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 as tester

WORKDIR /app

COPY --from=builder /app/src/Tests/out .
COPY --from=builder /app/wait-for.sh .

CMD ["dotnet", "Tests.dll"]

FROM mcr.microsoft.com/dotnet/core/runtime:2.1 as runner

# install grpc alpine dependencies
RUN apk update && apk add libc6-compat

# download the grpc health probe
# ref: https://github.com/grpc-ecosystem/grpc-health-probe/
RUN GRPC_HEALTH_PROBE_VERSION=v0.2.0 && \
    wget -qO/bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe

WORKDIR /app

COPY --from=builder /app/src/Lease/out .

CMD ["dotnet", "Lease.dll"]
