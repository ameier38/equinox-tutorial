FROM mcr.microsoft.com/dotnet/core/sdk:3.0

RUN dotnet tool install -g fake-cli
RUN dotnet tool install -g paket

ENV PATH="$PATH:/root/.dotnet/tools"

# Copy the entrypoint script.
COPY ./entrypoint.sh /usr/bin/fake-action
RUN chmod +x /usr/bin/fake-action

ENTRYPOINT [ "fake-action" ]
