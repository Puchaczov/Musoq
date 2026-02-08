using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for SQL aggregation functions combined with schema interpretation.
///     Tests COUNT, SUM, MIN, MAX, AVG, HAVING, and multiple aggregates on interpreted data.
/// </summary>
[TestClass]
public class SchemaAggregationFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region COUNT with GROUP BY

    /// <summary>
    ///     Tests counting schema array elements per group.
    /// </summary>
    [TestMethod]
    public void Binary_CountWithGroupBy_ShouldCountPerGroup()
    {
        var query = @"
            binary Record { Category: byte, Value: short le };
            select d.Category, Count(d.Category) as Cnt from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            group by d.Category
            order by d.Category asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x0A, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x02, 0x14, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x01, 0x1E, 0x00] },
            new BinaryEntity { Name = "4.bin", Data = [0x01, 0x28, 0x00] },
            new BinaryEntity { Name = "5.bin", Data = [0x02, 0x32, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(2, table[1][1]);
    }

    #endregion

    #region MIN and MAX with GROUP BY

    /// <summary>
    ///     Tests MIN and MAX aggregate functions on interpreted schema fields.
    /// </summary>
    [TestMethod]
    public void Binary_MinMaxWithGroupBy_ShouldFindExtremes()
    {
        var query = @"
            binary Measurement { Sensor: byte, Reading: short le };
            select m.Sensor, Min(m.Reading) as MinVal, Max(m.Reading) as MaxVal 
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Measurement') m
            group by m.Sensor
            order by m.Sensor asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x0A, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x01, 0x1E, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x01, 0x05, 0x00] },
            new BinaryEntity { Name = "4.bin", Data = [0x02, 0x64, 0x00] },
            new BinaryEntity { Name = "5.bin", Data = [0x02, 0x32, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(5m, table[0][1]);
        Assert.AreEqual(30m, table[0][2]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(50m, table[1][1]);
        Assert.AreEqual(100m, table[1][2]);
    }

    #endregion

    #region AVG with GROUP BY

    /// <summary>
    ///     Tests AVG aggregate function on interpreted data grouped by category.
    /// </summary>
    [TestMethod]
    public void Binary_AvgWithGroupBy_ShouldCalculateAverage()
    {
        var query = @"
            binary Sample { Group: byte, Score: int le };
            select s.Group, Avg(s.Score) as AvgScore 
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Sample') s
            group by s.Group
            order by s.Group asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x0A, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x01, 0x14, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x02, 0x64, 0x00, 0x00, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(15m, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(100m, table[1][1]);
    }

    #endregion

    #region Multiple Aggregates in One Query

    /// <summary>
    ///     Tests combining COUNT, SUM, MIN, MAX in a single query.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleAggregates_ShouldComputeAll()
    {
        var query = @"
            binary Item { Value: int le };
            select Count(d.Value) as Cnt, Sum(d.Value) as Total, Min(d.Value) as MinV, Max(d.Value) as MaxV
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Item') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x0A, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x14, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x05, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "4.bin", Data = [0x1E, 0x00, 0x00, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4, table[0][0]);
        Assert.AreEqual(65m, table[0][1]);
        Assert.AreEqual(5m, table[0][2]);
        Assert.AreEqual(30m, table[0][3]);
    }

    #endregion

    #region HAVING Clause

    /// <summary>
    ///     Tests HAVING clause to filter groups by aggregate result.
    /// </summary>
    [TestMethod]
    public void Binary_Having_ShouldFilterGroups()
    {
        var query = @"
            binary Record { Category: byte, Amount: int le };
            select d.Category, Sum(d.Amount) as Total
            from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d
            group by d.Category
            having Sum(d.Amount) > 20
            order by d.Category asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x05, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x02, 0x14, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "3.bin", Data = [0x01, 0x03, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "4.bin", Data = [0x02, 0x0A, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "5.bin", Data = [0x03, 0x02, 0x00, 0x00, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(30m, table[0][1]);
    }

    #endregion

    #region Text Schema GROUP BY with COUNT

    /// <summary>
    ///     Tests grouping and counting parsed text lines by a field.
    /// </summary>
    [TestMethod]
    public void Text_GroupByWithCount_ShouldCountPerCategory()
    {
        var query = @"
            text LogLine { Level: until ' ', Message: rest };
            select l2.Level, Count(l2.Level) as Cnt
            from #test.lines() l
            cross apply Parse(l.Line, 'LogLine') l2
            group by l2.Level
            order by l2.Level asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "ERROR disk full" },
            new TextEntity { Name = "2.txt", Text = "WARN low memory" },
            new TextEntity { Name = "3.txt", Text = "ERROR timeout" },
            new TextEntity { Name = "4.txt", Text = "INFO started" },
            new TextEntity { Name = "5.txt", Text = "ERROR crash" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual("INFO", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual("WARN", table[2][0]);
        Assert.AreEqual(1, table[2][1]);
    }

    #endregion

    #region Schema Array Element Aggregation

    /// <summary>
    ///     Tests aggregating over cross-applied schema array elements.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayAggregation_ShouldAggregateAcrossElements()
    {
        var query = @"
            binary Item { Score: byte };
            binary Container { Count: byte, Items: Item[Count] };
            select Sum(i.Score) as TotalScore, Count(i.Score) as NumItems
            from #test.files() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply c.Items i";

        var testData = new byte[]
        {
            0x04,
            0x0A,
            0x14,
            0x1E,
            0x28
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(4, table[0][1]);
    }

    #endregion

    #region GROUP BY on Schema Array Elements

    /// <summary>
    ///     Tests grouping schema array elements by a field and aggregating.
    /// </summary>
    [TestMethod]
    public void Binary_GroupBySchemaArrayElements_ShouldGroupAndAggregate()
    {
        var query = @"
            binary Entry { Type: byte, Value: short le };
            binary DataBlock { Count: byte, Entries: Entry[Count] };
            select e.Type, Sum(e.Value) as Total, Count(e.Type) as Cnt
            from #test.files() b
            cross apply Interpret(b.Content, 'DataBlock') d
            cross apply d.Entries e
            group by e.Type
            order by e.Type asc";

        var testData = new byte[]
        {
            0x05,
            0x01, 0x0A, 0x00,
            0x02, 0x14, 0x00,
            0x01, 0x1E, 0x00,
            0x02, 0x28, 0x00,
            0x01, 0x32, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(90m, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(60m, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
    }

    #endregion
}
