using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests to verify that parallelism doesn't lose output rows.
///     Tests with large input datasets should produce correct amount of output rows.
///     Tests verify both the count of rows and the correctness of row values.
/// </summary>
[TestClass]
public partial class ParallelismOutputRowsTests
{
    static ParallelismOutputRowsTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    private static ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    #region Simple SELECT Tests

    [TestMethod]
    public void SimpleSelect_WithParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 5000;
        const string query = "select Name, Id from #A.Entities()";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");


        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var expectedIds = Enumerable.Range(0, rowCount).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void SimpleSelect_WithoutParallelization_ShouldReturnAllRows()
    {
        const int rowCount = 5000;
        const string query = "select Name, Id from #A.Entities()";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();

        Assert.AreEqual(rowCount, table.Count, $"Expected {rowCount} rows but got {table.Count}");


        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var expectedIds = Enumerable.Range(0, rowCount).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void SimpleSelect_BothModes_ShouldReturnSameResults()
    {
        const int rowCount = 3000;
        const string query = "select Name, Id from #A.Entities()";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };

        var vmParallel =
            CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel,
            new CompilationOptions(ParallelizationMode.None));

        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count,
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds,
            "Result sets differ between parallel and non-parallel execution");
    }

    #endregion

    #region WHERE Clause Tests

    [TestMethod]
    public void WhereClause_WithParallelization_ShouldReturnCorrectFilteredRows()
    {
        const int totalRows = 5000;
        const string query = "select Name, Id from #A.Entities() where Id >= 2500";

        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();

        Assert.AreEqual(2500, table.Count, $"Expected 2500 rows but got {table.Count}");


        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(resultIds.All(id => id >= 2500), "Some rows have Id < 2500");

        var expectedIds = Enumerable.Range(2500, 2500).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void WhereClause_WithoutParallelization_ShouldReturnCorrectFilteredRows()
    {
        const int totalRows = 5000;
        const string query = "select Name, Id from #A.Entities() where Id >= 2500";

        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
        var table = vm.Run();

        Assert.AreEqual(2500, table.Count, $"Expected 2500 rows but got {table.Count}");


        var resultIds = table.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(resultIds.All(id => id >= 2500), "Some rows have Id < 2500");

        var expectedIds = Enumerable.Range(2500, 2500).ToList();
        CollectionAssert.AreEqual(expectedIds, resultIds, "Not all expected IDs were returned");
    }

    [TestMethod]
    public void WhereClause_BothModes_ShouldReturnSameResults()
    {
        const int totalRows = 3000;
        const string query = "select Name, Id from #A.Entities() where Id % 3 = 0";

        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sourcesParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };
        var sourcesNonParallel = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities.ToList() }
        };

        var vmParallel =
            CreateVirtualMachineWithOptions(query, sourcesParallel, new CompilationOptions(ParallelizationMode.Full));
        var vmNonParallel = CreateVirtualMachineWithOptions(query, sourcesNonParallel,
            new CompilationOptions(ParallelizationMode.None));

        var tableParallel = vmParallel.Run();
        var tableNonParallel = vmNonParallel.Run();

        Assert.AreEqual(tableNonParallel.Count, tableParallel.Count,
            $"Row count mismatch: Parallel={tableParallel.Count}, NonParallel={tableNonParallel.Count}");

        var parallelIds = tableParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        var nonParallelIds = tableNonParallel.Select(row => (int)row[1]).OrderBy(x => x).ToList();
        CollectionAssert.AreEqual(nonParallelIds, parallelIds,
            "Result sets differ between parallel and non-parallel execution");
    }

    [TestMethod]
    public void WhereClause_StringFilter_WithParallelization_ShouldReturnCorrectRows()
    {
        const int totalRows = 2000;
        const string query = "select Name, Id from #A.Entities() where Name like '%500%'";

        var entities = CreateBasicEntitiesWithIds(totalRows);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        var table = vm.Run();


        var expectedCount = entities.Count(e => e.Name?.Contains("500") == true);
        Assert.AreEqual(expectedCount, table.Count, $"Expected {expectedCount} rows but got {table.Count}");
    }

    #endregion

    #region Helper Methods

    private static List<BasicEntity> CreateBasicEntitiesWithIds(int count)
    {
        return Enumerable.Range(0, count).Select(i => new BasicEntity
        {
            Id = i,
            Name = $"Entity_{i:D5}",
            City = $"City_{i % 100}",
            Country = $"Country_{i % 10}",
            Population = i
        }).ToList();
    }

    private static List<BasicEntity> CreateGroupedEntities(int rowCount, int cityCount)
    {
        return Enumerable.Range(0, rowCount).Select(i => new BasicEntity
        {
            Id = i,
            Name = $"Entity_{i:D5}",
            City = $"City_{i % cityCount:D2}",
            Country = $"Country_{i % 4:D2}",
            Population = (i % 97) + 1
        }).ToList();
    }

    private static List<BasicEntity> CreateGroupedEntitiesWithNullKeys(int rowCount, int cityCount, int nullEvery)
    {
        var entities = CreateGroupedEntities(rowCount, cityCount);

        for (var i = 0; i < entities.Count; i++)
        {
            if (i % nullEvery == 0)
                entities[i].City = null;
        }

        return entities;
    }

    private static List<BasicEntity> CreateHighCardinalityGroupedEntities(int rowCount)
    {
        return Enumerable.Range(0, rowCount).Select(i => new BasicEntity
        {
            Id = i,
            Name = $"Entity_{i:D5}",
            City = $"City_{i:D5}",
            Country = $"Country_{i % 4:D2}",
            Population = (i % 97) + 1
        }).ToList();
    }

    private static void AssertBothModesReturnSameRows(string query, List<BasicEntity> entities)
    {
        var parallelTable = RunQuery(query, entities.ToList(), ParallelizationMode.Full);
        var serialTable = RunQuery(query, entities.ToList(), ParallelizationMode.None);

        Assert.AreEqual(serialTable.Count, parallelTable.Count,
            $"Row count mismatch: Parallel={parallelTable.Count}, Serial={serialTable.Count}");

        var parallelRows = NormalizeRows(parallelTable);
        var serialRows = NormalizeRows(serialTable);

        CollectionAssert.AreEqual(serialRows, parallelRows,
            "Result sets differ between parallel and serial execution");
    }

    private static Table RunQuery(string query, List<BasicEntity> entities, ParallelizationMode parallelizationMode)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };

        var vm = CreateVirtualMachineWithOptions(query, sources, new CompilationOptions(parallelizationMode));

        return TableMaterializationTestHelper.Materialize(vm.Run());
    }

    private static List<string> NormalizeRows(Table table)
    {
        return table
            .Select(row => string.Join("|", Enumerable.Range(0, row.Count).Select(column => FormatCell(row[column]))))
            .OrderBy(row => row, StringComparer.Ordinal)
            .ToList();
    }

    private static string FormatCell(object? value)
    {
        return value switch
        {
            null => "<null>",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static CompiledQuery CreateVirtualMachineWithOptions(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            options);
    }

    private static CompiledQuery CreateJoinVirtualMachine(
        string script,
        FirstEntity[] first,
        SecondEntity[] second,
        CompilationOptions options)
    {
        var schema = new MultiSchema(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            {
                "first",
                (new FirstEntityTable(), new MultiRowSource<FirstEntity>(first))
            },
            {
                "second",
                (new SecondEntityTable(), new MultiRowSource<SecondEntity>(second))
            }
        });

        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new MultiSchemaProvider(new Dictionary<string, ISchema>
            {
                { "#schema", schema }
            }),
            LoggerResolver,
            options);
    }

    #endregion
}
