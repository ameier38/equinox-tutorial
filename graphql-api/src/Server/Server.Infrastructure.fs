namespace Server

open FSharp.Data
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open Microsoft.FSharp.Reflection
open System
open System.Collections.Generic

[<Sealed>]
type OptionConverter() =
    inherit JsonConverter()

    override __.CanConvert(t) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

    override __.WriteJson(writer, value, serializer) =
        let value =
            if isNull value then null
            else
                let _,fields = Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(value, value.GetType())
                fields.[0]
        serializer.Serialize(writer, value)

    override __.ReadJson(reader, t, _, serializer) =
        let innerType = t.GetGenericArguments().[0]
        let innerType =
            if innerType.IsValueType then (typedefof<System.Nullable<_>>).MakeGenericType([|innerType|])
            else innerType
        let value = serializer.Deserialize(reader, innerType)
        let cases = FSharpType.GetUnionCases(t)
        if isNull value then FSharpValue.MakeUnion(cases.[0], [||])
        else FSharpValue.MakeUnion(cases.[1], [|value|])

module Helpers =
    let tee f x =
        f x
        x

module JsonHelpers =
    let tryGetJsonProperty (jobj: JObject) prop =
        match jobj.Property(prop) with
        | null -> None
        | p -> Some(p.Value.ToString())

    let getJsonSerializerSettings (converters : JsonConverter seq) =
        JsonSerializerSettings()
        |> Helpers.tee (fun s ->
            s.Converters <- List<JsonConverter>(converters)
            s.ContractResolver <- CamelCasePropertyNamesContractResolver())

    let getJsonSerializer (converters : JsonConverter seq) =
        JsonSerializer()
        |> Helpers.tee (fun c ->
            Seq.iter c.Converters.Add converters
            c.ContractResolver <- CamelCasePropertyNamesContractResolver())

    let private converters : JsonConverter [] = [| OptionConverter() |]

    let jsonSettings = getJsonSerializerSettings converters
    let jsonSerializer = getJsonSerializer converters

    let serialize (o:obj) = JsonConvert.SerializeObject(o, jsonSettings)
    let deserialize<'T> (s:string) = JsonConvert.DeserializeObject<'T>(s, jsonSettings)

module Variables =
    let private makeOption (t : Type) = typedefof<_ option>.MakeGenericType(t)
    let private makeArray (t : Type) = t.MakeArrayType()
    let private makeArrayOption = makeArray >> makeOption
    let readDefs 
        (schema:GraphQL.Types.ISchema) 
        (vardefs:GraphQL.Ast.VariableDefinition list) 
        (variables : Map<string, obj>) =
        let scalarTypes =
            [| "Int", typeof<int>
               "Boolean", typeof<bool>
               "Date", typeof<DateTime>
               "Float", typeof<float>
               "ID", typeof<string>
               "String", typeof<string>
               "URI", typeof<Uri> |]
            |> Map.ofArray
        let schemaTypes =
            schema.TypeMap.ToSeq()
            |> Seq.choose (fun (name, def) -> match def with | :? GraphQL.Types.InputDef as idef -> Some (name, idef.Type) | _ -> None)
            |> Map.ofSeq
        let unwrapOption (t : Type) = 
            if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<_ option>
            then t.GetGenericArguments().[0]
            else failwithf "Expected type to be an Option type, but it is %s." t.Name
        let rec resolveVariableType (inputType : GraphQL.Ast.InputType) =
            match inputType with
            | GraphQL.Ast.NamedType tname ->
                match scalarTypes.TryFind tname with
                | Some t -> makeOption t
                | None ->
                    match schemaTypes.TryFind tname with
                    | Some t -> makeOption t
                    | None -> failwithf "Could not determine variable type \"%s\"." tname
            | GraphQL.Ast.NonNullType t -> resolveVariableType t |> unwrapOption
            | GraphQL.Ast.ListType t -> resolveVariableType t |> makeArrayOption
        let resolveVariableValue (t : Type) (value : obj) =
            match value with
            | :? JToken as token -> token.ToObject(t, JsonHelpers.jsonSerializer)
            | _ -> value
        variables
        |> Seq.map (|KeyValue|)
        |> Seq.choose (fun (key, value) -> 
            vardefs 
            |> List.tryFind (fun def -> def.VariableName = key) 
            |> Option.map (fun def -> key, resolveVariableValue (resolveVariableType def.Type) value))
        |> Map.ofSeq
    let rec parseVariables (schema:GraphQL.Types.ISchema) (varDefs:GraphQL.Ast.VariableDefinition list) (variables:obj) =
        let casted =
            match variables with
            | null -> Map.empty
            | :? string as x when String.IsNullOrWhiteSpace(x) -> Map.empty
            | :? Map<string, obj> as x -> x
            | :? JToken as x -> x.ToObject<Map<string, obj>>(JsonHelpers.jsonSerializer)
            | :? string as x -> JsonConvert.DeserializeObject<Map<string, obj>>(x, JsonHelpers.jsonSettings)
            | _ -> failwithf "Failure deserializing variables. Unexpected variables object format."
        readDefs schema varDefs casted
    let parseVariableDefinitions (query:string) =
        let ast = GraphQL.Parser.parse query
        ast.Definitions
        |> List.choose (function GraphQL.Ast.OperationDefinition def -> Some def.VariableDefinitions | _ -> None)
        |> List.collect id
    let getVariables (schema:GraphQL.Schema<'T>) (varDefs:GraphQL.Ast.VariableDefinition list) (data:Map<string, obj>) =
        match data.TryFind("variables") with
        | Some null -> None
        | Some variables -> parseVariables schema varDefs variables |> Some
        | _ -> None
