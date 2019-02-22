namespace Dog.Codec

type OAttribute = System.Runtime.InteropServices.OptionalAttribute
type DAttribute = System.Runtime.InteropServices.DefaultParameterValueAttribute

open Newtonsoft.Json
open System
open System.IO
open TypeShape
open Dog.Ouroboros

/// Represents an encoded event
type EncodedEvent<'Encoding> = 
    { createdDate: DateTime
      effectiveDate: DateTime 
      effectiveOrder: int
      caseName: string
      payload: 'Encoding }

type Encode<'Union,'Encoding> = Event<'Union> -> EncodedEvent<'Encoding>
type TryDecode<'Union,'Encoding> = EncodedEvent<'Encoding> -> Event<'Union> option

/// Defines a contract interpreter for a Discriminated Union representing a set of events borne by a stream
type IEventEncoder<'Union,'Encoding> =
    /// Encodes a union instance into a decoded representation
    abstract Encode: value:Event<'Union> -> EncodedEvent<'Encoding>
    /// Decodes a formatted representation into a union instance. Does not throw exception on format mismatches
    abstract TryDecode: encodedEvent:EncodedEvent<'Encoding> -> Event<'Union> option

module private Impl =
    /// Newtonsoft.Json implementation of IEncoder that encodes direct to a UTF-8 Buffer
    type JsonUtf8Encoder(settings : JsonSerializerSettings) =
        let serializer = JsonSerializer.Create(settings)
        interface UnionContract.IEncoder<byte[]> with
            member __.Empty = Unchecked.defaultof<_>
            member __.Encode (value : 'T) =
                use ms = new MemoryStream()
                (   use jsonWriter = new JsonTextWriter(new StreamWriter(ms))
                    serializer.Serialize(jsonWriter, value, typeof<'T>))
                ms.ToArray()
            member __.Decode(json : byte[]) =
                use ms = new MemoryStream(json)
                use jsonReader = new JsonTextReader(new StreamReader(ms))
                serializer.Deserialize<'T>(jsonReader)

    /// Provide an IUnionContractEncoder based on a pair of encode and a tryDecode methods
    type EncodeTryDecodeCodec<'Union,'Encoding>(encode: Encode<'Union,'Encoding>, tryDecode: TryDecode<'Union,'Encoding>) =
        interface IEventEncoder<'Union,'Encoding> with
            member __.Encode e = encode e
            member __.TryDecode ee = tryDecode ee


/// Provides Codecs that render to a UTF-8 array suitable for storage in EventStore or CosmosDb.
type JsonUtf8 =

    /// <summary>
    ///     Generate a codec suitable for use with <c>Equinox.EventStore</c> or <c>Equinox.Cosmos</c>,
    ///       using the supplied `Newtonsoft.Json` <c>settings</c>.
    ///     The Event Type Names are inferred based on either explicit `DataMember(Name=` Attributes,
    ///       or (if unspecified) the Discriminated Union Case Name
    ///     The Union must be tagged with `interface TypeShape.UnionContract.IUnionContract` to signify this scheme applies.
    ///     See https://github.com/eiriktsarpalis/TypeShape/blob/master/tests/TypeShape.Tests/UnionContractTests.fs for example usage.</summary>
    /// <param name="settings">Configuration to be used by the underlying <c>Newtonsoft.Json</c> Serializer when encoding/decoding.</param>
    /// <param name="requireRecordFields">Fail encoder generation if union cases contain fields that are not F# records. Defaults to <c>false</c>.</param>
    /// <param name="allowNullaryCases">Fail encoder generation if union contains nullary cases. Defaults to <c>true</c>.</param>
    static member Create<'Union when 'Union :> UnionContract.IUnionContract>(settings, [<O;D(null)>]?requireRecordFields, [<O;D(null)>]?allowNullaryCases)
        : IEventEncoder<'Union,byte[]> =
        let inner =
            UnionContract.UnionContractEncoder.Create<'Union,byte[]>(
                new Impl.JsonUtf8Encoder(settings),
                ?requireRecordFields=requireRecordFields,
                ?allowNullaryCases=allowNullaryCases)
        { new IEventEncoder<'Union,byte[]> with
            member __.Encode value =
                let r = inner.Encode value.DomainEvent
                { createdDate = value.CreatedDate
                  effectiveDate = value.EffectiveDate
                  effectiveOrder = value.EffectiveOrder
                  caseName = r.CaseName
                  payload = r.Payload }
            member __.TryDecode encoded =
                inner.TryDecode { CaseName = encoded.caseName; Payload = encoded.payload }
                |> Option.map (fun domainEvent ->
                    { CreatedDate = encoded.createdDate
                      EffectiveDate = encoded.effectiveDate
                      EffectiveOrder = encoded.effectiveOrder
                      DomainEvent = domainEvent }
                ) }
