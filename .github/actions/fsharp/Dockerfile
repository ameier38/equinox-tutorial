FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive

# Opt out of dotnet telemetry
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Specify environment and versions
ENV PATH="$PATH:/usr/local/go/bin:/root/.dotnet/tools" \
    GO111MODULE=on \
    GO_VERSION=1.14.2 \
    BUF_VERSION=0.20.5 \
    PROTOC_VERSION=3.11.2 \
    GO_PLUGIN_VERSION=1.21.0 \
    GO_GRPC_PLUGIN_VERSION=1.28.1 \
    CSHARP_GRPC_PLUGIN_VERSION=2.28.1 \
    INCLUDE_PATH=/usr/local/include

# Configure apt and install packages
RUN apt-get update \
    && apt-get -y install --no-install-recommends apt-utils dialog 2>&1 \
    # Verify git, process tools, lsb-release (common in install instructions for CLIs) installed
    && apt-get -y install \
        git \
        unzip \
        openssh-client \
        less \
        iproute2 \
        procps \
        lsb-release \
        dnsutils \
        libglib2.0-0 \
        libx11-xcb1 \
        libx11-6 \
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# Install Node (for Fable apps)
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - \
    && apt-get install -y nodejs

# Install tools
COPY .config .config
RUN dotnet tool restore

# Copy the entrypoint script.
COPY ./entrypoint.sh /usr/bin/fsharp-action
RUN chmod +x /usr/bin/fsharp-action

WORKDIR /work

ENTRYPOINT [ "fsharp-action" ]
