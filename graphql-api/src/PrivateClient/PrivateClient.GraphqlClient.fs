namespace PrivateClient

open Fable.Remoting.Json
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Net.Http
open System.Text

type GraphqlInput<'T> = { query: string; variables: Option<'T> }
type GraphqlSuccessResponse<'T> = { data: 'T }
type GraphqlErrorResponse = { errors: ErrorType list }

type PrivateGraphqlClient(url: string, httpClient: HttpClient) =
    let converter = FableJsonConverter() :> JsonConverter
    let settings = JsonSerializerSettings(DateParseHandling=DateParseHandling.None, Converters = [| converter |])

    new(url: string) = PrivateGraphqlClient(url, new HttpClient())

    member _.AddVehicleAsync(input: AddVehicle.InputVariables) =
        async {
            let query = """
                mutation AddVehicle($input:AddVehicleInput!) {
                    addVehicle(input: $input) {
                        __typename
                        ...on Success {
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<AddVehicle.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.AddVehicle(input: AddVehicle.InputVariables) = Async.RunSynchronously(this.AddVehicleAsync input)

    member _.AddVehicleImageAsync(input: AddVehicleImage.InputVariables) =
        async {
            let query = """
                mutation AddVehicleImage($input:AddVehicleImageInput!) {
                    addVehicleImage(input: $input) {
                        __typename
                        ...on Success {
                            __typename
                            message
                        }
                        ... on VehicleNotFound {
                            __typename
                            message
                        }
                        ... on ImageInvalid {
                            __typename
                            message
                        }
                        ... on MaxImageCountReached {
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<AddVehicleImage.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.AddVehicleImage(input: AddVehicleImage.InputVariables) = Async.RunSynchronously(this.AddVehicleImageAsync input)

    member _.GetVehicleAsync(input: GetVehicle.InputVariables) =
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
                            status
                            vehicle {
                                make
                                model
                                year
                            }
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<GetVehicle.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.GetVehicle(input: GetVehicle.InputVariables) = Async.RunSynchronously(this.GetVehicleAsync input)

    member _.ListVehiclesAsync(input: ListVehicles.InputVariables) =
        async {
            let query = """
                query ListVehicles($input:ListVehiclesInput!) {
                    listVehicles(input:$input) {
                        __typename
                        ... on PermissionDenied {
                            __typename
                            message
                        }
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<ListVehicles.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.ListVehicles(input: ListVehicles.InputVariables) = Async.RunSynchronously(this.ListVehiclesAsync input)

    member _.RemoveVehicleAsync(input: RemoveVehicle.InputVariables) =
        async {
            let query = """
                mutation RemoveVehicle($input:RemoveVehicleInput!) {
                    removeVehicle(input: $input) {
                        __typename
                        ...on Success {
                            __typename
                            message
                        }
                        ... on VehicleNotFound {
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<RemoveVehicle.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.RemoveVehicle(input: RemoveVehicle.InputVariables) = Async.RunSynchronously(this.RemoveVehicleAsync input)

    member _.UpdateVehicleAsync(input: UpdateVehicle.InputVariables) =
        async {
            let query = """
                mutation UpdateVehicle($input:UpdateVehicleInput!) {
                    updateVehicle(input:$input) {
                        __typename
                        ...on Success {
                            __typename
                            message
                        }
                        ... on VehicleNotFound {
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
                    let response = responseJson.ToObject<GraphqlSuccessResponse<UpdateVehicle.Query>>(JsonSerializer.Create(settings))
                    return Ok response.data

            | errorStatus ->
                let response = responseJson.ToObject<GraphqlErrorResponse>(JsonSerializer.Create(settings))
                return Error response.errors
        }

    member this.UpdateVehicle(input: UpdateVehicle.InputVariables) = Async.RunSynchronously(this.UpdateVehicleAsync input)
