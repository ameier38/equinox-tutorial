module Dog.Api

open Serilog
open System
open Suave
open Suave.Successful
open Suave.ServerErrors
open Suave.Operators

type Handle = byte[] -> AsyncResult<string,DogError>

let JSON data =
    OK data 
    >=> Writers.setMimeType "application/json; charset=utf-8"

let createHandler (handle: Handle) : WebPart =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body }} = ctx
        async {
            match! handle body with
            | Ok data -> 
                return! JSON data ctx
            | Error err ->
                let errStr = err.ToString()
                return! INTERNAL_ERROR errStr ctx
        }

let handleGet : Handle =
    fun body ->
        asyncResult {
            let! getRequest =
                body
                |> GetRequestSchema.deserializeFromBytes
                |> Result.bind GetRequestSchema.toDomain
                |> AsyncResult.ofResult
            let! dogStateSchema =
                getRequest
                ||> Projection.dogState handler
            let data =
                dogStateSchema
                |> DogStateSchema.serializeToJson
            return data
        }