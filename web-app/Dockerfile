FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

# set build variables
ARG FAKE_VERSION=5.20.4-alpha.1642
ARG PAKET_VERSION=5.251.0
ARG APP_SCHEME=http
ARG APP_HOST=localhost
ARG APP_PORT=8080
ARG GRAPHQL_SCHEME=http
ARG GRAPHQL_HOST=localhost
ARG GRAPHQL_PORT=4000
ARG AUTH_DOMAIN=cosmicdealership.us.auth0.com
ARG AUTH_CLIENT_ID=oQDY1ytC1zBkhG9GMSBVUAypowVxBdYW
ARG AUTH_AUDIENCE=https://cosmicdealership.com

WORKDIR /app

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# install tools
RUN dotnet tool install -g fake-cli --version ${FAKE_VERSION} \
    && dotnet tool install -g paket --version ${PAKET_VERSION}

# add tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# install Node
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - \
    && apt-get install -y nodejs

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN paket install

COPY package.json .
COPY package-lock.json .
RUN npm install

# build application
COPY src src
COPY dist dist
COPY webpack.config.js .
COPY build.fsx .
RUN fake build -t Build

FROM nginx:1.17-alpine as runner

COPY --from=builder /app/dist /var/www
COPY nginx.conf /etc/nginx/conf.d/default.conf
