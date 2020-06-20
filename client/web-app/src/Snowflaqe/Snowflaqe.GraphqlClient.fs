namespace Snowflaqe

open Fable.SimpleHttp
open Fable.SimpleJson

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type SnowflaqeGraphqlClient(url: string, headers: Header list) =
    new(url: string) = SnowflaqeGraphqlClient(url, [ ])

    member _.ListVehicles(input: ListVehicles.InputVariables) =
        async {
            let query = """
                query ListVehicles($input:ListVehiclesInput!) {
                  listVehicles(input: $input) {
                    vehicles {
                      vehicleId
                      make
                      model
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
                let response = Json.parseNativeAs<GraphqlSuccessResponse<ListVehicles.Query>> response.responseText
                return Ok response.data

            | errorStatus ->
                let response = Json.parseNativeAs<GraphqlErrorResponse> response.responseText
                return Error response.errors
        }
