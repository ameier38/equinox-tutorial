#!/bin/bash
# This is an entrypoint for our Docker image that does some minimal bootstrapping before executing.

set -e

# If the root of the buf.yaml isn't the root of the repo, CD into it.
if [ ! -z "$1" ]; then
    cd ${1}
    echo "ROOT: $1"
else
    echo "Error! Must provide `root` argument"
    exit 1
fi

# Get the target to run.
if [ ! -z "$2" ]; then
    echo "TARGET: $2"
else
    echo "Error! Must provide `target` argument"
    exit 1
fi

COMMAND="dotnet fake build -t $2"

echo "Running: $COMMAND"
bash -c "$COMMAND"
