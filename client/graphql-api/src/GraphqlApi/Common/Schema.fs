module GraphqlApi.Common.Schema

open FSharp.Data.GraphQL.Types
open GraphqlApi.Common.Types

let pageSizeInputField = 
    Define.Input(
        name = "pageSize", 
        typedef = Nullable Int,
        description = "Maximum number of items in a page")

let pageTokenInputField = 
    Define.Input(
        name = "pageToken", 
        typedef = Nullable ID,
        description = "Token for page to retrieve; Empty string for first page")

let SuccessType =
    Define.Object<Message>(name = "Success", fields = [ Define.AutoField("message", String) ])

let PermissionDeniedType =
    Define.Object<Message>(name = "PermissionDenied", fields = [ Define.AutoField("message", String) ])
