namespace Reader

open Grpc.Core
open Google.Protobuf.WellKnownTypes
open MongoDB.Bson
open MongoDB.Driver
open Serilog
open Shared
open System
open System.Text

module PageToken =
    let tryDecode (token:string) =
        try
            if isNull token then Ok None
            else
                token
                |> Convert.FromBase64String
                |> Encoding.UTF8.GetString
                |> ObjectId.Parse
                |> Some
                |> Ok
        with ex ->
            Log.Error(ex, "failed to parse token: {Token}", token)
            Error (sprintf "token invalid: %s" token) 
    let encode (oid:ObjectId option) =
        match oid with
        | Some oid ->
            oid.ToString()
            |> Encoding.UTF8.GetBytes
            |> Convert.ToBase64String
        | None -> ""

module PageSize =
    let validate (pageSize:int) =
        if pageSize = 0 then Ok 10
        elif pageSize > 50 then sprintf "invalid page size: %i; page size must <= 50" pageSize |> Error
        else Ok pageSize

module InventoriedVehicleDto =
    let toProto (dto:Dto.InventoriedVehicleDto) =
        let status =
            match dto.status with
            | "" -> CosmicDealership.Vehicle.V1.VehicleStatus.Unspecified
            | "Available" -> CosmicDealership.Vehicle.V1.VehicleStatus.Available
            | "Leased" -> CosmicDealership.Vehicle.V1.VehicleStatus.Leased
            | other -> failwithf "invalid vehicle status: %s" other
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make=dto.make,
                Model=dto.model,
                Year=dto.year)
        let inventoriedVehicle =
            CosmicDealership.Vehicle.V1.InventoriedVehicle(
                VehicleId=dto.vehicleId,
                AddedAt=Timestamp.FromDateTimeOffset(dto.addedAt),
                UpdatedAt=Timestamp.FromDateTimeOffset(dto.updatedAt),
                Vehicle=vehicle,
                Status=status,
                Avatar=dto.avatar)
        inventoriedVehicle.Images.AddRange(dto.images)
        inventoriedVehicle

type VehicleQueryServiceImpl(store:Store) =
    inherit CosmicDealership.Vehicle.V1.VehicleQueryService.VehicleQueryServiceBase()

    let vehiclesCollection = store.GetCollection<Dto.InventoriedVehicleDto>("vehicles")
    let log = Log.ForContext<VehicleQueryServiceImpl>()

    let authorize (user:CosmicDealership.User.V1.User) (permission:string) =
        if (user.Permissions |> Seq.contains permission) then
            Ok "authorized"
        else
            Error (sprintf "user %s does not have %s permission" user.UserId permission)

    override _.ListVehicles(req:CosmicDealership.Vehicle.V1.ListVehiclesRequest, context:ServerCallContext) =
        async {
            try
                let authorizeResult = authorize req.User "list:vehicles"
                let pageTokenResult = PageToken.tryDecode req.PageToken
                let pageSizeResult = PageSize.validate req.PageSize
                match authorizeResult, pageTokenResult, pageSizeResult with
                | Ok _, Ok pageToken, Ok pageSize ->
                    let statusFilter =
                        Builders<Dto.InventoriedVehicleDto>.Filter
                            .In((fun doc -> doc.status), ["Available"; "Leased"])
                    let totalCount = vehiclesCollection.CountDocuments(statusFilter)
                    if totalCount > 0L then
                        let idFilter = 
                            match pageToken with
                            | None ->
                                Builders.Filter.Empty
                            | Some token ->
                                Builders<Dto.InventoriedVehicleDto>.Filter.Gt((fun doc -> doc._id), token)
                        let filter = Builders.Filter.And(statusFilter, idFilter)
                        let vehicles =
                            vehiclesCollection
                                .Find(filter)
                                .Limit(Nullable(pageSize))
                                .ToList()
                        let lastId =
                            vehiclesCollection.Find(statusFilter)
                                .SortByDescending(fun v -> box v._id)
                                .Limit(Nullable(1))
                                .ToList()
                            |> Seq.head
                            |> fun v -> v._id
                        let nextPageToken =
                            vehicles
                            |> Seq.tryLast
                            |> Option.bind (fun v -> if v._id = lastId then None else Some v._id)
                            |> PageToken.encode
                        let success =
                            CosmicDealership.Vehicle.V1.ListVehiclesSuccess(
                                TotalCount=totalCount,
                                NextPageToken=nextPageToken)
                        success.Vehicles.AddRange(vehicles |> Seq.map InventoriedVehicleDto.toProto)
                        return CosmicDealership.Vehicle.V1.ListVehiclesResponse(Success=success)
                    else
                        let success =
                            CosmicDealership.Vehicle.V1.ListVehiclesSuccess(
                                TotalCount=totalCount,
                                NextPageToken="")
                        return CosmicDealership.Vehicle.V1.ListVehiclesResponse(Success=success)
                | Ok _, Error pageTokenError, _ ->
                    return CosmicDealership.Vehicle.V1.ListVehiclesResponse(PageTokenInvalid=pageTokenError)
                | Ok _, _, Error pageSizeError ->
                    return CosmicDealership.Vehicle.V1.ListVehiclesResponse(PageSizeInvalid=pageSizeError)
                | Error permissionError, _, _ ->
                    return CosmicDealership.Vehicle.V1.ListVehiclesResponse(PermissionDenied=permissionError)
            with ex ->
                log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.ListAvailableVehicles(req:CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest, context:ServerCallContext) =
        async {
            try
                let pageTokenResult = PageToken.tryDecode req.PageToken
                let pageSizeResult = PageSize.validate req.PageSize
                match pageTokenResult, pageSizeResult with
                | Ok pageToken, Ok pageSize ->
                    let statusFilter =
                        Builders<Dto.InventoriedVehicleDto>.Filter
                            .In((fun doc -> doc.status), ["Available"])
                    let totalCount = vehiclesCollection.CountDocuments(statusFilter)
                    if totalCount > 0L then
                        let idFilter = 
                            match pageToken with
                            | None ->
                                Builders.Filter.Empty
                            | Some token ->
                                Builders<Dto.InventoriedVehicleDto>.Filter.Gt((fun doc -> doc._id), token)
                        let filter = Builders.Filter.And(statusFilter, idFilter)
                        let vehicles =
                            vehiclesCollection
                                .Find(filter)
                                .Limit(Nullable(pageSize))
                                .ToList()
                        let lastId =
                            vehiclesCollection.Find(statusFilter)
                                .SortByDescending(fun v -> box v._id)
                                .Limit(Nullable(1))
                                .ToList()
                            |> Seq.head
                            |> fun v -> v._id
                        let nextPageToken =
                            vehicles
                            |> Seq.tryLast
                            |> Option.bind (fun v -> if v._id = lastId then None else Some v._id)
                            |> PageToken.encode
                        let success =
                            CosmicDealership.Vehicle.V1.ListVehiclesSuccess(
                                TotalCount=totalCount,
                                NextPageToken=nextPageToken)
                        success.Vehicles.AddRange(vehicles |> Seq.map InventoriedVehicleDto.toProto)
                        return CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse(Success=success)
                    else
                        let success =
                            CosmicDealership.Vehicle.V1.ListVehiclesSuccess(
                                TotalCount=totalCount,
                                NextPageToken="")
                        return CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse(Success=success)
                | Error pageTokenError, _ ->
                    return CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse(PageTokenInvalid=pageTokenError)
                | _, Error pageSizeError ->
                    return CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse(PageSizeInvalid=pageSizeError)
            with ex ->
                log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.GetVehicle(req:CosmicDealership.Vehicle.V1.GetVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "get:vehicles" with
                | Ok _ ->
                    let vehicleIdFilter =
                        Builders<Dto.InventoriedVehicleDto>.Filter
                            .Where(fun doc -> doc.vehicleId = req.VehicleId)
                    let vehicle = vehiclesCollection.Find(vehicleIdFilter).FirstOrDefault()
                    if isNull (box vehicle) then
                        let vehicleProto = InventoriedVehicleDto.toProto vehicle
                        return CosmicDealership.Vehicle.V1.GetVehicleResponse(Vehicle=vehicleProto)
                    else
                        let msg = sprintf "Vehicle-%s not found" req.VehicleId 
                        return CosmicDealership.Vehicle.V1.GetVehicleResponse(VehicleNotFound=msg)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.GetVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.GetAvailableVehicle(req:CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest, context:ServerCallContext) =
        async {
            try
                let vehicleIdFilter =
                    Builders<Dto.InventoriedVehicleDto>.Filter
                        .Where(fun doc ->
                            doc.vehicleId = req.VehicleId
                            && doc.status = "Available")
                let vehicle = vehiclesCollection.Find(vehicleIdFilter).FirstOrDefault()
                if isNull (box vehicle) then
                    let vehicleProto = InventoriedVehicleDto.toProto vehicle
                    return CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse(Vehicle=vehicleProto)
                else
                    let msg = sprintf "Vehicle-%s not found" req.VehicleId 
                    return CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse(VehicleNotFound=msg)
            with ex ->
                log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask
