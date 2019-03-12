#!/usr/bin/env bash

endpoint="$1"
timeout=5
while [[ "$(curl -s -o /dev/null -w ''%{http_code}'' $endpoint)" != "200" ]]; do
    if [[ "$timeout" -eq 10 ]]; then
        echo "could not reach $endpoint"
        exit 1
    fi
    echo "waiting $timeout seconds for $endpoint"
    sleep 5
    ((timeout++))
done
echo "successfully reached $endpoint"
