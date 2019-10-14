FROM golang:1.12 as builder

RUN apt-get update && \
    apt-get install -y curl git unzip && \ 
    rm -rf /var/lib/apt/lists/*

ENV PROTOC_VERSION=3.9.2 \
    GO_PROTOC_PLUGIN_VERSION=1.3.2 \
    GO_GRPC_PLUGIN_VERSION=1.24.0 \
    CSHARP_GRPC_PLUGIN_VERSION=2.24.0 \
    PROTOTOOL_VERSION=1.8.0 \
    PROTOTOOL_PROTOC_BIN_PATH=/usr/local/bin/protoc \
    PROTOTOOL_PROTOC_WKT_PATH=/usr/local/include

# ref: https://github.com/protocolbuffers/protobuf/releases
RUN mkdir /tmp/protoc && \
    curl -sSL \
        https://github.com/protocolbuffers/protobuf/releases/download/v${PROTOC_VERSION}/protoc-${PROTOC_VERSION}-linux-x86_64.zip \
        -o /tmp/protoc/protoc.zip && \
    cd /tmp/protoc && \
    unzip protoc.zip && \
    mv /tmp/protoc/include/google ${PROTOTOOL_PROTOC_WKT_PATH}/google && \
    mv /tmp/protoc/bin/protoc ${PROTOTOOL_PROTOC_BIN_PATH} && \
    chmod +x ${PROTOTOOL_PROTOC_BIN_PATH} && \
    rm -rf /tmp/protoc

# ref: https://github.com/golang/protobuf/releases
RUN GO111MODULE=on go get github.com/golang/protobuf/protoc-gen-go@v${GO_PROTOC_PLUGIN_VERSION} && \
    mv /go/bin/protoc-gen-go /usr/local/bin/ && \
    chmod +x /usr/local/bin/protoc-gen-go

# ref: https://github.com/grpc/grpc-go
RUN GO111MODULE=on go get google.golang.org/grpc@v${GO_GRPC_PLUGIN_VERSION}

# ref: https://www.nuget.org/packages/Grpc.Tools/
RUN mkdir /tmp/grpc_tools && \
    curl -sSL \
        https://www.nuget.org/api/v2/package/Grpc.Tools/${CSHARP_GRPC_PLUGIN_VERSION} \
        -o /tmp/grpc_tools/grpc_tools.nupkg && \
    cd /tmp/grpc_tools && \
    unzip grpc_tools.nupkg && \
    mv /tmp/grpc_tools/tools/linux_x64/grpc_csharp_plugin /usr/bin/grpc_csharp_plugin && \
    chmod +x /usr/bin/grpc_csharp_plugin && \
    rm -rf /tmp/gprc_tools

# ref: https://github.com/uber/prototool/releases
RUN curl -sSL \ 
        https://github.com/uber/prototool/releases/download/v${PROTOTOOL_VERSION}/prototool-$(uname -s)-$(uname -m) \ 
        -o /usr/local/bin/prototool && \ 
    chmod +x /usr/local/bin/prototool

# ref: https://github.com/googleapis/googleapis
RUN git clone https://github.com/googleapis/googleapis.git ${PROTOTOOL_PROTOC_WKT_PATH}/google/googleapis

# Copy the entrypoint script.
COPY ./entrypoint.sh /usr/bin/prototool-action
RUN chmod +x /usr/bin/prototool-action

WORKDIR /work

ENTRYPOINT [ "prototool-action" ]
