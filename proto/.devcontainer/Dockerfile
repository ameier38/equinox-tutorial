FROM mcr.microsoft.com/dotnet/core/sdk:2.2-bionic

ARG NUGET_PACKAGES=$HOME/.nuget/packages

# Copy endpoint specific user settings into container to specify
# .NET Core should be used as the runtime.
COPY settings.vscode.json /root/.vscode-remote/data/Machine/settings.json

# Set non-interactive
ENV DEBIAN_FRONTEND=noninteractive

# Install locales
RUN apt update && \
    apt install -y locales && \
    sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    locale-gen

ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8
ENV LC_ALL en_US.UTF-8

# Install dependencies
RUN apt update && \
    apt install -y git curl apt-transport-https gnupg ca-certificates software-properties-common

# Install mono
# ref: https://www.mono-project.com/download/stable/#download-lin
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | \
    tee /etc/apt/sources.list.d/mono-official-stable.list && \
    apt update && \
    apt install -y mono-complete

# Install fake
# ref: https://fake.build/fake-gettingstarted.html
RUN dotnet tool install fake-cli -g
ENV PATH="$PATH:/root/.dotnet/tools"

# Install grpc plugins
# TODO: https://github.com/grpc/grpc/issues/18307
RUN curl -sSL \
    https://packages.grpc.io/archive/2019/05/e553590a0ac50a91b32fb5cbd0b229ed62f6564e-e68e8f1a-9e50-4c24-a7e5-655eba7ad2b4/protoc/grpc-protoc_linux_x64-1.22.0-dev.tar.gz | \
    tar -C /usr/local/bin -xz

# Install prototool
# ref: https://github.com/uber/prototool/blob/dev/docs/install.md#github-releases
RUN curl -sSL \
    https://github.com/uber/prototool/releases/download/v1.7.0/prototool-$(uname -s)-$(uname -m) \
    -o /usr/local/bin/prototool && \
    chmod +x /usr/local/bin/prototool

# Install go
# ref: https://golang.org/dl/
# ref: https://golang.org/doc/install
RUN curl -sSL \
    https://dl.google.com/go/go1.12.5.linux-amd64.tar.gz | \
    tar -C /usr/local -xz
ENV PATH=$PATH:/usr/local/go/bin
ENV GOPATH=$HOME/go
ENV PATH=$PATH:$GOPATH/bin

# Install go gRPC
RUN go get -u google.golang.org/grpc

# Install go protoc plugin
RUN go get -u github.com/golang/protobuf/protoc-gen-go

# Install C++ Protobuf
RUN curl -sSL \
    https://github.com/protocolbuffers/protobuf/releases/download/v3.7.1/protobuf-cpp-3.7.1.tar.gz | \
    tar -C /usr/local/bin -xz

# Install C# Protobuf
RUN curl -sSL \
    https://github.com/protocolbuffers/protobuf/releases/download/v3.7.1/protobuf-csharp-3.7.1.tar.gz | \
    tar -C /usr/local/bin -xz

# Clean up
RUN apt-get autoremove -y && \
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/*

# Set the default shell to bash instead of sh
ENV SHELL /bin/bash
