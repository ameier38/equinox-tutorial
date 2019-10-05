#!/bin/bash
# This is an entrypoint for our Docker image that does some minimal bootstrapping before executing.

set -e

# If the root of the prototool.yaml isn't the root of the repo, CD into it.
if [ ! -z "$INPUT_ROOT" ]; then
    cd $INPUT_ROOT
    echo "ROOT: $INPUT_ROOT"
fi

# Run target
PROTOTOOL_COMMAND="prototool $*"
bash -c "$PROTOTOOL_COMMAND"
