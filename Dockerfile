# use build image to compile
FROM ameier38/dotnet-mono-sdk:2.1 as builder

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

WORKDIR /app

# copy paket dependencies
COPY paket.dependencies .
COPY paket.lock .

# copy FAKE script
COPY build.fsx .
COPY build.fsx.lock .

# copy Lease
COPY src/Lease/Lease.fsproj src/Lease/
COPY src/Lease/paket.references src/Lease/

# copy Tests
COPY src/Tests/Tests.fsproj src/Tests/
COPY src/Tests/paket.references src/Tests/

# install dependencies
RUN fake build -t InstallDependencies

# copy solution
COPY Tutorial.sln .

# restore dependencies
RUN fake build -t Restore

# copy everything else and build
COPY . .
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/core/runtime:2.1 as test-runtime

WORKDIR /app

COPY --from=builder /app/src/Tests/out .

CMD ["dotnet", "Tests.dll"]

FROM mcr.microsoft.com/dotnet/core/runtime:2.1 as runtime

WORKDIR /app

COPY --from=builder /app/src/Lease/out .

CMD ["dotnet", "Lease.dll"]
