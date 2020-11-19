# Proto
Protobuf definitions.

See the [handbook](https://handbook.cosmicdealership.com) for more information.

## Setup
Open the project in [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers).

![reopen-in-container](./etc/reopen-in-container.png)

## Usage
Create a new `.proto` file.
```
cd proto
mkdir -p tutorial/lease/v1
touch tutorial/lease/v1/lease.proto
```

Lint the proto files.
```
fake build -t Lint
```

Generate the gRPC files.
```
fake build -t Generate
```
> Generated files can be found in the `gen` directory.

## Resources
- [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
- [Protocol Buffer Development Guide](https://developers.google.com/protocol-buffers/docs/overview)
- [Buf](https://buf.build)
- [Buf Style Guide](https://buf.build/docs/style-guide)
