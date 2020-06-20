FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive
# Opt out of dotnet telemetry
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Specify environment and versions
ENV PATH="$PATH:/usr/local/go/bin:/root/.dotnet/tools" \
    GO111MODULE=on \
    GO_VERSION=1.14.2 \
    BUF_VERSION=0.11.0 \
    PROTOC_VERSION=3.11.2 \
    GO_PLUGIN_VERSION=1.21.0 \
    GO_GRPC_PLUGIN_VERSION=1.28.1 \
    CSHARP_GRPC_PLUGIN_VERSION=2.28.1 \
    INCLUDE_PATH=/usr/local/include

# Configure apt and install packages
RUN apt-get update \
    && apt-get -y install --no-install-recommends apt-utils dialog 2>&1 \
    # Verify git, process tools, lsb-release (common in install instructions for CLIs) installed
    && apt-get -y install git unzip openssh-client less iproute2 procps lsb-release \
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# Install go
# ref: https://golang.org/doc/install
RUN mkdir /tmp/go && \
    curl -sSL \
        https://dl.google.com/go/go${GO_VERSION}.linux-amd64.tar.gz \
        -o /tmp/go/go.tar.gz && \
    tar -C /usr/local -xzf /tmp/go/go.tar.gz && \
    rm -rf /tmp/go

# Download buf
# ref: https://buf.build/docs/introduction
RUN curl -sSL \
        https://github.com/bufbuild/buf/releases/download/v${BUF_VERSION}/buf-$(uname -s)-$(uname -m) \
        -o /usr/local/bin/buf && \
    chmod +x /usr/local/bin/buf

# Download protocol buffer compiler (protoc) and well known type definitions
# ref: https://github.com/protocolbuffers/protobuf/releases
RUN mkdir /tmp/protoc && \
    curl -sSL \
        https://github.com/protocolbuffers/protobuf/releases/download/v${PROTOC_VERSION}/protoc-${PROTOC_VERSION}-linux-x86_64.zip \
        -o /tmp/protoc/protoc.zip && \
    cd /tmp/protoc && \
    unzip protoc.zip && \
    mv /tmp/protoc/include/google ${INCLUDE_PATH}/google && \
    mv /tmp/protoc/bin/protoc /usr/local/bin/protoc && \
    chmod +x /usr/local/bin/protoc && \
    rm -rf /tmp/protoc

# Download C# gRPC plugin
# note: nuget packages are just zip files
# note: protoc already includes protoc-gen-csharp (used for csharp_out option)
# ref: https://www.nuget.org/packages/Grpc.Tools/
RUN mkdir /tmp/grpc_tools && \
    curl -sSL \
        https://www.nuget.org/api/v2/package/Grpc.Tools/${CSHARP_GRPC_PLUGIN_VERSION} \
        -o /tmp/grpc_tools/grpc_tools.nupkg && \
    cd /tmp/grpc_tools && \
    unzip grpc_tools.nupkg && \
    mv /tmp/grpc_tools/tools/linux_x64/grpc_csharp_plugin /usr/local/bin/grpc_csharp_plugin && \
    chmod +x /usr/local/bin/grpc_csharp_plugin && \
    rm -rf /tmp/gprc_tools

# Download go grpc and protoc language plugin
# note: protoc-gen-go will be in $GOPATH/bin (/root/go/bin)
# ref: https://github.com/grpc/grpc-go
# ref: https://github.com/protocolbuffers/protobuf-go
RUN go get google.golang.org/grpc@v${GO_GRPC_PLUGIN_VERSION} && \
    go get google.golang.org/protobuf/cmd/protoc-gen-go@v${GO_PLUGIN_VERSION}

# Install dotnet tools
RUN dotnet tool install fake-cli -g \
    && dotnet tool install paket -g

# Switch back to dialog for any ad-hoc use of apt-get
ENV DEBIAN_FRONTEND=dialog
