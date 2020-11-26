# Cosmic Dealership Handbook
This handbook provides all the information you need to operate the Cosmic Dealership.
It is inspired by the [GitLab Handbook](https://about.gitlab.com/handbook/).

## C4 Models
[C4 Model](https://c4model.com/) is a framework for visualizing
software architecture. To understand the Cosmic Dealership
services, first review the following C4 models.

- [Development](./c4-dev.md): Development architecture running in Docker.
- [Production](./c4-prod.md): Production architecture running in Kubernetes.

## Development Environment
Next set up your development environment.

1. Install [VS Code]https://code.visualstudio.com/)
2. Create development [certificates](./infrastructure/certificates.md).
3. Add the following entries to `etc/hosts`.
    ```
    # Cosmic Dealership development
    127.0.0.1 web-app.proxy
    127.0.0.1 graphql-api.proxy
    ```
4. Add development root certificate to Chrome.
    1. Open the Chrome settings.

        ![open-chrome-settings](./images/open-chrome-settings.png)

    2. Scroll down to 'Privacy and security' section and click on the
    'Security' item.
    
        ![chome-security-settings](./images/chrome-security-settings.png)
    
    3. Scroll down to the advanced section and click 'Manage certificates'.

        ![chrome-manage-certificates](./images/chrome-manage-certificates.png)

    4. A dialog box will open. Select the 'Trusted Root Certifcation Authorities'
    tab and then click 'Import'.

        ![chrome-trusted-root-certificates](./images/chrome-trusted-root-certificates.png)

    5. Follow the dialog instructions and import the `ca.crt` file created in step (2).
