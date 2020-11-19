# F# Action
Action to build F# projects

## Inputs

### `root`
**Required** The directory in which the action should be run.

### `target`
**Required** The build target to run.

## Example Usage
```yaml
uses: ./.github/fsharp
with:
    root: ./vehicle
    target: UpdateProtos
```
