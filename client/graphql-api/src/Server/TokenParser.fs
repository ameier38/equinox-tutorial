namespace Server

open FSharp.UMX
open Shared
open System.IdentityModel.Tokens

type TokenParser() =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()

    member _.ParseToken(bearer:string) =
        match bearer with
        | Regex.Match "^Bearer (.+)$" [token] ->
            let parsedToken = tokenHandler.ReadJwtToken(token)
            let permissions =
                parsedToken.Claims
                |> Seq.choose (fun claim ->
                    if claim.Type = "permissions" then Some claim.Value
                    else None)
                |> Seq.toList
            let userId = UMX.tag<userId> parsedToken.Subject
            { UserId = userId; Permissions = permissions }
        | _ -> failwithf "could not parse token"
