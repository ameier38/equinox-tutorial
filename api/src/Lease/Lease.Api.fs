module Lease.Api

open Lease.Service
open Suave
open Suave.Operators
open Suave.RequestErrors
open Suave.Filters

type Handle = HttpContext -> AsyncResult<string,string>
type HandlePath<'PathParams> = 'PathParams -> Handle

let JSON = Writers.setMimeType "application/json; charset=utf-8"

let ok s =
    fun (ctx:HttpContext) -> 
        { ctx with 
            response = 
                { ctx.response with 
                    status = HTTP_200.status
                    content = UTF8.bytes s |> Bytes }}

let createHandler 
    (handle:HttpContext -> AsyncResult<string,string>) =
    fun (ctx:HttpContext) ->
        let onSuccess data = ok data ctx |> Some
        let onFailure err = failwith err
        handle ctx |> AsyncResult.bimap onSuccess onFailure

let createPathHandler 
    (handlePath:'PathParams -> HttpContext -> AsyncResult<string,string>) = 
    fun (pathParams:'PathParams) ->
        handlePath pathParams
          |> createHandler


let init (service:Service) =
    choose
        [ path "/lease" >=> choose 
            [ POST >=> (createHandler service.Create) >=> JSON ]
          pathRegex "/lease/[^/]+?$" >=> choose
            [ GET >=> pathScan "/lease/%s" (createPathHandler service.Get) >=> JSON
              DELETE >=> pathScan "/lease/%s" (createPathHandler service.Terminate) >=> JSON ]
          pathRegex "/lease/[^/]+?/[^/]+?$" >=> choose
            [ POST >=> choose
                  [ pathScan "/lease/%s/schedule" (createPathHandler service.SchedulePayment) >=> JSON
                    pathScan "/lease/%s/payment" (createPathHandler service.ReceivePayment) >=> JSON ] ]
          NOT_FOUND "handler not implemented" ]
