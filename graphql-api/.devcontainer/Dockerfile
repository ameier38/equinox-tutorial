FROM ubuntu:bionic

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
    apt install -y wget apt-transport-https gnupg ca-certificates software-properties-common

# Install dotnet
# ref: https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current
RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    add-apt-repository universe && \
    apt update && \
    apt install -y dotnet-sdk-2.1

# Install mono
# ref: https://www.mono-project.com/download/stable/#download-lin
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | \
    tee /etc/apt/sources.list.d/mono-official-stable.list && \
    apt update && \
    apt install -y mono-devel

# Install fake
# ref: https://fake.build/fake-gettingstarted.html
RUN dotnet tool install fake-cli -g
ENV PATH="$PATH:/root/.dotnet/tools"

# Clean up
RUN apt autoremove -y && \
    apt clean -y && \
    rm -rf /var/lib/apt/lists/*

# Set the default shell to bash instead of sh
ENV SHELL /bin/bash
