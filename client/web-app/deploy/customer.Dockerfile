ARG RUNTIME_IMAGE=nginx:1.17-alpine

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

WORKDIR /app

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# install tools
RUN dotnet tool install -g fake-cli
RUN dotnet tool install -g paket

# add tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# install Node
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - \
    && apt-get install -y nodejs

# install dependencies
COPY package.json .
COPY package-lock.json .
COPY paket.dependencies .
COPY paket.lock .
COPY build.fsx .
RUN fake build -t Install

# set build variables
ARG APP_SCHEME=http
ARG APP_HOST=localhost
ARG APP_PORT=8080
ARG GRAPHQL_API_SCHEME=http
ARG GRAPHQL_API_HOST=localhost
ARG GRAPHQL_API_PORT=4000
ARG AUTH0_DOMAIN=ameier38.auth0.com
ARG AUTH0_CLIENT_ID=C55b1AVbrUrsOWcASrxW7BwHEU99ES0C
ARG AUTH0_AUDIENCE=https://cosmicdealership.com

# build application
COPY src src
COPY dist dist
COPY webpack.config.js .
RUN fake build -t BuildCustomerApp

FROM ${RUNTIME_IMAGE} as runner

COPY --from=builder /app/dist /var/www
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
