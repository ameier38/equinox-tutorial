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

## Resources
- [VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
- [Uber's Prototool](https://github.com/uber/prototool)
- [Uber's Protobuf Style Guide V2](https://github.com/uber/prototool/tree/dev/style)
