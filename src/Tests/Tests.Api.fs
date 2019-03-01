module Tests.Api

open Suave
open System

let postRequest endpoint =
    let uri = new Uri("http://testing.test" + endpoint)
    let emptyReq = HttpRequest.empty
    let req = 
        { emptyReq with 
            url = uri 
            method = HttpMethod.GET 
            rawQuery = rawQuery }
    { HttpContext.empty with request = req }