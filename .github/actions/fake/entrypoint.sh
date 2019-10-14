#!/bin/bash
# This is an entrypoint for our Docker image that does some minimal bootstrapping before executing.

set -e

# If the root of the build.fsx isn't the root of the repo, CD into it.
if [ ! -z "$INPUT_ROOT" ]; then
    cd $INPUT_ROOT
    echo "ROOT: $INPUT_ROOT"
fi

if [ ! -z "$INPUT_TARGET" ]; then
    FAKE_COMMAND="fake build -t $INPUT_TARGET"
else
    FAKE_COMMAND="fake build"
fi

echo "Running $FAKE_COMMAND"
bash -c "$FAKE_COMMAND"
