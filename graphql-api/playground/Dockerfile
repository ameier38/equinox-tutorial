FROM node:10

RUN npm install -g graphql-cli

WORKDIR /playground

COPY .graphqlconfig.yaml .

EXPOSE 3001

ENTRYPOINT [ "graphql", "playground", "--port", "3001", "--endpoint", "" ]
