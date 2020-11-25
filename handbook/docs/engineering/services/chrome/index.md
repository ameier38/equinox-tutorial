# Chrome
Headless browser used for integration testing
using [browserless](https://docs.browserless.io/).

## SSL
In order for Chrome to allow HTTPS using our
[generated certificates](../../infrastructure/certificates.md)
we need to tell Chrome to trust the root certificate. We can do
this by adding our `ca.crt` certificate to Chrome's certificate
store. See the [Dockerfile](https://github.com/ameier38/equinox-tutorial/blob/main/chrome/Dockerfile)

## Resources
- [Chrome Manage Certificates](https://chromium.googlesource.com/chromium/src.git/+/master/docs/linux/cert_management.md)
