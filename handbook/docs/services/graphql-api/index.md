# GraphQL API
WIP

## Authentication
Authentication is performed by validating a JSON Web Token (JWT)
signed using the RS256 algorithm. This is the default algorithm
for [Auth0](../infrastructure/auth0.md) and recommended as the secret
used to sign the JWT does not have to be shared in order to verify the JWT.


## Resources
- [Navigating RS256](https://auth0.com/blog/navigating-rs256-and-jwks/)
- [OpenSSL Essentials](https://www.digitalocean.com/community/tutorials/openssl-essentials-working-with-ssl-certificates-private-keys-and-csrs)
- [Create RSA Keys Using OpenSSL](https://www.scottbrady91.com/OpenSSL/Creating-RSA-Keys-using-OpenSSL)
