# use build image to compile
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 as builder

# set nuget package location
ARG NUGET_PACKAGES=/home/.nuget/packages

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

# install fake
RUN dotnet tool install fake-cli -g

# install paket
RUN dotnet tool install paket -g

# add fake to path
ENV PATH="$PATH:/root/.dotnet/tools"

# copy paket dependencies
COPY paket.dependencies .
COPY paket.lock .

# copy FAKE script
COPY build.fsx .

# install dependencies
RUN fake build

# copy everything else and build
COPY . .
RUN fake build -t PublishTests
RUN fake build -t PublishServer

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 as tester

WORKDIR /app

COPY --from=builder /app/src/Tests/out .

CMD ["dotnet", "Tests.dll"]

FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine as runner

# ref: https://github.com/sgerrand/alpine-pkg-glibc
RUN apk --no-cache add ca-certificates wget && \
    wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub && \
    wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.30-r0/glibc-2.30-r0.apk && \
    apk add glibc-2.30-r0.apk

WORKDIR /app

COPY --from=builder /app/src/Server/out .

CMD ["dotnet", "Server.dll"]
