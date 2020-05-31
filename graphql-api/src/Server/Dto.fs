namespace Server

open FSharp.Data
open FSharp.UMX

type User =
    { UserId: UserId
      Permissions: string list }

module User =
    let toProto (user:User) =
        let userProto = Tutorial.User.V1.User(UserId = %user.UserId)
        userProto.Permissions.AddRange(user.Permissions)
        userProto
    let fromObj (o:obj) =
        match o with
        | :? User as user -> user
        | other -> failwithf "could not unbox User from %A" other
    let fromMeta (m:GraphQL.Types.Metadata) =
        match m.TryFind "user" with
        | Some o -> fromObj o
        | None -> failwithf "could not find 'user' key in metadata %A" m

type ListVehiclesInput =
    { PageToken: string option
      PageSize: int option }

type GetVehicleInput =
    { VehicleId: string }

type AddVehicleInput =
    { Make: string
      Model: string
      Year: int }
