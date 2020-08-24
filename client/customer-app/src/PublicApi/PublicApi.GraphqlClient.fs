namespace PublicApi

open Fable.SimpleHttp
open Fable.SimpleJson

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type PublicApiGraphqlClient(url: string, headers: Header list) =
    new(url: string) = PublicApiGraphqlClient(url, [ ])

    member _.GetAvailableVehicle(input: GetAvailableVehicle.InputVariables) =
        async {
            let query = """
                query GetAvailableVehicle($input:GetVehicleInput!) {
                    getAvailableVehicle(input:$input) {
                        __typename
                        ... on VehicleNotFound {
                            __typename
                            message
                        }
                        ... on VehicleState {
                            __typename
                            vehicleId
                            make
                            model
                            year
                            status
                        }
                    }
                }
                
            """
            let! response =
                Http.request url
                |> Http.method POST
                |> Http.headers [ Headers.contentType "application/json"; yield! headers ]
                |> Http.content (BodyContent.Text (Json.stringify { query = query; variables = Some input }))
                |> Http.send

            match response.statusCode with
            | 200 ->
                let response = Json.parseNativeAs<GraphqlSuccessResponse<GetAvailableVehicle.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }

    member _.ListAvailableVehicles(input: ListAvailableVehicles.InputVariables) =
        async {
            let query = """
                query ListAvailableVehicles($input:ListVehiclesInput!) {
                  listAvailableVehicles(input: $input) {
                    vehicles {
                      vehicleId
                      make
                      model
                      status
                    }
                    nextPageToken
                    prevPageToken
                    totalCount
                  }
                }
                
            """
            let! response =
                Http.request url
                |> Http.method POST
                |> Http.headers [ Headers.contentType "application/json"; yield! headers ]
                |> Http.content (BodyContent.Text (Json.stringify { query = query; variables = Some input }))
                |> Http.send

            match response.statusCode with
            | 200 ->
                let response = Json.parseNativeAs<GraphqlSuccessResponse<ListAvailableVehicles.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }
