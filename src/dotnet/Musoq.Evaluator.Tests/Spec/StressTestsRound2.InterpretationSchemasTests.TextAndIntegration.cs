using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsRound2InterpretationSchemasTests
{
    #region Category 5: Text Schema Edge Cases

    /// <summary>
    ///     chars[N] with lower modifier should lowercase the captured text.
    /// </summary>
    [TestMethod]
    public void R2_Text_CharsLower_ShouldLowercase()
    {
        var query = @"
            text Rec {
                Name: chars[5] lower
            };
            select r.Name
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "HELLO" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
    }

    /// <summary>
    ///     chars[N] with upper modifier should uppercase the captured text.
    /// </summary>
    [TestMethod]
    public void R2_Text_CharsUpper_ShouldUppercase()
    {
        var query = @"
            text Rec {
                Name: chars[5] upper
            };
            select r.Name
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "hello" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("HELLO", table[0][0]);
    }

    /// <summary>
    ///     Multiple sequential optional fields, some present, some not.
    /// </summary>
    [TestMethod]
    public void R2_Text_MultipleOptionals_MixedPresence()
    {
        var query = @"
            text Rec {
                Key: until '=',
                Value: until ';',
                _: optional literal ' ',
                Extra: optional pattern '[A-Z]+'
            };
            select r.Key, r.Value, r.Extra
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        // Extra is present
        var entities1 = new[] { new TextEntity { Name = "a.txt", Text = "color=red; BOLD" } };
        var schemaProvider1 = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities1 } });
        var vm1 = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider1, LoggerResolver, TestCompilationOptions);
        var table1 = vm1.Run(CancellationToken.None);

        Assert.AreEqual(1, table1.Count);
        Assert.AreEqual("color", table1[0][0]);
        Assert.AreEqual("red", table1[0][1]);
        Assert.AreEqual("BOLD", table1[0][2]);

        // Extra is absent
        var entities2 = new[] { new TextEntity { Name = "a.txt", Text = "color=red;" } };
        var schemaProvider2 = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities2 } });
        var vm2 = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider2, LoggerResolver, TestCompilationOptions);
        var table2 = vm2.Run(CancellationToken.None);

        Assert.AreEqual(1, table2.Count);
        Assert.AreEqual("color", table2[0][0]);
        Assert.AreEqual("red", table2[0][1]);
        Assert.IsNull(table2[0][2]);
    }

    /// <summary>
    ///     until with multi-character delimiter.
    /// </summary>
    [TestMethod]
    public void R2_Text_UntilMultiCharDelimiter()
    {
        var query = @"
            text Rec {
                First: until '::',
                Second: rest
            };
            select r.First, r.Second
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "hello::world" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
    }

    /// <summary>
    ///     Token at end of input should capture remaining non-whitespace.
    /// </summary>
    [TestMethod]
    public void R2_Text_TokenAtEndOfInput()
    {
        var query = @"
            text Rec {
                First: token,
                _: whitespace,
                Second: token
            };
            select r.First, r.Second
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "alpha beta" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("beta", table[0][1]);
    }

    /// <summary>
    ///     rest on empty remaining input should return empty string.
    /// </summary>
    [TestMethod]
    public void R2_Text_RestEmpty_ShouldReturnEmptyString()
    {
        var query = @"
            text Rec {
                All: chars[5],
                Remaining: rest
            };
            select r.All, r.Remaining
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "ABCDE" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCDE", table[0][0]);
        Assert.AreEqual(string.Empty, table[0][1]);
    }

    /// <summary>
    ///     between with immediately adjacent delimiters should capture empty string.
    /// </summary>
    [TestMethod]
    public void R2_Text_BetweenAdjacentDelimiters_EmptyCapture()
    {
        var query = @"
            text Rec {
                Val: between '[' ']'
            };
            select r.Val
            from #test.files() f
            cross apply Parse<Rec>(f.Text) r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "[]" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(string.Empty, table[0][0]);
    }

    /// <summary>
    ///     Text parse applied to multiple lines via filter.
    /// </summary>
    [TestMethod]
    public void R2_Text_MultipleRowsWithFilter_OrderBy()
    {
        var query = @"
            text KV {
                Key: until '=',
                Value: rest trim
            };
            select r.Key, r.Value
            from #test.files() f
            cross apply Parse<KV>(f.Text) r
            where r.Key <> 'skip'
            order by r.Key asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "alpha=100" },
            new TextEntity { Name = "2.txt", Text = "skip=xxx" },
            new TextEntity { Name = "3.txt", Text = "beta=200" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("100", table[0][1]);
        Assert.AreEqual("beta", table[1][0]);
        Assert.AreEqual("200", table[1][1]);
    }

    #endregion

    #region Category 6: Complex Integration

    /// <summary>
    ///     InterpretAt with computed offset from Interpret result of first schema.
    /// </summary>
    [TestMethod]
    public void R2_Complex_InterpretAtWithComputedOffset()
    {
        var query = @"
            binary Header {
                Magic: int le,
                DataOffset: int le
            };
            binary DataBlock {
                Tag: byte,
                Val: short le
            };
            select h.Magic, d.Tag, d.Val
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h
            cross apply InterpretAt<DataBlock>(f.Content, h.DataOffset) d";

        using var ms = new MemoryStream();
        // Header: Magic=0xBEEF, DataOffset=16
        ms.Write(BitConverter.GetBytes(0xBEEF));
        ms.Write(BitConverter.GetBytes(16));
        // Pad to offset 16
        ms.Write(new byte[8]);
        // DataBlock at offset 16: Tag=42, Val=999
        ms.WriteByte(42);
        ms.Write(BitConverter.GetBytes((short)999));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xBEEF, table[0][0]);
        Assert.AreEqual((byte)42, table[0][1]);
        Assert.AreEqual((short)999, table[0][2]);
    }

    /// <summary>
    ///     Three-level schema reference chain: A → B → C.
    /// </summary>
    [TestMethod]
    public void R2_Complex_ThreeLevelSchemaChain()
    {
        var query = @"
            binary Inner { Val: short le };
            binary Middle { Core: Inner, Extra: byte };
            binary Outer { Wrapper: Middle, Tag: byte };
            select o.Tag, o.Wrapper.Extra, o.Wrapper.Core.Val
            from #test.files() f
            cross apply Interpret<Outer>(f.Content) o";

        using var ms = new MemoryStream();
        // Inner.Val=42, Middle.Extra=7, Outer.Tag=99
        ms.Write(BitConverter.GetBytes((short)42));
        ms.WriteByte(7);
        ms.WriteByte(99);

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)99, table[0][0]);
        Assert.AreEqual((byte)7, table[0][1]);
        Assert.AreEqual((short)42, table[0][2]);
    }

    /// <summary>
    ///     CTE with TryInterpret: filter out invalid files, aggregate valid ones.
    /// </summary>
    [TestMethod]
    public void R2_Complex_CteWithTryInterpret_FilterInvalid()
    {
        var query = @"
            binary Rec {
                Magic: int le check Magic = 48879,
                Val: short le
            };
            with ValidRecs as (
                select r.Val as V
                from #test.files() f
                cross apply TryInterpret<Rec>(f.Content) r
            )
            select Sum(V) as Total, Count(V) as Cnt
            from ValidRecs";

        // File 1: valid (Magic=0xBEEF=48879, Val=10)
        using var ms1 = new MemoryStream();
        ms1.Write(BitConverter.GetBytes(48879));
        ms1.Write(BitConverter.GetBytes((short)10));

        // File 2: invalid (Magic=0xDEAD)
        using var ms2 = new MemoryStream();
        ms2.Write(BitConverter.GetBytes(0xDEAD));
        ms2.Write(BitConverter.GetBytes((short)99));

        // File 3: valid (Magic=0xBEEF=48879, Val=20)
        using var ms3 = new MemoryStream();
        ms3.Write(BitConverter.GetBytes(48879));
        ms3.Write(BitConverter.GetBytes((short)20));

        var entities = new[]
        {
            new BinaryEntity { Name = "ok1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "bad.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "ok2.bin", Content = ms3.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)30, table[0][0]); // 10+20
        Assert.AreEqual(2, Convert.ToInt32(table[0][1]));
    }

    /// <summary>
    ///     Binary and text schemas in same query with correlation using two CTEs.
    /// </summary>
    [TestMethod]
    public void R2_Complex_BinaryAndText_CorrelatedJoin()
    {
        var query = @"
            binary BinRec {
                Id: byte,
                Score: short le
            };
            text TxtRec {
                Id: until ':',
                Label: rest trim
            };
            with BinData as (
                select ToString(b.Id) as BinId, b.Score as Score
                from #bin.files() bf
                cross apply Interpret<BinRec>(bf.Content) b
            ),
            TextData as (
                select r.Id as TxtId, r.Label as Label
                from #txt.files() tf
                cross apply Parse<TxtRec>(tf.Text) r
            )
            select bd.BinId, bd.Score, t.Label
            from BinData bd
            inner join TextData t on bd.BinId = t.TxtId
            order by bd.BinId asc";

        // Binary files: Id=1 Score=100, Id=2 Score=200
        using var ms1 = new MemoryStream();
        ms1.WriteByte(1);
        ms1.Write(BitConverter.GetBytes((short)100));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(2);
        ms2.Write(BitConverter.GetBytes((short)200));

        var binEntities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "b.bin", Content = ms2.ToArray() }
        };

        // Text files: Id=1 Label=Alpha, Id=2 Label=Beta
        var txtEntities = new[]
        {
            new TextEntity { Name = "a.txt", Text = "1:Alpha" },
            new TextEntity { Name = "b.txt", Text = "2:Beta" }
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#bin", binEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#txt", txtEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("1", table[0][0]);
        Assert.AreEqual((short)100, table[0][1]);
        Assert.AreEqual("Alpha", table[0][2]);
        Assert.AreEqual("2", table[1][0]);
        Assert.AreEqual((short)200, table[1][1]);
        Assert.AreEqual("Beta", table[1][2]);
    }

    /// <summary>
    ///     Multiple cross apply chains from same data source with WHERE.
    /// </summary>
    [TestMethod]
    public void R2_Complex_MultipleCrossApplyFromSameSource_WithWhere()
    {
        var query = @"
            binary Header { Magic: int le, Version: short le };
            binary Record { Id: byte, Val: int le };
            select h.Version, r.Id, r.Val
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h
            cross apply InterpretAt<Record>(f.Content, 6) r
            where h.Version > 1 and r.Val > 50";

        using var ms = new MemoryStream();
        // Header: Magic=1234, Version=3
        ms.Write(BitConverter.GetBytes(1234));
        ms.Write(BitConverter.GetBytes((short)3));
        // Record at offset 6: Id=7, Val=99
        ms.WriteByte(7);
        ms.Write(BitConverter.GetBytes(99));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)3, table[0][0]);
        Assert.AreEqual((byte)7, table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     DISTINCT on interpreted field values.
    /// </summary>
    [TestMethod]
    public void R2_Complex_DistinctOnInterpretedFields()
    {
        var query = @"
            binary Rec { Cat: byte };
            select distinct r.Cat
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r
            order by r.Cat asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [3] },
            new BinaryEntity { Name = "b.bin", Content = [1] },
            new BinaryEntity { Name = "c.bin", Content = [3] },
            new BinaryEntity { Name = "d.bin", Content = [2] },
            new BinaryEntity { Name = "e.bin", Content = [1] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)3, table[2][0]);
    }

    #endregion
}
