namespace PublicClient

open Fable.Remoting.Json
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Net.Http
open System.Text

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type PublicGraphqlClient(url: string, httpClient: HttpClient) =
    let converter = FableJsonConverter() :> JsonConverter
    let settings = JsonSerializerSettings(DateParseHandling=DateParseHandling.None, Converters = [| converter |])

    new(url: string) = PublicGraphqlClient(url, new HttpClient())

    member _.GetAvailableVehicleAsync(input: GetAvailableVehicle.InputVariables) =
        async {
            let query = """
                query GetAvailableVehicle($input: GetVehicleInput!) {
                    getAvailableVehicle(input: $input) {
                        __typename
                        ... on VehicleNotFound {
                            __typename
                            message
                        }
                        ... on Vehicle {
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

            let inputJson = JsonConvert.SerializeObject({ query = query; variables = Some input }, [| converter |])

            let! response =
                httpClient.PostAsync(url, new StringContent(inputJson, Encoding.UTF8, "application/json"))
                |> Async.AwaitTask

            let! responseContent = Async.AwaitTask(response.Content.ReadAsStringAsync())
            let responseJson = JsonConvert.DeserializeObject<JObject>(responseContent, settings)

            match response.IsSuccessStatusCode with
            | true ->
                let errorsReturned =
                    responseJson.ContainsKey "errors"
                    && responseJson.["errors"].Type = JTokenType.Array
                    && responseJson.["errors"].HasValues

                if errorsReturned then
                    let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                    return Error response.errors
                else
                    let response = responseJson.ToObject<GraphqlSuccessResponse<GetAvailableVehicle.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.GetAvailableVehicle(input: GetAvailableVehicle.InputVariables) = Async.RunSynchronously(this.GetAvailableVehicleAsync input)

    member _.ListAvailableVehiclesAsync(input: ListAvailableVehicles.InputVariables) =
        async {
            let query = """
                query ListAvailableVehicles($input:ListVehiclesInput!) {
                    listAvailableVehicles(input:$input) {
                        totalCount
                        prevPageToken
                        nextPageToken
                        vehicles {
                            vehicleId
                            make
                            model
                            year
                        }
                    }
                }
                
            """

            let inputJson = JsonConvert.SerializeObject({ query = query; variables = Some input }, [| converter |])

            let! response =
                httpClient.PostAsync(url, new StringContent(inputJson, Encoding.UTF8, "application/json"))
                |> Async.AwaitTask

            let! responseContent = Async.AwaitTask(response.Content.ReadAsStringAsync())
            let responseJson = JsonConvert.DeserializeObject<JObject>(responseContent, settings)

            match response.IsSuccessStatusCode with
            | true ->
                let errorsReturned =
                    responseJson.ContainsKey "errors"
                    && responseJson.["errors"].Type = JTokenType.Array
                    && responseJson.["errors"].HasValues

                if errorsReturned then
                    let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                    return Error response.errors
                else
                    let response = responseJson.ToObject<GraphqlSuccessResponse<ListAvailableVehicles.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.ListAvailableVehicles(input: ListAvailableVehicles.InputVariables) = Async.RunSynchronously(this.ListAvailableVehiclesAsync input)
