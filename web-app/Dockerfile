FROM node:carbon

WORKDIR /app

COPY ./package.json .
COPY ./yarn.lock .

ENV PATH=$PATH:/app/node_modules/.bin
ENV CHOKIDAR_USEPOLLING=true

RUN yarn install

EXPOSE 3000
EXPOSE 35729

ENTRYPOINT ["./entrypoint.sh"]
CMD ["start"]
