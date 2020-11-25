FROM nginx:1.17-alpine as runner

COPY certs/proxy.crt /etc/nginx/proxy.crt
COPY certs/proxy.key /etc/nginx/proxy.key
COPY nginx.conf /etc/nginx/conf.d/default.conf
