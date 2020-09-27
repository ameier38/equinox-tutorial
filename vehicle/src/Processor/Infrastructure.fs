namespace Server

module Result =
    type ResultBuilder() =
        member _.Bind(x, f) =
            match x with
            | Error error -> Error error
            | Ok v -> f v
        member _.Return(x) =
            Ok x

    module Builders =
        let result = ResultBuilder()
    