using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for SQL query features combined with schema interpretation.
///     Tests DISTINCT, SKIP/TAKE, CASE WHEN, compound WHERE, and string functions.
/// </summary>
[TestClass]
public class SchemaQueryFeaturesFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region DISTINCT on Schema Fields

    /// <summary>
    ///     Tests DISTINCT to remove duplicate values from interpreted data.
    /// </summary>
    [TestMethod]
    public void Binary_Distinct_ShouldRemoveDuplicates()
    {
        var query = @"
            binary Record { Category: byte };
            select distinct d.Category from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            order by d.Category asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01] },
            new BinaryEntity { Name = "2.bin", Data = [0x02] },
            new BinaryEntity { Name = "3.bin", Data = [0x01] },
            new BinaryEntity { Name = "4.bin", Data = [0x03] },
            new BinaryEntity { Name = "5.bin", Data = [0x02] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)3, table[2][0]);
    }

    #endregion

    #region SKIP and TAKE

    /// <summary>
    ///     Tests SKIP and TAKE to paginate interpreted data.
    /// </summary>
    [TestMethod]
    public void Binary_SkipTake_ShouldPaginate()
    {
        var query = @"
            binary Record { Value: byte };
            select d.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            order by d.Value asc
            skip 2
            take 2";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x05] },
            new BinaryEntity { Name = "2.bin", Data = [0x01] },
            new BinaryEntity { Name = "3.bin", Data = [0x04] },
            new BinaryEntity { Name = "4.bin", Data = [0x02] },
            new BinaryEntity { Name = "5.bin", Data = [0x03] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual((byte)4, table[1][0]);
    }

    #endregion

    #region TAKE Only

    /// <summary>
    ///     Tests TAKE without SKIP to limit result count.
    /// </summary>
    [TestMethod]
    public void Binary_TakeOnly_ShouldLimitResults()
    {
        var query = @"
            binary Record { Id: byte };
            select d.Id from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            order by d.Id asc
            take 3";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x05] },
            new BinaryEntity { Name = "2.bin", Data = [0x01] },
            new BinaryEntity { Name = "3.bin", Data = [0x04] },
            new BinaryEntity { Name = "4.bin", Data = [0x02] },
            new BinaryEntity { Name = "5.bin", Data = [0x03] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)3, table[2][0]);
    }

    #endregion

    #region CASE WHEN on Schema Fields

    /// <summary>
    ///     Tests CASE WHEN expression on interpreted binary schema fields.
    /// </summary>
    [TestMethod]
    public void Binary_CaseWhen_ShouldClassifyValues()
    {
        var query = @"
            binary Record { Score: byte };
            select d.Score, 
                   case when d.Score >= 90 then 'A' 
                        when d.Score >= 80 then 'B' 
                        when d.Score >= 70 then 'C' 
                        else 'F' end as Grade
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            order by d.Score desc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x5F] },
            new BinaryEntity { Name = "2.bin", Data = [0x50] },
            new BinaryEntity { Name = "3.bin", Data = [0x46] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)0x5F, table[0][0]);
        Assert.AreEqual("A", table[0][1]);
        Assert.AreEqual((byte)0x50, table[1][0]);
        Assert.AreEqual("B", table[1][1]);
        Assert.AreEqual((byte)0x46, table[2][0]);
        Assert.AreEqual("C", table[2][1]);
    }

    #endregion

    #region CASE WHEN on Text Parsed Fields

    /// <summary>
    ///     Tests CASE WHEN for classification on parsed text data.
    /// </summary>
    [TestMethod]
    public void Text_CaseWhen_ShouldClassifyParsedValues()
    {
        var query = @"
            text Entry { Level: until ' ', Message: rest };
            select e.Level,
                   case when e.Level = 'ERROR' then 'Critical' 
                        when e.Level = 'WARN' then 'Warning' 
                        else 'Info' end as Severity
            from #test.lines() l
            cross apply Parse(l.Line, 'Entry') e
            order by e.Level asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "ERROR disk failure" },
            new TextEntity { Name = "2.txt", Text = "WARN low space" },
            new TextEntity { Name = "3.txt", Text = "INFO started" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual("Critical", table[0][1]);
        Assert.AreEqual("INFO", table[1][0]);
        Assert.AreEqual("Info", table[1][1]);
        Assert.AreEqual("WARN", table[2][0]);
        Assert.AreEqual("Warning", table[2][1]);
    }

    #endregion

    #region Compound WHERE with AND/OR

    /// <summary>
    ///     Tests multiple WHERE conditions combined with AND on interpreted data.
    /// </summary>
    [TestMethod]
    public void Binary_CompoundWhereAnd_ShouldFilterBothConditions()
    {
        var query = @"
            binary Record { Type: byte, Value: short le };
            select d.Type, d.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            where d.Type = 1 and d.Value > 10
            order by d.Value asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x05, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x02, 0x14, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x01, 0x1E, 0x00] },
            new BinaryEntity { Name = "4.bin", Data = [0x01, 0x0F, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((short)15, table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual((short)30, table[1][1]);
    }

    /// <summary>
    ///     Tests WHERE with OR to match multiple conditions.
    /// </summary>
    [TestMethod]
    public void Binary_CompoundWhereOr_ShouldMatchEitherCondition()
    {
        var query = @"
            binary Record { Type: byte, Value: short le };
            select d.Type, d.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            where d.Type = 1 or d.Value = 100
            order by d.Value asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x0A, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x02, 0x64, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x03, 0x32, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((short)100, table[1][1]);
    }

    #endregion

    #region Arithmetic in SELECT on Schema Fields

    /// <summary>
    ///     Tests arithmetic expressions in SELECT using interpreted field values.
    /// </summary>
    [TestMethod]
    public void Binary_ArithmeticInSelect_ShouldComputeExpressions()
    {
        var query = @"
            binary Dimensions { Width: short le, Height: short le };
            select d.Width, d.Height, d.Width * d.Height as Area, (d.Width + d.Height) * 2 as Perimeter
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Dimensions') d
            order by d.Width asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x03, 0x00, 0x04, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x05, 0x00, 0x06, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((short)3, table[0][0]);
        Assert.AreEqual((short)4, table[0][1]);
        Assert.AreEqual((short)(3 * 4), table[0][2]);
        Assert.AreEqual((3 + 4) * 2, table[0][3]);
        Assert.AreEqual((short)5, table[1][0]);
        Assert.AreEqual((short)6, table[1][1]);
        Assert.AreEqual((short)(5 * 6), table[1][2]);
        Assert.AreEqual((5 + 6) * 2, table[1][3]);
    }

    #endregion

    #region String Functions on Parsed Text

    /// <summary>
    ///     Tests using string functions (ToUpperInvariant, Length) on parsed text fields.
    /// </summary>
    [TestMethod]
    public void Text_StringFunctions_ShouldTransformParsedFields()
    {
        var query = @"
            text Data { Key: until '=', Value: rest };
            select ToUpperInvariant(d.Key) as UpperKey, Length(d.Value) as ValueLen
            from #test.lines() l
            cross apply Parse(l.Line, 'Data') d
            order by d.Key asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "name=Alice" },
            new TextEntity { Name = "2.txt", Text = "city=Portland" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("CITY", table[0][0]);
        Assert.AreEqual(8, table[0][1]);
        Assert.AreEqual("NAME", table[1][0]);
        Assert.AreEqual(5, table[1][1]);
    }

    #endregion

    #region DISTINCT on Text Schema

    /// <summary>
    ///     Tests DISTINCT on parsed text fields to remove duplicates.
    /// </summary>
    [TestMethod]
    public void Text_Distinct_ShouldRemoveDuplicateParsedValues()
    {
        var query = @"
            text LogLine { Level: until ' ', Message: rest };
            select distinct l2.Level from #test.lines() l
            cross apply Parse(l.Line, 'LogLine') l2
            order by l2.Level asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "ERROR disk full" },
            new TextEntity { Name = "2.txt", Text = "WARN low memory" },
            new TextEntity { Name = "3.txt", Text = "ERROR timeout" },
            new TextEntity { Name = "4.txt", Text = "INFO started" },
            new TextEntity { Name = "5.txt", Text = "ERROR crash" },
            new TextEntity { Name = "6.txt", Text = "WARN cpu hot" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual("INFO", table[1][0]);
        Assert.AreEqual("WARN", table[2][0]);
    }

    #endregion
}
