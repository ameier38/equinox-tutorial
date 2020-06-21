namespace Server

open FSharp.Data.GraphQL.Types
open FSharp.UMX
open MongoDB.Bson

type [<Measure>] userId
type UserId = string<userId>

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
    let fromMetadata (m:Metadata) =
        match m.TryFind "user" with
        | Some o -> fromObj o
        | None -> failwithf "could not find 'user' key in metadata %A" m

type ListVehiclesInput =
    { pageToken: string option
      pageSize: int option }

type GetVehicleInput =
    { vehicleId: string }

type AddVehicleInput =
    { make: string
      model: string
      year: int }

type VehicleState =
    { _id: ObjectId
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

type ListVehiclesResponse =
    { vehicles: VehicleState list
      totalCount: int64
      prevPageToken: string
      nextPageToken: string }

type GraphQLQuery =
    { ExecutionPlan : ExecutionPlan
      Variables : Map<string, obj> }
