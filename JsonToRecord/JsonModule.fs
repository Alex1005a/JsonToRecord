module JsonModule
open FSharp.Data
open System

let private tryParseInt (s: decimal) = 
    try 
        s.ToString() |> int |> ignore
        true
    with :? FormatException -> 
        false

let private tryParseDateTime (d: string) = 
    match System.DateTime.TryParse d with
    | true, _ -> true
    | _ -> false

let private tryParseGuid (d: string) = 
    match System.Guid.TryParse d with
    | true, _ -> true
    | _ -> false

let rec private jsonValueToTypeString (value: JsonValue) =
    match value with
    | JsonValue.Boolean _ -> "bool"
    | JsonValue.Float _ -> "float"
    | JsonValue.Array [||] -> "? array"
    | JsonValue.Array v -> jsonValueToTypeString v.[0] + " array"
    | JsonValue.Number d -> if tryParseInt d then "int" else "decimal"
    | JsonValue.String s when tryParseDateTime s -> "DateTime"
    | JsonValue.String s when tryParseGuid s -> "Guid"
    | JsonValue.String _ -> "string"
    | _ -> "?"

let private jsonRecordToRecord (record: (string * JsonValue) [] ) =
    match record with
    | [||] -> Error("Json is empty")
    | _ -> Ok(record |> Array.map(fun (s, t) -> s + ": " + jsonValueToTypeString t + "; ")
                     |> Array.reduce(fun a t -> a + t))
    
let rec private jsonValueToRecord value =
    match value with
    | JsonValue.Array arr -> jsonValueToRecord arr.[0]
    | JsonValue.Record record -> jsonRecordToRecord record
    | _ -> Error("Error")

let jsonToRecord json name =
    let info = JsonValue.TryParse(json)
    match info with
    | Some v -> 
        let result =  jsonValueToRecord v
        match result with
        | Ok(res) -> "type " + name + " = { " + res + "}"
        | Error(v) -> v
    | None -> "Error json parsing"