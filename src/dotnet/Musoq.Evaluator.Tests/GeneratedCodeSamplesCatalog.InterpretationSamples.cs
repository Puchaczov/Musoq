using System;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    private static GeneratedCodeSample BinaryInterpretation()
    {
        const string query = @"
            binary TlvRecord {
                Type: byte,
                Length: byte,
                Value: byte[Length]
            };
            select
                t.Type,
                t.Length
            from #test.files() f
            cross apply Interpret<TlvRecord>(f.Content) t";

        return BinaryInterpretationSample("Q16_BinaryInterpret", query);
    }

    private static GeneratedCodeSample TextInterpretation()
    {
        const string query = @"
            text LogLine {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                l.Timestamp,
                l.Level,
                l.Message
            from #test.lines() f
            cross apply Parse<LogLine>(f.Line) l";

        return TextInterpretationSample("Q17_TextParse", query);
    }

    private static GeneratedCodeSample BinaryConditionalInterpretation()
    {
        const string query = @"
            binary OptionalPacket {
                HasValue: byte,
                Value: int le when HasValue = 1
            };
            select
                p.HasValue,
                p.Value
            from #test.files() f
            cross apply Interpret<OptionalPacket>(f.Content) p";

        return BinaryInterpretationSample("Q51_BinaryConditionalInterpret", query);
    }

    private static GeneratedCodeSample BinaryStringInterpretation()
    {
        const string query = @"binary TextPacket {
                Length: byte,
                Text: string[Length] ascii trim
            };
            select
                p.Length,
                p.Text
            from #test.files() f
            cross apply Interpret<TextPacket>(f.Content) p";

        return BinaryInterpretationSample("Q52_BinaryStringInterpret", query);
    }

    private static GeneratedCodeSample BinaryComputedInterpretation()
    {
        const string query = @"binary Rectangle {
                Width: int le,
                Height: int le,
                Area: Width * Height
            };
            select
                r.Width,
                r.Height,
                r.Area
            from #test.files() f
            cross apply Interpret<Rectangle>(f.Content) r";

        return BinaryInterpretationSample("Q53_BinaryComputedInterpret", query);
    }

    private static GeneratedCodeSample BinaryNestedInterpretation()
    {
        const string query = @"binary Point {
                X: short le,
                Y: short le
            };
            binary Vertex {
                Id: byte,
                Position: Point
            };
            select
                v.Id,
                v.Position.X as X,
                v.Position.Y as Y
            from #test.files() f
            cross apply Interpret<Vertex>(f.Content) v";

        return BinaryInterpretationSample("Q54_BinaryNestedInterpret", query);
    }

    private static GeneratedCodeSample BinaryInlineArrayInterpretation()
    {
        const string query = @"binary InlineArrayPacket {
                Count: byte,
                Items: { Tag: byte, Value: short le }[Count]
            };
            select
                p.Count,
                it.Tag,
                it.Value
            from #test.files() f
            cross apply Interpret<InlineArrayPacket>(f.Content) p
            cross apply p.Items it";

        return BinaryInterpretationSample("Q55_BinaryInlineArrayInterpret", query);
    }

    private static GeneratedCodeSample BinaryStringRepeatUntilInterpretation()
    {
        const string query = @"binary StringRepeatPacket {
                Names: string[3] ascii repeat until Names = 'END'
            };
            select
                n.Value as Text
            from #test.files() f
            cross apply Interpret<StringRepeatPacket>(f.Content) p
            cross apply p.Names n";

        return BinaryInterpretationSample("Q56_BinaryStringRepeatUntilInterpret", query);
    }

    private static GeneratedCodeSample BinaryInlineRepeatUntilInterpretation()
    {
        const string query = @"binary InlineRepeatPacket {
                Items: { Tag: byte, Value: short le } repeat until Items.Tag = 0
            };
            select
                it.Tag,
                it.Value
            from #test.files() f
            cross apply Interpret<InlineRepeatPacket>(f.Content) p
            cross apply p.Items it";

        return BinaryInterpretationSample("Q57_BinaryInlineRepeatUntilInterpret", query);
    }

    private static GeneratedCodeSample BinaryGenericInterpretation()
    {
        const string query = @"binary GenericItem {
                Value: byte
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary GenericContainer {
                Items: LengthPrefixed<GenericItem>
            };
            select
                d.Value as ItemValue
            from #test.files() f
            cross apply Interpret<GenericContainer>(f.Content) c
            cross apply c.Items.Data d";

        return BinaryInterpretationSample("Q58_BinaryGenericInterpret", query);
    }

    private static GeneratedCodeSample BinaryNestedGenericInterpretation()
    {
        const string query = @"binary ByteValue {
                Value: byte
            };
            binary ShortValue {
                Value: short le
            };
            binary Pair<T, U> {
                LeftItem: T,
                RightItem: U
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary NestedGenericContainer {
                Items: LengthPrefixed<Pair<ByteValue,ShortValue>>
            };
            select
                p.LeftItem.Value as LeftValue,
                p.RightItem.Value as RightValue
            from #test.files() f
            cross apply Interpret<NestedGenericContainer>(f.Content) c
            cross apply c.Items.Data p";

        return BinaryInterpretationSample("Q59_BinaryNestedGenericInterpret", query);
    }

    private static GeneratedCodeSample BinaryBitsRepeatUntilInterpretation()
    {
        const string query = @"binary BitsRepeatPacket {
                Flags: bits[1] repeat until Flags = 0
            };
            select
                f.Value as FlagValue
            from #test.files() file
            cross apply Interpret<BitsRepeatPacket>(file.Content) p
            cross apply p.Flags f";

        return BinaryInterpretationSample("Q60_BinaryBitsRepeatUntilInterpret", query);
    }

    private static GeneratedCodeSample BenchmarkMultipleFilesInterpretation()
    {
        const string query = @"binary SimpleHeader {
                Id: int le,
                Value: int le
            };
            select h.Id, h.Value
            from #test.files() f
            cross apply Interpret<SimpleHeader>(f.Content) h";

        return BenchmarkBinaryInterpretationSample("Q183_BenchmarkInterpretationMultipleFilesMaterialized", query);
    }

    private static GeneratedCodeSample BenchmarkHighThroughputInterpretation()
    {
        const string query = @"binary TinyHeader {
                Id: int le
            };
            select h.Id
            from #test.files() f
            cross apply Interpret<TinyHeader>(f.Content) h";

        return BenchmarkBinaryInterpretationSample("Q184_BenchmarkInterpretationHighThroughputMaterialized", query);
    }

    private static GeneratedCodeSample BinaryInterpretationSample(string name, string query)
    {
        return Interpretation(
            name,
            query,
            InterpretationSchemaProviderFactory.CreateBinary);
    }

    private static GeneratedCodeSample TextInterpretationSample(string name, string query)
    {
        return Interpretation(
            name,
            query,
            InterpretationSchemaProviderFactory.CreateText);
    }

    private static GeneratedCodeSample BenchmarkBinaryInterpretationSample(string name, string query)
    {
        return Interpretation(
            name,
            query,
            InterpretationSchemaProviderFactory.CreateBinary) with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                .WithTableResultMaterialization()
                .WithStabilityAwareScalarReuse()
        };
    }

    private static GeneratedCodeSample Interpretation(
        string name,
        string query,
        Func<Musoq.Schema.ISchemaProvider> createSchemaProvider)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "Interpretation",
            Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
            CreateSchemaProvider = createSchemaProvider
        };
    }
}
