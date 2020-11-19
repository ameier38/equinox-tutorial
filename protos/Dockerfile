FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive

# Opt out of dotnet telemetry
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Specify environment and versions
ENV PATH="$PATH:/usr/local/go/bin" \
    GO111MODULE=on \
    GO_VERSION=1.15.5 \
    BUF_VERSION=0.31.1 \
    PROTOC_VERSION=3.13.0 \
    GO_PLUGIN_VERSION=1.25.0 \
    GO_GRPC_PLUGIN_VERSION=1.0.1 \
    CSHARP_GRPC_PLUGIN_VERSION=2.33.1 \
    GOOGLE_APIS_COMMIT=d4aa417ed2bba89c2d216900282bddfdafef6128

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

# Install go
# ref: https://golang.org/doc/install
RUN mkdir /tmp/go && \
    curl -sSL \
        https://dl.google.com/go/go${GO_VERSION}.linux-amd64.tar.gz \
        -o /tmp/go/go.tar.gz && \
    tar -C /usr/local -xzf /tmp/go/go.tar.gz && \
    rm -rf /tmp/go

# Install buf
# ref: https://buf.build/docs/introduction
RUN curl -sSL \
        https://github.com/bufbuild/buf/releases/download/v${BUF_VERSION}/buf-$(uname -s)-$(uname -m) \
        -o /usr/local/bin/buf && \
    chmod +x /usr/local/bin/buf

# Download protocol buffer compiler (protoc)
# ref: https://github.com/protocolbuffers/protobuf/releases
RUN mkdir /tmp/protoc && \
    curl -sSL \
        https://github.com/protocolbuffers/protobuf/releases/download/v${PROTOC_VERSION}/protoc-${PROTOC_VERSION}-linux-x86_64.zip \
        -o /tmp/protoc/protoc.zip && \
    cd /tmp/protoc && \
    unzip protoc.zip && \
    mv /tmp/protoc/bin/protoc /usr/local/bin/protoc && \
    chmod +x /usr/local/bin/protoc && \
    rm -rf /tmp/protoc

# Download C# gRPC plugin
# NB: nuget packages are just zip files
# ref: https://www.nuget.org/packages/Grpc.Tools/
RUN mkdir /tmp/grpc_tools && \
    curl -sSL \
        https://www.nuget.org/api/v2/package/Grpc.Tools/${CSHARP_GRPC_PLUGIN_VERSION} \
        -o /tmp/grpc_tools/grpc_tools.nupkg && \
    cd /tmp/grpc_tools && \
    unzip grpc_tools.nupkg && \
    mv /tmp/grpc_tools/tools/linux_x64/grpc_csharp_plugin /usr/local/bin/protoc-gen-csharp-grpc && \
    chmod +x /usr/local/bin/protoc-gen-csharp-grpc && \
    rm -rf /tmp/gprc_tools

# Download go gRPC and protoc plugin
# NB: binaries will be in $GOPATH/bin (/root/go/bin)
# ref: https://github.com/grpc/grpc-go
# ref: https://github.com/protocolbuffers/protobuf-go
RUN go get google.golang.org/grpc/cmd/protoc-gen-go-grpc@v${GO_GRPC_PLUGIN_VERSION} && \
    go get google.golang.org/protobuf/cmd/protoc-gen-go@v${GO_PLUGIN_VERSION}

# Install tools
COPY .config .config
RUN dotnet tool restore

WORKDIR /work

# Clone Google APIs protocol buffer repository
RUN git clone https://github.com/googleapis/googleapis.git /work/vendor/github.com/googleapis/googleapis \
    && cd /work/vendor/github.com/googleapis/googleapis \
    && git reset --hard ${GOOGLE_APIS_COMMIT} \
    && cd /work

FROM builder as runner

ENTRYPOINT [ "dotnet", "fake", "build", "-t" ]
CMD [ "Default" ]
