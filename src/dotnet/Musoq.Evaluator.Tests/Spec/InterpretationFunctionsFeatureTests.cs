using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Interpretation Functions (Section 7 of specification).
///     Tests Interpret, Parse, TryInterpret, TryParse, and InterpretAt functions.
/// </summary>
[TestClass]
public class InterpretationFunctionsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 7: Combined Interpretation Functions

    /// <summary>
    ///     Tests using both Interpret and Parse in same query.
    /// </summary>
    [TestMethod]
    public void InterpretAndParse_Combined_ShouldWork()
    {
        var query = @"
            binary BinaryData { 
                Value: int le
            };
            text TextData { 
                Name: until ':'
            };
            select b.Name, d.Value, t.Name as TextName 
            from #btest.bytes() b
            cross apply Interpret(b.Content, 'BinaryData') d
            cross apply #ttest.lines() l
            cross apply Parse(l.Line, 'TextData') t";

        var binaryData = new byte[] { 0x0A, 0x00, 0x00, 0x00 };
        var binaryEntities = new[] { new BinaryEntity { Name = "test.bin", Data = binaryData } };
        var textEntities = new[] { new TextEntity { Name = "test.txt", Text = "Label:data" } };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#btest", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#ttest", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test.bin", table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual("Label", table[0][2]);
    }

    #endregion

    #region Section 7: Interpretation with Aggregation

    /// <summary>
    ///     Tests interpretation results with GROUP BY.
    /// </summary>
    [TestMethod]
    public void Interpret_WithGroupBy_ShouldAggregate()
    {
        var query = @"
            binary Data { 
                Category: byte,
                Value: int le
            };
            select d.Category, Sum(d.Value) as Total from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            group by d.Category
            order by d.Category";

        var data1 = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0x02, 0x14, 0x00, 0x00, 0x00 };
        var data3 = new byte[] { 0x01, 0x1E, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = data1 },
            new BinaryEntity { Name = "2.bin", Data = data2 },
            new BinaryEntity { Name = "3.bin", Data = data3 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(40m, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(20m, table[1][1]);
    }

    #endregion

    #region Section 7.1: Interpret Function

    /// <summary>
    ///     Tests basic Interpret function for binary data.
    /// </summary>
    [TestMethod]
    public void Interpret_BasicBinary_ShouldParse()
    {
        var query = @"
            binary Header { 
                Magic: int le,
                Version: byte
            };
            select h.Magic, h.Version from #test.bytes() b
            cross apply Interpret(b.Content, 'Header') h";

        var data = new byte[]
        {
            0x41, 0x42, 0x43, 0x44,
            0x01
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x44434241, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
    }

    /// <summary>
    ///     Tests Interpret with multiple schema definitions.
    /// </summary>
    [TestMethod]
    public void Interpret_MultipleSchemas_ShouldSelectCorrect()
    {
        var query = @"
            binary First { 
                Value: int le
            };
            binary Second { 
                A: short le,
                B: short le
            };
            select f.Value, s.A, s.B from #test.bytes() b
            cross apply Interpret(b.Content, 'First') f
            cross apply Interpret(b.Content, 'Second') s";

        var data = new byte[] { 0x01, 0x00, 0x02, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x00020001, table[0][0]);
        Assert.AreEqual((short)1, table[0][1]);
        Assert.AreEqual((short)2, table[0][2]);
    }

    #endregion

    #region Section 7.2: Parse Function

    /// <summary>
    ///     Tests basic Parse function for text data.
    /// </summary>
    [TestMethod]
    public void Parse_BasicText_ShouldParse()
    {
        var query = @"
            text Record { 
                Name: until ',',
                Value: rest trim
            };
            select r.Name, r.Value from #test.lines() l
            cross apply Parse(l.Line, 'Record') r";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "Key,Value123" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Key", table[0][0]);
        Assert.AreEqual("Value123", table[0][1]);
    }

    /// <summary>
    ///     Tests Parse with multiple text schemas.
    /// </summary>
    [TestMethod]
    public void Parse_MultipleTextSchemas_ShouldWork()
    {
        var query = @"
            text CsvRow { 
                Field1: until ',',
                Field2: until ',',
                Field3: rest
            };
            text KeyValue { 
                Key: until ':',
                Value: rest
            };
            select c.Field1, c.Field2, c.Field3 from #test.lines() l
            cross apply Parse(l.Line, 'CsvRow') c";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "A,B,C" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual("B", table[0][1]);
        Assert.AreEqual("C", table[0][2]);
    }

    #endregion

    #region Section 7.3: TryInterpret Function

    /// <summary>
    ///     Tests TryInterpret with valid data.
    /// </summary>
    [TestMethod]
    public void TryInterpret_ValidData_ShouldSucceed()
    {
        var query = @"
            binary Data { 
                Value: int le
            };
            select d.Value from #test.bytes() b
            outer apply TryInterpret(b.Content, 'Data') d
            where d.Value is not null";

        var data = new byte[] { 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
    }

    /// <summary>
    ///     Tests TryInterpret with insufficient data.
    /// </summary>
    [TestMethod]
    public void TryInterpret_InsufficientData_ShouldReturnNull()
    {
        var query = @"
            binary Data { 
                Value: long le
            };
            select d.Value from #test.bytes() b
            outer apply TryInterpret(b.Content, 'Data') d";

        var data = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region Section 7.4: TryParse Function

    /// <summary>
    ///     Tests TryParse with valid text.
    /// </summary>
    [TestMethod]
    public void TryParse_ValidText_ShouldSucceed()
    {
        var query = @"
            text Record { 
                Name: until ':'
            };
            select r.Name from #test.lines() l
            outer apply TryParse(l.Line, 'Record') r
            where r.Name is not null";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "Valid:data" },
            new TextEntity { Name = "line2", Text = "Also:valid" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    ///     Tests TryParse with missing delimiter.
    /// </summary>
    [TestMethod]
    public void TryParse_MissingDelimiter_ShouldReturnNull()
    {
        var query = @"
            text Record { 
                Name: until ':'
            };
            select l.Line, r.Name from #test.lines() l
            outer apply TryParse(l.Line, 'Record') r";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "Has:colon" },
            new TextEntity { Name = "line2", Text = "NoColonHere" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var lines = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        Assert.IsTrue(lines.Contains("Has:colon"));
        Assert.IsTrue(lines.Contains("NoColonHere"));

        var foundHasRow = false;
        for (var i = 0; i < table.Count; i++)
            if (table[i][1] != null && table[i][1].ToString() == "Has")
            {
                Assert.AreEqual("Has:colon", table[i][0]);
                foundHasRow = true;
                break;
            }

        Assert.IsTrue(foundHasRow);
    }

    #endregion

    #region Section 7.5: InterpretAt Function

    /// <summary>
    ///     Tests InterpretAt with specific offset.
    /// </summary>
    [TestMethod]
    public void InterpretAt_WithOffset_ShouldStartAtPosition()
    {
        var query = @"
            binary Data { 
                Value: int le
            };
            select d.Value from #test.bytes() b
            cross apply InterpretAt(b.Content, 4, 'Data') d";

        var data = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0x0A, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
    }

    /// <summary>
    ///     Tests InterpretAt with zero offset (same as Interpret).
    /// </summary>
    [TestMethod]
    public void InterpretAt_ZeroOffset_ShouldStartAtBeginning()
    {
        var query = @"
            binary Data { 
                Value: int le
            };
            select d.Value from #test.bytes() b
            cross apply InterpretAt(b.Content, 0, 'Data') d";

        var data = new byte[] { 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
    }

    /// <summary>
    ///     Tests InterpretAt with dynamic offset from field.
    /// </summary>
    [TestMethod]
    public void InterpretAt_DynamicOffset_ShouldUseFieldValue()
    {
        var query = @"
            binary Header { 
                DataOffset: int le
            };
            binary Payload { 
                Value: short le
            };
            select h.DataOffset, p.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Header') h
            cross apply InterpretAt(b.Content, h.DataOffset, 'Payload') p";

        var data = new byte[]
        {
            0x08, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x2A, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table[0][0]);
        Assert.AreEqual((short)42, table[0][1]);
    }

    #endregion
}
