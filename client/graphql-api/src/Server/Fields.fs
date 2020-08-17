module Server.Fields

open FSharp.Data.GraphQL.Types

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
