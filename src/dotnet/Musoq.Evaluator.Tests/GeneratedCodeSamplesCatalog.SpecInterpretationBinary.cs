namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationBinaryInterpretationSamples()
    {
        return
        [
            BinaryInterpretationSample(
                "Q302_SpecBinaryPrimitiveEndianMatrix",
                @"
                    binary PrimitiveMatrix {
                        LittleInt: int le,
                        BigInt: int be,
                        LittleShort: short le,
                        BigShort: short be,
                        LittleFloat: float le,
                        LittleDouble: double le
                    };
                    select p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble
                    from #test.files() f
                    cross apply Interpret<PrimitiveMatrix>(f.Content) p"),
            BinaryInterpretationSample(
                "Q303_SpecBinaryBytesAndStrings",
                @"
                    binary EncodedRecord {
                        FixedBytes: byte[4],
                        Length: byte,
                        DynamicBytes: byte[Length],
                        AsciiValue: string[4] ascii trim,
                        Utf8Value: string[8] utf8 rtrim,
                        Utf16Value: string[8] utf16le trim,
                        LatinValue: string[4] latin1 ltrim,
                        NullTermValue: string[8] ascii nullterm
                    };
                    select r.FixedBytes[0], r.DynamicBytes[0], r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue
                    from #test.files() f
                    cross apply Interpret<EncodedRecord>(f.Content) r") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q304_SpecBinaryBitsAlignmentAbsolute",
                @"
                    binary PositionedFlags {
                        Flags: bits[3],
                        _: align[8],
                        NextByte: byte,
                        Signature: int le at 0
                    };
                    select p.Flags, p.NextByte, p.Signature
                    from #test.files() f
                    cross apply Interpret<PositionedFlags>(f.Content) p"),
            BinaryInterpretationSample(
                "Q305_SpecBinaryChecksConstMagicOneOf",
                @"
                    binary ValidatedRecord {
                        Version: byte const 1,
                        Signature: byte[4] magic [0xCA, 0xFE, 0xBA, 0xBE],
                        Kind: string[4] ascii oneOf ['DATA', 'TEST']
                    };
                    select r.Version, r.Signature, r.Kind
                    from #test.files() f
                    cross apply Interpret<ValidatedRecord>(f.Content) r") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q306_SpecBinaryDiscardRepeatUntilEof",
                @"
                    binary Item {
                        Value: byte,
                        _: byte[2]
                    };
                    binary ItemStream {
                        Items: Item repeat until eof
                    };
                    select i.Value
                    from #test.files() f
                    cross apply Interpret<ItemStream>(f.Content) s
                    cross apply s.Items i"),
            BinaryInterpretationSample(
                "Q307_SpecBinarySwitchTaggedUnion",
                @"
                    binary LoginPayload { UserId: int le };
                    binary DataPayload { Size: short le };
                    binary Packet {
                        Type: byte,
                        Length: byte,
                        Payload: switch Type {
                            1 => Login: LoginPayload,
                            2 => Data: DataPayload,
                            _ => Raw: byte[Length]
                        }
                    };
                    select p.Type, p.Payload.Case, p.Payload.Login.UserId, p.Payload.Data.Size, p.Payload.Raw
                    from #test.files() f
                    cross apply Interpret<Packet>(f.Content) p") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q308_SpecBinaryRawSubstream",
                @"
                    binary Packet {
                        Kind: byte,
                        Length: byte,
                        Payload: substream[Length] raw,
                        Checksum: byte
                    };
                    select p.Kind, p.Payload, p.Checksum
                    from #test.files() f
                    cross apply Interpret<Packet>(f.Content) p") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q309_SpecBinaryStructuredSubstreams",
                @"
                    binary Body { A: byte, B: byte };
                    binary Frame {
                        ExactLength: byte,
                        Exact: substream[ExactLength] as Body exact,
                        LaxLength: byte,
                        Lax: substream[LaxLength] as Body lax,
                        InlineLength: byte,
                        Inline: substream[InlineLength] as { A: byte, B: byte }
                    };
                    select f.Exact.A, f.Exact.B, f.Lax.A, f.Lax.B, f.Inline.A, f.Inline.B
                    from #test.files() src
                    cross apply Interpret<Frame>(src.Content) f") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q310_SpecBinaryTryInterpretAndInterpretAt",
                @"
                    binary Header { Magic: int le, Offset: int le };
                    binary Payload { Value: short le };
                    select h.Magic, h.Offset, p.Value
                    from #test.files() f
                    cross apply TryInterpret<Header>(f.Content) h
                    cross apply InterpretAt<Payload>(f.Content, h.Offset) p") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q311_SpecBinaryPartialInterpret",
                @"
                    binary DebugPacket { Magic: int le, Version: byte };
                    select p.ErrorField, p.ErrorMessage, p.BytesConsumed
                    from #test.files() f
                    cross apply PartialInterpret<DebugPacket>(f.Content) p") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            BinaryInterpretationSample(
                "Q312_SpecBinaryInheritance",
                @"
                    binary Base { Id: byte };
                    binary Child extends Base { Version: byte };
                    binary Grandchild extends Child { Flags: byte };
                    select g.Id, g.Version, g.Flags
                    from #test.files() f
                    cross apply Interpret<Grandchild>(f.Content) g"),
            BinaryInterpretationSample(
                "Q313_SpecBinaryTextComposition",
                @"
                    text KeyValue {
                        Key: until ':',
                        Value: rest trim
                    };
                    binary Config {
                        Version: byte,
                        Data: string[20] ascii trim as KeyValue,
                        Checksum: byte
                    };
                    select c.Version, c.Data.Key, c.Data.Value, c.Checksum
                    from #test.files() f
                    cross apply Interpret<Config>(f.Content) c")
        ];
    }
}
