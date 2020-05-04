module Graphql

open Fable.SimpleHttp
open Fable.SimpleJson

type VehicleDto =
    { vehicleId: string
      make: string
      model: string
      year: int }

type ListVehiclesInputDto =
    { pageToken: string
      pageSize: int }

type ListVehiclesResponseDto =
    { vehicles: VehicleDto list
      prevPageToken: string
      nextPageToken: string
      totalCount: int }

let listVehiclesQuery = """
query ListVehicles($input:ListVehiclesInput!) {
    listVehicles(input: $input) {
        vehicles {
            vehicleId
            make
            model
            year
        }
        prevPageToken
        nextPageToken
        totalCount
    }
}
"""

type Variables<'T> =
    { input: 'T }

type Request<'T> =
    { query: string
      variables: Variables<'T> }

type ResponseData =
    { listVehicles: ListVehiclesResponseDto option }

type Response =
    { data: ResponseData
      errors: (string list) option }

type GraphqlClient(config:Config.GraphqlClientConfig) =
    let post (f:ResponseData -> 'T) (data:string) =
        async {
            try
                sprintf "url: %s" config.Url |> Log.info
                let! (statusCode, responseData) = Http.post config.Url data
                let parsedResponse = Json.parseAs<Response> responseData
                return
                    match statusCode with
                    | 200 -> Ok (f parsedResponse.data)
                    | other ->
                        let error = sprintf "Error %i! %A" other parsedResponse.errors
                        Log.error error
                        Error error
            with ex ->
                let error = sprintf "Error! %A" ex
                Log.error error
                return Error error
        }

    member _.ListVehicles(input:ListVehiclesInputDto) =
        async {
            let requestData =
                { query = listVehiclesQuery
                  variables = { input = input } }
                |> Json.stringify
            let mapper (responseData:ResponseData) =
                match responseData.listVehicles with
                | Some listVehiclesResponse -> listVehiclesResponse
                | None -> failwithf "listVehicles not found"
            let! response = post mapper requestData
            return response
        }

let graphqlClient = GraphqlClient(Config.graphqlClientConfig)
