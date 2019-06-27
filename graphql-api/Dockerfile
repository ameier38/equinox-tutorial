# use build image to compile
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as builder

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

# add fake to path
ENV PATH="$PATH:/root/.dotnet/tools"

# copy paket dependencies
COPY paket.dependencies .
COPY paket.lock .

# copy FAKE script
COPY build.fsx .

# copy Proto
COPY src/Proto/Proto.csproj src/Proto/
COPY src/Proto/paket.references src/Proto/

# copy Graphql
COPY src/Graphql/Graphql.fsproj src/Graphql/
COPY src/Graphql/paket.references src/Graphql/

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

CMD ["dotnet", "Tests.dll"]

FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine as runner

# install grpc alpine dependencies
RUN apk update && apk add libc6-compat

WORKDIR /app

COPY --from=builder /app/src/Graphql/out .

CMD ["dotnet", "Graphql.dll"]
