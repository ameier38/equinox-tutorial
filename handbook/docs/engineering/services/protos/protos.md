# Protocol Buffers
[Protocal Buffers](https://developers.google.com/protocol-buffers) are a mechanism
from serializing data and generating service interfaces. Service interfaces (or stubs)
are created using [gRPC](https://grpc.io/).

## Why gRPC?
gRPC provides a centralized way to define the behavior of your system and 
how services will communicate. You only need to read through one directory
to get a sense of all the services and their behaviors. It also takes the
boilerplate out of creating service interfaces and messages. This allows
you to focus on the actually implementation (read: business logic).

## Versioning
It would be nice if the protoc plugins for each language were released
with each language package release [GitHub Issue](https://github.com/grpc/grpc/issues/18307).
For now we have to extract the binaries from the language specific release.

## Development

### Setup
Open the project in [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers).

![reopen-in-container](./images/reopen-in-container.png)

### Usage
Create a new `.proto` file.
```
cd proto
mkdir -p tutorial/lease/v1
touch tutorial/lease/v1/lease.proto
```

Lint the proto files.
```
dotnet fake build -t Lint
```

Generate the gRPC files.
```
dotnet fake build -t Generate
```
> Generated files can be found in the `gen` directory.

## Resources
- [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
- [Protocol Buffer Development Guide](https://developers.google.com/protocol-buffers/docs/overview)
- [Buf](https://buf.build)
- [Buf Style Guide](https://buf.build/docs/style-guide)
