#!/bin/sh

case $1 in
  start)
    yarn start
    ;;
  build)
    yarn build
    ;;
  test)
    CI=true yarn test $@
    ;;
  *)
    exec "$@"
    ;;
esac
