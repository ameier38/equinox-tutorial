# use build image to compile
FROM ameier38/dotnet-mono:2.2 as builder

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
RUN fake build -t Publish

CMD ["dotnet", "src/Lease/out/Lease.dll"]
