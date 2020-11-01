namespace PublicClient

open Fable.SimpleHttp
open Fable.SimpleJson

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type PublicGraphqlClient(url: string, headers: Header list) =
    new(url: string) = PublicGraphqlClient(url, [ ])

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
                        ... on InventoriedVehicle {
                            __typename
                            vehicleId
                            addedAt
                            updatedAt
                            vehicle {
                                make
                                model
                                year
                            }
                            status
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
                        __typename
                        ... on PageTokenInvalid {
                            __typename
                            message
                        }
                        ... on PageSizeInvalid {
                            __typename
                            message
                        }
                        ... on ListVehiclesSuccess {
                            __typename
                            totalCount
                            nextPageToken
                            vehicles {
                                vehicleId
                                addedAt
                                avatar
                                status
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
                let response = Json.parseNativeAs<GraphqlSuccessResponse<ListAvailableVehicles.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }
