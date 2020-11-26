# Certificates

## Setup
First check the timezone of your host machine. We want to make sure we
are generating test certificates using the same timezone (UTC) in which the
server will be running so we don't get errors in certificate validation.
```shell
cat /etc/timezone
```
```
America/New_York
```

If the timezone is something other than `Etc/UTC` then change the timezone.
```shell
sudo dpkg-reconfigure tzdata
```

![tzdata-none-of-the-above](./images/tzdata-none-of-the-above.png)

![tzdata-utc](./images/tzdata-utc.png)

Check the timezone.
```shell
cat /etc/timezone
```
```
Etc/UTC
```

## SSL

1. Generate private key.

    ```
    openssl genrsa -des3 -out ca.key 2048
    ```

    !!! note
        - `-des3` is the cipher used to encrypt private key with password
        - `-out` is the name of they key that is outputted
        - `2048` is the size of the key

2. Generate a root certificate. The root certificate is a certificate signed
by a _trusted_ certificate authority (CA). Web browsers (e.g. Chrome) verify that
a website's certificate was issued by a _recognized_ CA. For testing purposes, will
we use ourselves as the CA and issue our own certificates. The root certificate will
be installed on the machines that are requesting our site. It will also be used to issue
certificates for our sites.

    ```
    openssl req \
        -x509 \
        -new \
        -nodes \
        -key ca.key \
        -sha256 \
        -out ca.crt \
        -days 365 \
        -subj "/C=US/ST=New York/L=New York/O=Cosmic Dealership/OU=Engineering/CN=Cosmic Dealership"
    ```

    !!! note
        - `-x509` Output a x509 structure instead of a cert request
        - `-new` Indicates a new request
        - `-nodes` Don't encrypt the output key
        - `-key` Private key to use
        - `-sha256` Hash function to use
        - `-out` Name of the file to output
        - `-days` Number of days cert is valid for
        - `-subj` Set or modify request subject (instead of typing interactively)

3. Create a private key to use for creating certificates for our site.
    ```
    openssl genrsa -out proxy.key 2048
    ```

4. Create a certificate signing request (CSR).
    ```
    openssl req \
        -new \
        -key proxy.key \
        -out proxy.csr \
        -subj "/C=US/ST=New York/L=New York/O=Cosmic Dealership/OU=Engineering/CN=localhost"
    ```

5. Create SAN extension file called `proxy.ext`.
    ```
    authorityKeyIdentifier=keyid,issuer
    basicConstraints=CA:FALSE
    keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
    subjectAltName = @alt_names

    [alt_names]
    DNS.1 = web-app.proxy
    DNS.2 = graphql-api.proxy
    ```

6. Create certificate for site.
    ```
    openssl x509 \
        -req \
        -in proxy.csr \
        -CA ca.crt \
        -CAkey ca.key \
        -CAcreateserial \
        -out proxy.crt \
        -days 365 \
        -sha256 \
        -extfile proxy.ext
    ```

## OAuth

Generate a private key to test token validation.
```
openssl genrsa -o private-key 2048
```

Generate a test certificate.
```
openssl req \
    -new \
    -x509 \
    -key private-key \
    -out client-cert \
    -days 365 \
    -subj "/C=US/ST=New York/L=New York/O=Cosmic Dealership/OU=Engineering/CN=oauth"
```

## Resources
- [SSL CA for Local Development](https://deliciousbrains.com/ssl-certificate-authority-for-local-https-development/)
- [Chrome Manage Certificates](https://chromium.googlesource.com/chromium/src.git/+/master/docs/linux/cert_management.md)
