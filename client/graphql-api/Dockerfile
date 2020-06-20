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
RUN dotnet tool install fake-cli -g && \
    dotnet tool install paket -g

# add tools to path
ENV PATH="$PATH:/root/.dotnet/tools"

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN paket install

# copy everything else and build
COPY build.fsx .
COPY src src
RUN fake build -t Publish

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as runner

WORKDIR /app

COPY --from=builder /app/src/Server/out .

ENTRYPOINT [ "dotnet", "Server.dll" ]
