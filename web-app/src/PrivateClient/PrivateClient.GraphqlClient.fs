namespace PrivateClient

open Fable.SimpleHttp
open Fable.SimpleJson

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type PrivateGraphqlClient(url: string, headers: Header list) =
    new(url: string) = PrivateGraphqlClient(url, [ ])

    member _.AddVehicle(input: AddVehicle.InputVariables) =
        async {
            let query = """
                mutation AddVehicle($input:AddVehicleInput!) {
                  addVehicle(input: $input) {
                      __typename
                      ... on Success {
                          __typename
                          message
                      }
                      ... on VehicleAlreadyExists {
                          __typename
                          message
                      }
                      ... on VehicleInvalid {
                          __typename
                          message
                      }
                      ... on PermissionDenied {
                          __typename
                          message
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
                let response = Json.parseNativeAs<GraphqlSuccessResponse<AddVehicle.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }

    member _.GetVehicle(input: GetVehicle.InputVariables) =
        async {
            let query = """
                query GetVehicle($input: GetVehicleInput!) {
                    getVehicle(input: $input) {
                        __typename
                        ... on VehicleNotFound {
                            __typename
                            message
                        }
                        ... on PermissionDenied {
                            __typename
                            message
                        }
                        ... on InventoriedVehicle {
                            __typename
                            vehicleId
                            addedAt
                            updatedAt
                            status
                            vehicle {
                                make
                                model
                                year
                            }
                            avatar
                            images
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
                let response = Json.parseNativeAs<GraphqlSuccessResponse<GetVehicle.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }

    member _.ListVehicles(input: ListVehicles.InputVariables) =
        async {
            let query = """
                query ListVehicles($input:ListVehiclesInput!) {
                    listVehicles(input:$input) {
                        __typename
                        ... on PageTokenInvalid {
                            __typename
                            message
                        }
                        ... on PageSizeInvalid {
                            __typename
                            message
                        }
                        ... on PermissionDenied {
                            __typename
                            message
                        }
                        ... on ListVehiclesSuccess {
                            __typename
                            totalCount
                            nextPageToken
                            vehicles {
                                vehicleId
                                avatar
                                addedAt
                                vehicle {
                                    make
                                    model
                                    year
                                }
                            }
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
                let response = Json.parseNativeAs<GraphqlSuccessResponse<ListVehicles.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }
