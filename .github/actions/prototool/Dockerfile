FROM uber/prototool:1.8.1

RUN git clone https://github.com/googleapis/googleapis.git /vendor/googleapis/googleapis

# Copy the entrypoint script.
COPY ./entrypoint.sh /usr/bin/prototool-action

ENTRYPOINT [ "prototool-action" ]
