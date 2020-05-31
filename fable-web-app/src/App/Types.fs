module Types

open FSharp.UMX

type [<Measure>] token
type Token = string<token>

type Vehicle =
    { vehicleId: string
      make: string
      model: string
      year: int }
