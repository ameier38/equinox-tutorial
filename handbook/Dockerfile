FROM squidfunk/mkdocs-material as builder

RUN pip install mkdocs-awesome-pages-plugin

COPY mkdocs.yml /docs/mkdocs.yml
COPY docs /docs/docs

RUN mkdocs build

ENTRYPOINT [ "mkdocs", "serve" ]

FROM nginx:1.17-alpine as runner

COPY --from=builder /docs/site /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/nginx.conf
