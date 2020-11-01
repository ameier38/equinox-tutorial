FROM golang:1.14-alpine3.11 as builder

WORKDIR /app

ENV GO111MODULE=on \
    CGO_ENABLED=0 \
    GOOS=linux \
    GOARCH=amd64

COPY go.mod .
COPY go.sum .
RUN go mod download

COPY cmd cmd

# ldflag=s turns off symbol table
# ldflag=w turns off debugging information
RUN go build -ldflags="-w -s" -o server ./cmd/server

FROM alpine:3.11 as runner

COPY --from=builder /app/server /usr/local/bin/server
RUN chmod +x /usr/local/bin/server

ENTRYPOINT [ "/usr/local/bin/server" ]
