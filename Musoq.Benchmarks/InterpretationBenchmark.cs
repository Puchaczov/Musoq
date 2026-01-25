using System.Text;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Comprehensive benchmarks for binary and textual column parsing (Interpretation Schemas).
///     Tests various scenarios from simple primitives to complex nested structures.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class InterpretationBenchmark : BenchmarkBase
{
    // New benchmarks
    private CompiledQuery _binary64BitTypesQuery = null!;
    private CompiledQuery _binaryArrayQuery = null!;
    private CompiledQuery _binaryBitFieldsQuery = null!;
    private CompiledQuery _binaryFloatDoubleQuery = null!;
    private CompiledQuery _binaryHighThroughputQuery = null!;
    private CompiledQuery _binaryMixedEndiannessQuery = null!;
    private CompiledQuery _binaryNullTerminatedStringsQuery = null!;
    private CompiledQuery _binaryStringParsingQuery = null!;
    private CompiledQuery _binaryWithTextPayloadQuery = null!;
    private CompiledQuery _conditionalFieldsQuery = null!;
    private CompiledQuery _deeplyNestedQuery = null!;
    private CompiledQuery _largeBinaryArrayQuery = null!;
    private CompiledQuery _multipleFilesQuery = null!;

    private CompiledQuery _nestedBinarySchemaQuery = null!;

    // Pre-compiled queries for each scenario
    private CompiledQuery _simpleBinaryPrimitivesQuery = null!;
    private CompiledQuery _simpleTextParsingQuery = null!;
    private CompiledQuery _textBetweenQuery = null!;
    private CompiledQuery _textDelimitedFieldsQuery = null!;
    private CompiledQuery _textLargeFieldsQuery = null!;
    private CompiledQuery _textMultiplePatternsQuery = null!;
    private CompiledQuery _textPatternMatchingQuery = null!;
    private CompiledQuery _textTokenParsingQuery = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simpleBinaryPrimitivesQuery = CreateSimpleBinaryPrimitivesQuery();
        _binaryArrayQuery = CreateBinaryArrayQuery();
        _nestedBinarySchemaQuery = CreateNestedBinarySchemaQuery();
        _binaryStringParsingQuery = CreateBinaryStringParsingQuery();
        _binaryWithTextPayloadQuery = CreateBinaryWithTextPayloadQuery();
        _simpleTextParsingQuery = CreateSimpleTextParsingQuery();
        _textDelimitedFieldsQuery = CreateTextDelimitedFieldsQuery();
        _textPatternMatchingQuery = CreateTextPatternMatchingQuery();
        _largeBinaryArrayQuery = CreateLargeBinaryArrayQuery();
        _multipleFilesQuery = CreateMultipleFilesQuery();
        _deeplyNestedQuery = CreateDeeplyNestedQuery();
        _conditionalFieldsQuery = CreateConditionalFieldsQuery();


        _binary64BitTypesQuery = CreateBinary64BitTypesQuery();
        _binaryFloatDoubleQuery = CreateBinaryFloatDoubleQuery();
        _binaryBitFieldsQuery = CreateBinaryBitFieldsQuery();
        _textMultiplePatternsQuery = CreateTextMultiplePatternsQuery();
        _textBetweenQuery = CreateTextBetweenQuery();
        _binaryMixedEndiannessQuery = CreateBinaryMixedEndiannessQuery();
        _textLargeFieldsQuery = CreateTextLargeFieldsQuery();
        _binaryHighThroughputQuery = CreateBinaryHighThroughputQuery();
        _textTokenParsingQuery = CreateTextTokenParsingQuery();
        _binaryNullTerminatedStringsQuery = CreateBinaryNullTerminatedStringsQuery();
    }

    #region Scalability Benchmarks

    /// <summary>
    ///     Benchmark: Multiple files processing
    /// </summary>
    [Benchmark(Description = "Scalability: 100 files with binary data")]
    public Table Scalability_MultipleFiles()
    {
        return _multipleFilesQuery.Run();
    }

    #endregion

    #region Binary Parsing Benchmarks

    /// <summary>
    ///     Benchmark: Simple binary primitives (int, short, byte)
    /// </summary>
    [Benchmark(Description = "Binary: Simple primitives (int, short, byte)")]
    public Table Binary_SimplePrimitives()
    {
        return _simpleBinaryPrimitivesQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Binary array parsing
    /// </summary>
    [Benchmark(Description = "Binary: Array of 100 integers")]
    public Table Binary_Array()
    {
        return _binaryArrayQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Nested binary schemas
    /// </summary>
    [Benchmark(Description = "Binary: Nested schemas (3 levels)")]
    public Table Binary_NestedSchema()
    {
        return _nestedBinarySchemaQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Binary with string parsing
    /// </summary>
    [Benchmark(Description = "Binary: String field with UTF-8 encoding")]
    public Table Binary_StringParsing()
    {
        return _binaryStringParsingQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Binary with embedded text payload
    /// </summary>
    [Benchmark(Description = "Binary: With embedded text payload (as clause)")]
    public Table Binary_WithTextPayload()
    {
        return _binaryWithTextPayloadQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Large binary array (1000 elements)
    /// </summary>
    [Benchmark(Description = "Binary: Large array (1000 integers)")]
    public Table Binary_LargeArray()
    {
        return _largeBinaryArrayQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Deeply nested schema (5 levels)
    /// </summary>
    [Benchmark(Description = "Binary: Deeply nested (5 levels)")]
    public Table Binary_DeeplyNested()
    {
        return _deeplyNestedQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Conditional fields
    /// </summary>
    [Benchmark(Description = "Binary: Conditional fields (when clause)")]
    public Table Binary_ConditionalFields()
    {
        return _conditionalFieldsQuery.Run();
    }

    #endregion

    #region Text Parsing Benchmarks

    /// <summary>
    ///     Benchmark: Simple text parsing
    /// </summary>
    [Benchmark(Description = "Text: Simple until delimiter")]
    public Table Text_SimpleUntil()
    {
        return _simpleTextParsingQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Text with multiple delimited fields
    /// </summary>
    [Benchmark(Description = "Text: Multiple delimited fields (CSV-like)")]
    public Table Text_DelimitedFields()
    {
        return _textDelimitedFieldsQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Text pattern matching
    /// </summary>
    [Benchmark(Description = "Text: Pattern matching (regex)")]
    public Table Text_PatternMatching()
    {
        return _textPatternMatchingQuery.Run();
    }

    #endregion

    #region Additional Binary Benchmarks

    /// <summary>
    ///     Benchmark: 64-bit integer types (long, ulong)
    /// </summary>
    [Benchmark(Description = "Binary: 64-bit types (long, ulong)")]
    public Table Binary_64BitTypes()
    {
        return _binary64BitTypesQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Floating point types (float, double)
    /// </summary>
    [Benchmark(Description = "Binary: Float and Double")]
    public Table Binary_FloatDouble()
    {
        return _binaryFloatDoubleQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Bit field parsing
    /// </summary>
    [Benchmark(Description = "Binary: Bit fields (flags parsing)")]
    public Table Binary_BitFields()
    {
        return _binaryBitFieldsQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Mixed endianness parsing
    /// </summary>
    [Benchmark(Description = "Binary: Mixed endianness (LE + BE)")]
    public Table Binary_MixedEndianness()
    {
        return _binaryMixedEndiannessQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Null-terminated strings
    /// </summary>
    [Benchmark(Description = "Binary: Null-terminated strings")]
    public Table Binary_NullTerminatedStrings()
    {
        return _binaryNullTerminatedStringsQuery.Run();
    }

    /// <summary>
    ///     Benchmark: High throughput - 1000 files with simple headers
    /// </summary>
    [Benchmark(Description = "Scalability: 1000 files high throughput")]
    public Table Binary_HighThroughput()
    {
        return _binaryHighThroughputQuery.Run();
    }

    #endregion

    #region Additional Text Benchmarks

    /// <summary>
    ///     Benchmark: Multiple regex patterns in single schema
    /// </summary>
    [Benchmark(Description = "Text: Multiple patterns (5 regex)")]
    public Table Text_MultiplePatterns()
    {
        return _textMultiplePatternsQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Between delimiters parsing
    /// </summary>
    [Benchmark(Description = "Text: Between delimiters")]
    public Table Text_Between()
    {
        return _textBetweenQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Large text field parsing
    /// </summary>
    [Benchmark(Description = "Text: Large fields (1KB each)")]
    public Table Text_LargeFields()
    {
        return _textLargeFieldsQuery.Run();
    }

    /// <summary>
    ///     Benchmark: Token-based parsing (whitespace delimited)
    /// </summary>
    [Benchmark(Description = "Text: Token parsing (whitespace)")]
    public Table Text_TokenParsing()
    {
        return _textTokenParsingQuery.Run();
    }

    #endregion

    #region Query Creation Methods

    private CompiledQuery CreateSimpleBinaryPrimitivesQuery()
    {
        var query = @"
            binary Header {
                Magic: int le,
                Version: short le,
                Flags: byte
            };
            select h.Magic, h.Version, h.Flags
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[7];
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 0);
        BitConverter.GetBytes((short)0x0100).CopyTo(testData, 4);
        testData[6] = 0xFF;

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryArrayQuery()
    {
        var query = @"
            binary DataPacket {
                Count: byte,
                Values: int[Count] le
            };
            select v.Value from #test.files() f
            cross apply Interpret(f.Content, 'DataPacket') d
            cross apply d.Values v";

        var count = 100;
        var testData = new byte[1 + count * 4];
        testData[0] = (byte)count;
        for (var i = 0; i < count; i++) BitConverter.GetBytes(i * 10).CopyTo(testData, 1 + i * 4);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateNestedBinarySchemaQuery()
    {
        var query = @"
            binary Point { X: int le, Y: int le };
            binary Segment { Start: Point, Finish: Point };
            binary Shape { Outline: Segment };
            select s.Outline.Start.X, s.Outline.Start.Y, s.Outline.Finish.X, s.Outline.Finish.Y
            from #test.files() f
            cross apply Interpret(f.Content, 'Shape') s";

        var testData = new byte[16];
        BitConverter.GetBytes(10).CopyTo(testData, 0);
        BitConverter.GetBytes(20).CopyTo(testData, 4);
        BitConverter.GetBytes(100).CopyTo(testData, 8);
        BitConverter.GetBytes(200).CopyTo(testData, 12);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryStringParsingQuery()
    {
        var query = @"
            binary Record {
                Id: int le,
                Name: string[32] utf8 trim,
                Code: string[8] ascii
            };
            select r.Id, r.Name, r.Code
            from #test.files() f
            cross apply Interpret(f.Content, 'Record') r";

        var testData = new byte[44];
        BitConverter.GetBytes(12345).CopyTo(testData, 0);
        Encoding.UTF8.GetBytes("John Doe".PadRight(32)).CopyTo(testData, 4);
        Encoding.ASCII.GetBytes("ABCD1234").CopyTo(testData, 36);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryWithTextPayloadQuery()
    {
        var query = @"
            text KeyValue {
                Key: until ':',
                Value: rest trim
            };
            binary ConfigPacket {
                Version: byte,
                Config: string[30] utf8 as KeyValue,
                Checksum: byte
            };
            select p.Version, p.Config.Key, p.Config.Value, p.Checksum
            from #test.files() f
            cross apply Interpret(f.Content, 'ConfigPacket') p";

        var testData = new byte[32];
        testData[0] = 1;
        var configText = "hostname:server.example.com".PadRight(30);
        Encoding.UTF8.GetBytes(configText).CopyTo(testData, 1);
        testData[31] = 0xFF;

        var entities = new[] { new BenchmarkBinaryEntity { Name = "config.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateSimpleTextParsingQuery()
    {
        var query = @"
            text LogEntry {
                Timestamp: until ' ',
                Message: rest trim
            };
            select l.Timestamp, l.Message
            from #test.lines() f
            cross apply Parse(f.Text, 'LogEntry') l";

        var entities = new[]
        {
            new BenchmarkTextEntity { Name = "log.txt", Text = "2024-01-15T10:30:00 Application started successfully" }
        };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextDelimitedFieldsQuery()
    {
        var query = @"
            text CsvRow {
                Field1: until ',',
                Field2: until ',',
                Field3: until ',',
                Field4: until ',',
                Field5: rest trim
            };
            select r.Field1, r.Field2, r.Field3, r.Field4, r.Field5
            from #test.lines() f
            cross apply Parse(f.Text, 'CsvRow') r";

        var entities = new[]
            { new BenchmarkTextEntity { Name = "data.csv", Text = "value1,value2,value3,value4,value5" } };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextPatternMatchingQuery()
    {
        var query = @"
            text IpAddress {
                Octet1: pattern '\d+',
                _: literal '.',
                Octet2: pattern '\d+',
                _: literal '.',
                Octet3: pattern '\d+',
                _: literal '.',
                Octet4: pattern '\d+'
            };
            select ip.Octet1, ip.Octet2, ip.Octet3, ip.Octet4
            from #test.lines() f
            cross apply Parse(f.Text, 'IpAddress') ip";

        var entities = new[] { new BenchmarkTextEntity { Name = "ip.txt", Text = "192.168.1.100" } };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateLargeBinaryArrayQuery()
    {
        var query = @"
            binary LargePacket {
                Count: short le,
                Values: int[Count] le
            };
            select v.Value from #test.files() f
            cross apply Interpret(f.Content, 'LargePacket') d
            cross apply d.Values v";

        var count = 1000;
        var testData = new byte[2 + count * 4];
        BitConverter.GetBytes((short)count).CopyTo(testData, 0);
        for (var i = 0; i < count; i++) BitConverter.GetBytes(i).CopyTo(testData, 2 + i * 4);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateMultipleFilesQuery()
    {
        var query = @"
            binary SimpleHeader {
                Id: int le,
                Value: int le
            };
            select h.Id, h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'SimpleHeader') h";

        var entities = new List<BenchmarkBinaryEntity>();
        for (var i = 0; i < 100; i++)
        {
            var testData = new byte[8];
            BitConverter.GetBytes(i).CopyTo(testData, 0);
            BitConverter.GetBytes(i * 100).CopyTo(testData, 4);
            entities.Add(new BenchmarkBinaryEntity { Name = $"file{i}.bin", Content = testData });
        }

        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateDeeplyNestedQuery()
    {
        var query = @"
            binary Level1 { Value: int le };
            binary Level2 { L1: Level1 };
            binary Level3 { L2: Level2 };
            binary Level4 { L3: Level3 };
            binary Level5 { L4: Level4 };
            select d.L4.L3.L2.L1.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Level5') d";

        var testData = BitConverter.GetBytes(42);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateConditionalFieldsQuery()
    {
        var query = @"
            binary ConditionalPacket {
                Type: byte,
                HasPayload: byte,
                PayloadLength: int le when HasPayload <> 0,
                Payload: byte[PayloadLength] when HasPayload <> 0
            };
            select c.Type, c.HasPayload, c.PayloadLength
            from #test.files() f
            cross apply Interpret(f.Content, 'ConditionalPacket') c";

        var testData = new byte[10];
        testData[0] = 0x01;
        testData[1] = 0x01;
        BitConverter.GetBytes(4).CopyTo(testData, 2);
        testData[6] = 0xDE;
        testData[7] = 0xAD;
        testData[8] = 0xBE;
        testData[9] = 0xEF;

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinary64BitTypesQuery()
    {
        var query = @"
            binary LargeNumbers {
                SignedLong: long le,
                UnsignedLong: ulong le,
                AnotherLong: long be,
                FinalLong: ulong be
            };
            select n.SignedLong, n.UnsignedLong, n.AnotherLong, n.FinalLong
            from #test.files() f
            cross apply Interpret(f.Content, 'LargeNumbers') n";

        var testData = new byte[32];
        BitConverter.GetBytes(long.MaxValue / 2).CopyTo(testData, 0);
        BitConverter.GetBytes(ulong.MaxValue / 2).CopyTo(testData, 8);
        Array.Reverse(BitConverter.GetBytes(long.MinValue / 2), 0, 8);
        BitConverter.GetBytes(long.MinValue / 2).Reverse().ToArray().CopyTo(testData, 16);
        BitConverter.GetBytes(ulong.MaxValue / 4).Reverse().ToArray().CopyTo(testData, 24);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryFloatDoubleQuery()
    {
        var query = @"
            binary FloatingPoint {
                SingleLE: float le,
                SingleBE: float be,
                DoubleLE: double le,
                DoubleBE: double be
            };
            select fp.SingleLE, fp.SingleBE, fp.DoubleLE, fp.DoubleBE
            from #test.files() f
            cross apply Interpret(f.Content, 'FloatingPoint') fp";

        var testData = new byte[24];
        BitConverter.GetBytes(3.14159f).CopyTo(testData, 0);
        BitConverter.GetBytes(2.71828f).Reverse().ToArray().CopyTo(testData, 4);
        BitConverter.GetBytes(Math.PI).CopyTo(testData, 8);
        BitConverter.GetBytes(Math.E).Reverse().ToArray().CopyTo(testData, 16);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryBitFieldsQuery()
    {
        var query = @"
            binary Flags {
                RawFlags: byte,
                Id: short le,
                Status: byte
            };
            select fl.RawFlags, fl.Id, fl.Status
            from #test.files() f
            cross apply Interpret(f.Content, 'Flags') fl";

        var testData = new byte[4];
        testData[0] = 0b10101010;
        BitConverter.GetBytes((short)1234).CopyTo(testData, 1);
        testData[3] = 0xFF;

        var entities = new[] { new BenchmarkBinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextMultiplePatternsQuery()
    {
        var query = @"
            text LogLine {
                Date: pattern '\d{4}-\d{2}-\d{2}',
                _: literal ' ',
                Time: pattern '\d{2}:\d{2}:\d{2}',
                _: literal ' ',
                Level: pattern '[A-Z]+',
                _: literal ' ',
                Code: pattern '\d+',
                _: literal ' ',
                Message: rest trim
            };
            select l.Date, l.Time, l.Level, l.Code, l.Message
            from #test.lines() f
            cross apply Parse(f.Text, 'LogLine') l";

        var entities = new[]
            { new BenchmarkTextEntity { Name = "log.txt", Text = "2024-01-15 10:30:45 ERROR 500 Connection timeout" } };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextBetweenQuery()
    {
        var query = @"
            text XmlLike {
                Tag1: between '<tag1>' '</tag1>',
                Tag2: between '<tag2>' '</tag2>',
                Tag3: between '<tag3>' '</tag3>'
            };
            select x.Tag1, x.Tag2, x.Tag3
            from #test.lines() f
            cross apply Parse(f.Text, 'XmlLike') x";

        var entities = new[]
        {
            new BenchmarkTextEntity
                { Name = "data.xml", Text = "<tag1>value1</tag1><tag2>value2</tag2><tag3>value3</tag3>" }
        };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryMixedEndiannessQuery()
    {
        var query = @"
            binary NetworkPacket {
                VersionLE: short le,
                LengthBE: int be,
                ChecksumLE: int le,
                FlagsBE: short be,
                SequenceLE: long le,
                TimestampBE: long be
            };
            select p.VersionLE, p.LengthBE, p.ChecksumLE, p.FlagsBE, p.SequenceLE, p.TimestampBE
            from #test.files() f
            cross apply Interpret(f.Content, 'NetworkPacket') p";

        var testData = new byte[30];
        BitConverter.GetBytes((short)1).CopyTo(testData, 0);
        BitConverter.GetBytes(1024).Reverse().ToArray().CopyTo(testData, 2);
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 6);
        BitConverter.GetBytes(unchecked((short)0xFF00)).Reverse().ToArray().CopyTo(testData, 10);
        BitConverter.GetBytes(123456789L).CopyTo(testData, 12);
        BitConverter.GetBytes(DateTime.UtcNow.Ticks).Reverse().ToArray().CopyTo(testData, 20);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "packet.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextLargeFieldsQuery()
    {
        var query = @"
            text LargeRecord {
                Field1: until '|',
                Field2: until '|',
                Field3: rest trim
            };
            select r.Field1, r.Field2, r.Field3
            from #test.lines() f
            cross apply Parse(f.Text, 'LargeRecord') r";


        var field1 = new string('A', 1024);
        var field2 = new string('B', 1024);
        var field3 = new string('C', 1024);
        var entities = new[] { new BenchmarkTextEntity { Name = "data.txt", Text = $"{field1}|{field2}|{field3}" } };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryHighThroughputQuery()
    {
        var query = @"
            binary TinyHeader {
                Id: int le
            };
            select h.Id
            from #test.files() f
            cross apply Interpret(f.Content, 'TinyHeader') h";

        var entities = new List<BenchmarkBinaryEntity>();
        for (var i = 0; i < 1000; i++)
        {
            var testData = BitConverter.GetBytes(i);
            entities.Add(new BenchmarkBinaryEntity { Name = $"file{i}.bin", Content = testData });
        }

        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateTextTokenParsingQuery()
    {
        var query = @"
            text CommandLine {
                Command: token,
                _: whitespace,
                Arg1: token,
                _: whitespace,
                Arg2: token,
                _: whitespace,
                Arg3: rest trim
            };
            select c.Command, c.Arg1, c.Arg2, c.Arg3
            from #test.lines() f
            cross apply Parse(f.Text, 'CommandLine') c";

        var entities = new[]
            { new BenchmarkTextEntity { Name = "cmd.txt", Text = "git commit -m \"Initial commit message\"" } };
        var schemaProvider = new BenchmarkTextSchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkTextEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CreateBinaryNullTerminatedStringsQuery()
    {
        var query = @"
            binary StringRecord {
                Name: string[32] utf8 nullterm,
                Description: string[64] utf8 nullterm,
                Tag: string[16] ascii nullterm
            };
            select r.Name, r.Description, r.Tag
            from #test.files() f
            cross apply Interpret(f.Content, 'StringRecord') r";

        var testData = new byte[112];
        var name = Encoding.UTF8.GetBytes("TestName\0");
        var desc = Encoding.UTF8.GetBytes("This is a test description\0");
        var tag = Encoding.ASCII.GetBytes("TAG001\0");
        name.CopyTo(testData, 0);
        desc.CopyTo(testData, 32);
        tag.CopyTo(testData, 96);

        var entities = new[] { new BenchmarkBinaryEntity { Name = "strings.bin", Content = testData } };
        var schemaProvider = new BenchmarkBinarySchemaProvider(
            new Dictionary<string, IEnumerable<BenchmarkBinaryEntity>> { { "#test", entities } });

        return CompileQuery(query, schemaProvider);
    }

    private CompiledQuery CompileQuery(string query, ISchemaProvider schemaProvider)
    {
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new BenchmarkLoggerResolver(),
            options);
    }

    #endregion
}

#region Benchmark Infrastructure

/// <summary>
///     Entity with binary content for benchmarks.
/// </summary>
public class BenchmarkBinaryEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Content), 1 }
    };

    public static readonly IReadOnlyDictionary<int, Func<BenchmarkBinaryEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BenchmarkBinaryEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Content }
        };

    public string Name { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

/// <summary>
///     Entity with text content for benchmarks.
/// </summary>
public class BenchmarkTextEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Text), 1 }
    };

    public static readonly IReadOnlyDictionary<int, Func<BenchmarkTextEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BenchmarkTextEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Text }
        };

    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
///     Binary entity table for benchmarks.
/// </summary>
public class BenchmarkBinaryEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new BenchmarkSchemaColumn(nameof(BenchmarkBinaryEntity.Name), 0, typeof(string)),
        new BenchmarkSchemaColumn(nameof(BenchmarkBinaryEntity.Content), 1, typeof(byte[]))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BenchmarkBinaryEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}

/// <summary>
///     Text entity table for benchmarks.
/// </summary>
public class BenchmarkTextEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new BenchmarkSchemaColumn(nameof(BenchmarkTextEntity.Name), 0, typeof(string)),
        new BenchmarkSchemaColumn(nameof(BenchmarkTextEntity.Text), 1, typeof(string))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BenchmarkTextEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}

/// <summary>
///     Schema column for benchmarks.
/// </summary>
public class BenchmarkSchemaColumn : ISchemaColumn
{
    public BenchmarkSchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}

/// <summary>
///     Binary schema for benchmarks.
/// </summary>
public class BenchmarkBinarySchema : SchemaBase
{
    private readonly IEnumerable<BenchmarkBinaryEntity> _entities;

    public BenchmarkBinarySchema(IEnumerable<BenchmarkBinaryEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkBinaryEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkEntitySource<BenchmarkBinaryEntity>(
            _entities,
            BenchmarkBinaryEntity.NameToIndexMap,
            BenchmarkBinaryEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}

/// <summary>
///     Text schema for benchmarks.
/// </summary>
public class BenchmarkTextSchema : SchemaBase
{
    private readonly IEnumerable<BenchmarkTextEntity> _entities;

    public BenchmarkTextSchema(IEnumerable<BenchmarkTextEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkTextEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkEntitySource<BenchmarkTextEntity>(
            _entities,
            BenchmarkTextEntity.NameToIndexMap,
            BenchmarkTextEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}

/// <summary>
///     Binary schema provider for benchmarks.
/// </summary>
public class BenchmarkBinarySchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BenchmarkBinaryEntity>> _values;

    public BenchmarkBinarySchemaProvider(IDictionary<string, IEnumerable<BenchmarkBinaryEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BenchmarkBinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}

/// <summary>
///     Text schema provider for benchmarks.
/// </summary>
public class BenchmarkTextSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BenchmarkTextEntity>> _values;

    public BenchmarkTextSchemaProvider(IDictionary<string, IEnumerable<BenchmarkTextEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BenchmarkTextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}

/// <summary>
///     Generic entity source for benchmarks.
/// </summary>
public class BenchmarkEntitySource<T> : RowSource
{
    private readonly IEnumerable<T> _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public BenchmarkEntitySource(
        IEnumerable<T> entities,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities)
                yield return new BenchmarkEntityResolver<T>(entity, _nameToIndexMap, _indexToObjectAccessMap);
        }
    }
}

/// <summary>
///     Generic entity resolver for benchmarks.
/// </summary>
public class BenchmarkEntityResolver<T> : IObjectResolver
{
    private readonly T _entity;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public BenchmarkEntityResolver(
        T entity,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entity = entity;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public object[] Contexts => [];

    public object this[string name]
    {
        get
        {
            if (_nameToIndexMap.TryGetValue(name, out var index))
                return _indexToObjectAccessMap[index](_entity);
            return null!;
        }
    }

    public object this[int index]
    {
        get
        {
            if (_indexToObjectAccessMap.TryGetValue(index, out var accessor))
                return accessor(_entity);
            return null!;
        }
    }

    public bool HasColumn(string name)
    {
        return _nameToIndexMap.ContainsKey(name);
    }
}

#endregion
