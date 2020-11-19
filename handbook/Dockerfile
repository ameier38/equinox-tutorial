FROM squidfunk/mkdocs-material:6.1.5 as base

RUN pip install mkdocs-awesome-pages-plugin

ENTRYPOINT [ "mkdocs", "serve" ]

FROM base as builder

COPY mkdocs.yml /docs/mkdocs.yml
COPY docs /docs/docs

RUN mkdocs build

FROM nginx:1.17-alpine as runner

COPY --from=builder /docs/site /var/www

COPY nginx.conf /etc/nginx/conf.d/nginx.conf
