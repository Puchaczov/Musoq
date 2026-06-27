using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsInterpretationSchemasTests
{
    #region Step 10: Complex Multi-Schema Queries

    /// <summary>
    ///     Tests multiple CROSS APPLY chains on same binary data.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_MultipleCrossApplyChains_ShouldWork()
    {
        var query = @"
            binary Header {
                Magic: int le,
                Count: short le
            };
            binary Record {
                Id: int le,
                Value: double le
            };
            select h.Magic, h.Count, r.Id, r.Value
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h
            cross apply InterpretAt<Record>(f.Content, 6) r";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0xBEEF); // Magic
        bw.Write((short)1); // Count
        bw.Write(42); // Record.Id at offset 6
        bw.Write(3.14d); // Record.Value

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xBEEF, table[0][0]);
        Assert.AreEqual((short)1, table[0][1]);
        Assert.AreEqual(42, table[0][2]);
        Assert.AreEqual(3.14d, table[0][3]);
    }

    /// <summary>
    ///     Tests WHERE and GROUP BY on interpreted fields across multiple files.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_GroupByOnInterpretedField_ShouldAggregate()
    {
        var query = @"
            binary Data {
                Category: byte,
                Value: int le
            };
            select s.Category, Count(s.Category), Sum(s.Value)
            from #test.files() f
            cross apply Interpret<Data>(f.Content) s
            group by s.Category
            having Count(s.Category) > 1";

        using var ms1 = new MemoryStream();
        ms1.WriteByte(1);
        ms1.Write(BitConverter.GetBytes(10));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(2);
        ms2.Write(BitConverter.GetBytes(20));
        using var ms3 = new MemoryStream();
        ms3.WriteByte(1);
        ms3.Write(BitConverter.GetBytes(30));
        using var ms4 = new MemoryStream();
        ms4.WriteByte(1);
        ms4.Write(BitConverter.GetBytes(40));

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "f2.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "f3.bin", Content = ms3.ToArray() },
            new BinaryEntity { Name = "f4.bin", Content = ms4.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Only category 1 has count > 1 (3 entries)
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(3L, table[0][1]);
        Assert.AreEqual(80, table[0][2]); // 10+30+40
    }

    /// <summary>
    ///     Tests ORDER BY on interpreted fields.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_OrderByOnInterpretedField_ShouldSort()
    {
        var query = @"
            binary Data {
                Priority: byte,
                Name: string[10] ascii trim
            };
            select s.Priority, s.Name
            from #test.files() f
            cross apply Interpret<Data>(f.Content) s
            order by s.Priority desc";

        var makeData = (byte priority, string name) =>
        {
            var ms = new MemoryStream();
            ms.WriteByte(priority);
            ms.Write(Encoding.ASCII.GetBytes(name.PadRight(10)));
            return ms.ToArray();
        };

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = makeData(3, "Low") },
            new BinaryEntity { Name = "f2.bin", Content = makeData(1, "Critical") },
            new BinaryEntity { Name = "f3.bin", Content = makeData(2, "Medium") }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("Low", table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual("Medium", table[1][1]);
        Assert.AreEqual((byte)1, table[2][0]);
        Assert.AreEqual("Critical", table[2][1]);
    }

    /// <summary>
    ///     Tests CTE with interpretation functions.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_CteWithInterpret_ShouldWork()
    {
        var query = @"
            binary Data {
                Tag: byte,
                Value: int le
            };
            with ParsedData as (
                select s.Tag as Tag, s.Value as Value
                from #test.files() f
                cross apply Interpret<Data>(f.Content) s
            )
            select p.Tag, p.Value from ParsedData p
            where p.Tag > 0
            order by p.Value asc";

        using var ms1 = new MemoryStream();
        ms1.WriteByte(0);
        ms1.Write(BitConverter.GetBytes(999));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(1);
        ms2.Write(BitConverter.GetBytes(300));
        using var ms3 = new MemoryStream();
        ms3.WriteByte(2);
        ms3.Write(BitConverter.GetBytes(100));

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "f2.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "f3.bin", Content = ms3.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Tag=0 is filtered out; Tag=2 (Value=100) comes first, then Tag=1 (Value=300)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual(300, table[1][1]);
    }

    /// <summary>
    ///     Tests text interpretation with GROUP BY and multiple rows.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_TextGroupByMultipleRows_ShouldAggregate()
    {
        var query = @"
            text LogEntry {
                Level: until ':',
                Message: rest trim
            };
            select l.Level, Count(l.Level)
            from #test.lines() t
            cross apply Parse<LogEntry>(t.Line) l
            group by l.Level
            order by Count(l.Level) desc";

        var entities = new[]
        {
            new TextEntity { Name = "l1", Text = "ERROR: something bad" },
            new TextEntity { Name = "l2", Text = "WARN: low disk" },
            new TextEntity { Name = "l3", Text = "ERROR: timeout" },
            new TextEntity { Name = "l4", Text = "ERROR: crash" },
            new TextEntity { Name = "l5", Text = "INFO: started" },
            new TextEntity { Name = "l6", Text = "WARN: slow query" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual(3L, table[0][1]);
        Assert.AreEqual("WARN", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual("INFO", table[2][0]);
        Assert.AreEqual(1L, table[2][1]);
    }

    /// <summary>
    ///     Tests multiple files with schema arrays and aggregation on array elements.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_SchemaArrayWithCrossApplyAndAggregation_ShouldWork()
    {
        var query = @"
            binary Item { Id: int le, Score: int le };
            binary Container { Count: byte, Items: Item[Count] };
            select Sum(i.Score), Min(i.Score), Max(i.Score)
            from #test.files() f
            cross apply Interpret<Container>(f.Content) c
            cross apply c.Items i";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)4); // Count
        bw.Write(1);
        bw.Write(10); // Item 1
        bw.Write(2);
        bw.Write(50); // Item 2
        bw.Write(3);
        bw.Write(20); // Item 3
        bw.Write(4);
        bw.Write(40); // Item 4

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(120, table[0][0]); // 10+50+20+40
        Assert.AreEqual(10, table[0][1]); // Min
        Assert.AreEqual(50, table[0][2]); // Max
    }

    /// <summary>
    ///     Tests combining text and binary parsing in same query via MixedSchemaProvider.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_MixedBinaryAndTextSources_ShouldQueryBoth()
    {
        var query = @"
            binary BinData {
                Value: int le
            };
            text TextData {
                Key: until '=',
                Val: rest
            };
            select b.Value
            from #bin.files() f
            cross apply Interpret<BinData>(f.Content) b
            where b.Value > 50";

        var binaryEntities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "f2.bin", Content = BitConverter.GetBytes(25) },
            new BinaryEntity { Name = "f3.bin", Content = BitConverter.GetBytes(75) }
        };

        var textEntities = new[]
        {
            new TextEntity { Name = "l1", Text = "host=localhost" }
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#bin", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#txt", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count); // 100 and 75
    }

    /// <summary>
    ///     Tests absolute positioning combined with schema arrays.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_AbsolutePositionWithArrays_ShouldParseCorrectly()
    {
        var query = @"
            binary Header {
                Magic: int le at 0,
                RecordCount: short le at 4,
                DataStart: int le at 6
            };
            select h.Magic, h.RecordCount, h.DataStart
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0x1234ABCD); // Magic at 0
        bw.Write((short)10); // RecordCount at 4
        bw.Write(100); // DataStart at 6

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x1234ABCD, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
    }

    /// <summary>
    ///     Tests parsing the same binary data with two different schemas
    ///     (two CROSS APPLY Interpret calls on same input).
    /// </summary>
    [TestMethod]
    public void Stress_Complex_TwoSchemasOnSameData_ShouldParseBoth()
    {
        var query = @"
            binary AsInts { A: int le, B: int le };
            binary AsBytes { X: byte[8] };
            select i.A, i.B, b.X
            from #test.files() f
            cross apply Interpret<AsInts>(f.Content) i
            cross apply Interpret<AsBytes>(f.Content) b";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42);
        bw.Write(99);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual(99, table[0][1]);
        var rawBytes = (byte[])table[0][2];
        Assert.HasCount(8, rawBytes);
    }

    /// <summary>
    ///     Tests computed field with complex expression involving multiple fields and operators.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_ComputedFieldComplexExpression_ShouldEvaluate()
    {
        var query = @"
            binary Data {
                Width: short le,
                Height: short le,
                Depth: byte,
                Volume: Width * Height * Depth,
                IsLarge: Width * Height > 10000
            };
            select s.Width, s.Height, s.Depth, s.Volume, s.IsLarge
            from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((short)10); // Width
        bw.Write((short)20); // Height
        bw.Write((byte)3); // Depth

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)10, table[0][0]);
        Assert.AreEqual((short)20, table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
        // Volume = 10 * 20 * 3 = 600
        Assert.AreEqual((short)600, table[0][3]);
        // IsLarge = 10 * 20 > 10000 = false (200 > 10000 is false)
        Assert.IsFalse((bool?)table[0][4]);
    }

    /// <summary>
    ///     Tests combining discard fields with check constraints and nested schemas.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_DiscardCheckNested_ShouldParseCorrectly()
    {
        var query = @"
            binary Inner { X: int le, Y: int le };
            binary Outer {
                Magic: int le check Magic = 42,
                _: byte[4],
                Data: Inner,
                _: short le,
                Footer: byte
            };
            select o.Magic, o.Data.X, o.Data.Y, o.Footer
            from #test.files() f
            cross apply Interpret<Outer>(f.Content) o";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42); // Magic (check Magic = 42)
        bw.Write("\u0000\u0000\u0000\u0000"u8.ToArray()); // discard byte[4]
        bw.Write(10); // Inner.X
        bw.Write(20); // Inner.Y
        bw.Write((short)0); // discard short
        bw.Write((byte)0xFF); // Footer

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, table[0][2]);
        Assert.AreEqual((byte)0xFF, table[0][3]);
    }

    /// <summary>
    ///     Tests a realistic file format scenario: header + variable records.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_RealisticFileFormat_ShouldParseEntirely()
    {
        var query = @"
            binary Record {
                Id: int le,
                NameLen: byte,
                Name: string[NameLen] utf8,
                Score: short le
            };
            binary FileFormat {
                Magic: int le check Magic = 0x46494C45,
                Version: byte,
                RecordCount: short le,
                Records: Record[RecordCount]
            };
            select r.Id, r.Name, r.Score
            from #test.files() f
            cross apply Interpret<FileFormat>(f.Content) ff
            cross apply ff.Records r
            where r.Score >= 80
            order by r.Score desc";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Header
        bw.Write(0x46494C45); // Magic = "FILE"
        bw.Write((byte)1); // Version
        bw.Write((short)4); // RecordCount

        // Record 1: Id=1, Name="Alice", Score=95
        bw.Write(1);
        bw.Write((byte)5);
        bw.Write("Alice"u8.ToArray());
        bw.Write((short)95);

        // Record 2: Id=2, Name="Bob", Score=72
        bw.Write(2);
        bw.Write((byte)3);
        bw.Write("Bob"u8.ToArray());
        bw.Write((short)72);

        // Record 3: Id=3, Name="Charlie", Score=88
        bw.Write(3);
        bw.Write((byte)7);
        bw.Write("Charlie"u8.ToArray());
        bw.Write((short)88);

        // Record 4: Id=4, Name="Dana", Score=91
        bw.Write(4);
        bw.Write((byte)4);
        bw.Write("Dana"u8.ToArray());
        bw.Write((short)91);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "data.dat", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Scores >= 80: Alice(95), Charlie(88), Dana(91) - ordered desc: 95, 91, 88
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0][0]); // Alice
        Assert.AreEqual("Alice", table[0][1]);
        Assert.AreEqual((short)95, table[0][2]);
        Assert.AreEqual(4, table[1][0]); // Dana
        Assert.AreEqual("Dana", table[1][1]);
        Assert.AreEqual((short)91, table[1][2]);
        Assert.AreEqual(3, table[2][0]); // Charlie
        Assert.AreEqual("Charlie", table[2][1]);
        Assert.AreEqual((short)88, table[2][2]);
    }

    #endregion
}
