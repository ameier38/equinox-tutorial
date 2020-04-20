# Proto
Protobuf definitions.

## Setup
Install .NET Core SDK.
```
choco install dotnetcore-sdk -y
```
> Must be run as administrator.

Install FAKE.
```
dotnet tool install fake-cli -g
```

Add tool path to `PATH`.

_Linux/macOS_
```shell
export PATH = "$PATH:$HOME/.dotnet/tools"
```
_Windows Powershell_
```powershell
$env:PATH += ";C:/Users/<user>/.dotnet/tools"
```

## Usage
Create a new `.proto` file.
```
cd protos
mkdir -p tutorial/lease/v1/lease.proto
prototool create tutorial/lease/v1/lease.proto
```
> You must be in the same folder as the `prototool.yaml`
to run `prototool` commands.

Lint the proto files.
```
fake build -t Lint
```

Check that the proto files can compile.
```
fake build -t Compile
```

Generate the gRPC files.
```
fake build -t Generate
```

## Plugins
To get protoc to use the plugin, do one of the following:

Place the plugin binary somewhere in the PATH and give it the name "protoc-gen-NAME"
(replacing "NAME" with the name of your plugin). If you then invoke protoc with the
parameter –NAME_out=OUT_DIR (again, replace "NAME" with your plugin's name), protoc
will invoke your plugin to generate the output, which will be placed in OUT_DIR.

Place the plugin binary anywhere, with any name, and pass the –plugin parameter to protoc to direct it to your plugin like so:
```
protoc --plugin=protoc-gen-NAME=path/to/mybinary --NAME_out=OUT_DIR
```
> Copied from [here](https://developers.google.com/protocol-buffers/docs/reference/cpp/google.protobuf.compiler.plugin).

## Resources
- [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
- [Protocol Buffer Development Guide](https://developers.google.com/protocol-buffers/docs/overview)
- [Buf](https://buf.build)
- [Buf Style Guide](https://buf.build/docs/style-guide)