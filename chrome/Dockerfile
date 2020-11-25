FROM browserless/chrome:1.42.0-chrome-stable

USER root

RUN apt-get update \
    && apt-get install -y \
        libnss3-tools \
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# NB: see https://github.com/browserless/chrome/blob/master/Dockerfile
USER blessuser

COPY ./certs/ca.crt /var/ssl/ca.crt

RUN \
    # NB: create certificate store for blessuser 
    mkdir -p $HOME/.pki/nssdb \
    # NB: initialize the certificate store
    && certutil -d $HOME/.pki/nssdb -N \
    # NB: add our self signed certificate as trusted certificate authority
    && certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n cosmicdealership -i /var/ssl/ca.crt
