FROM node:10

RUN npm install -g graphql-cli

WORKDIR /playground

COPY .graphqlconfig.yaml .

ENTRYPOINT [ "graphql", "playground", "--port", "3001", "--endpoint", "", "--server-only"]
