using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PredicateTransferEvaluationBoundaryTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(SourcePlanningMode.RejectAll)]
    [DataRow(SourcePlanningMode.AcceptPredicate)]
    [DataRow(SourcePlanningMode.AcceptFirstPredicate)]
    public void WherePrefilter_ShouldNotSuppressEarlierJoinOnFailure(SourcePlanningMode mode)
    {
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            {
                ["#left"] = [new() { Id = 1, JoinKey = 1 }],
                ["#right"] = [new() { Id = 10, JoinKey = 1, Name = "bad" }]
            });
        using var query = CreateAndRunVirtualMachine(
            "select l.Id,r.Id from #left.items() l join #right.items() r on l.JoinKey=r.JoinKey AND r.Name::int=1 where l.Id<0",
            schemaProvider: provider);
        Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() => query.Run().Count());
    }

    [TestMethod]
    [DataRow(SourcePlanningMode.RejectAll)]
    [DataRow(SourcePlanningMode.AcceptPredicate)]
    [DataRow(SourcePlanningMode.AcceptFirstPredicate)]
    public void QualifyPredicate_ShouldNotMoveBeforeJoinedWindowEvaluation(SourcePlanningMode mode)
    {
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            {
                ["#left"] = [new() { Id = 1, Score = 0, JoinKey = 1 }, new() { Id = 2, Score = 1, JoinKey = 1 }],
                ["#right"] = [new() { Id = 10, JoinKey = 1 }]
            });
        using var query = CreateAndRunVirtualMachine(
            "select l.Id,ROW_NUMBER() OVER (ORDER BY l.Id) as rn from #left.items() l join #right.items() r on l.JoinKey=r.JoinKey QUALIFY rn>0 AND l.Score>0",
            schemaProvider: provider);
        var row = query.Run().Single();
        Assert.AreEqual(2, row.ReadValue(0));
        Assert.AreEqual(2L, Convert.ToInt64(row.ReadValue(1)));
    }

    [TestMethod]
    public void MembershipMatch_ShouldSkipLaterDynamicRegexPreparation()
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.RejectAll,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            { ["#single"] = [new() { Id = 1, Name = "value" }] });
        using var query = CreateAndRunVirtualMachine(
            "param(pattern: string = '[') select s.Id from #single.items() s where true IN (true, (s.Name RLIKE $pattern))",
            schemaProvider: provider);
        Assert.AreEqual(1, query.Run().Count());
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(64)]
    public void MembershipMatch_ShouldSkipLaterThrowingMembers(int size)
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.RejectAll,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            { ["#single"] = [new() { Id = 1, Score = 0, Name = "bad" }] });
        var members = "0," + string.Join(',', Enumerable.Repeat("s.Name::int", size - 1));
        using var query = CreateAndRunVirtualMachine(
            $"select s.Id from #single.items() s where s.Score IN ({members})", schemaProvider: provider);
        Assert.AreEqual(1, query.Run().Count());
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(64)]
    public void MembershipMatch_ShouldSkipLaterVolatileMembers(int size)
    {
        var right = new PredicateVolatileRow();
        var provider = new PredicateVolatileProvider(new PredicateVolatileRow(), right);
        var members = "0," + string.Join(',', Enumerable.Repeat("r.Score", size - 1));
        using var query = InstanceCreator.CompileForExecution(
            $"select r.Id from #B.entities() r where r.Score IN ({members})",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(parallelizationMode: ParallelizationMode.None, usePrimitiveTypeValidation: false));
        Assert.AreEqual(1, query.Run().Count());
        Assert.AreEqual(1, right.Reads);
    }

    [TestMethod]
    [DataRow(SourcePlanningMode.RejectAll)]
    [DataRow(SourcePlanningMode.AcceptPredicate)]
    [DataRow(SourcePlanningMode.AcceptFirstPredicate)]
    public void LaterFalseConjunct_ShouldNotSuppressEarlierParsingFailure(SourcePlanningMode mode)
    {
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            {
                ["#left"] = [new() { Id = 1, JoinKey = 1 }],
                ["#right"] = [new() { Id = 10, JoinKey = 1, Name = "bad" }]
            });
        using var query = CreateAndRunVirtualMachine(
            "select l.Id,r.Id from #left.items() l join #right.items() r on l.JoinKey=r.JoinKey where r.Name::int = 1 AND l.Id < 0",
            schemaProvider: provider);
        Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() => query.Run().Count());
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(64)]
    public void VolatileMembershipOperand_ShouldBeEvaluatedOnce(int size)
    {
        var left = new PredicateVolatileRow();
        var right = new PredicateVolatileRow();
        var provider = new PredicateVolatileProvider(left, right);
        var members = string.Join(',', Enumerable.Range(0, size));
        using var query = InstanceCreator.CompileForExecution(
            $"select r.Id from #B.entities() r where r.Score IN ({members})",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(parallelizationMode: ParallelizationMode.None, usePrimitiveTypeValidation: false));
        Assert.AreEqual(1, query.Run().Count());
        Assert.AreEqual(1, right.Reads);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void VolatileJoinedColumn_ShouldBeReadOnlyAtOriginalBoundary(bool hashJoin)
    {
        var left = new PredicateVolatileRow();
        var right = new PredicateVolatileRow();
        var provider = new PredicateVolatileProvider(left, right);
        using var query = InstanceCreator.CompileForExecution(
            "select l.Id,r.Id from #A.entities() l join #B.entities() r on l.Id=r.Id where r.Score=0",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(useHashJoin: hashJoin, useSortMergeJoin: false,
                parallelizationMode: ParallelizationMode.None, usePrimitiveTypeValidation: false));
        Assert.AreEqual(1, query.Run().Count());
        Assert.AreEqual(1, right.Reads);
    }

    [TestMethod]
    [DataRow("r.Name::int = 1", 1)]
    [DataRow("l.Id < 0 AND r.Name::int = 1", 0)]
    public void ParsingCast_ShouldNotEvaluateUnmatchedOrShortCircuitedRows(string predicate, int count)
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.AcceptPredicate,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
            {
                ["#left"] = [new() { Id = 1, JoinKey = 1 }],
                ["#right"] = [new() { Id = 10, JoinKey = 1, Name = "1" }, new() { Id = 11, JoinKey = 99, Name = "bad" }]
            });
        var query = CreateAndRunVirtualMachine(
            $"select l.Id,r.Id from #left.items() l join #right.items() r on l.JoinKey=r.JoinKey where {predicate}",
            schemaProvider: provider);
        Assert.AreEqual(count, query.Run().Count());
    }
}

public sealed class PredicateVolatileRow
{
    public int Id => 1;
    public int Reads { get; set; }
    public int Score => Reads++ % 2;
}
public sealed class PredicateVolatileTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = [new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)), new Musoq.Schema.DataSources.SchemaColumn("Score", 1, typeof(int), ColumnStability.Volatile)];
    public SchemaTableMetadata Metadata { get; } = new(typeof(PredicateVolatileRow));
    public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);
    public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
}
public sealed class PredicateVolatileProvider(PredicateVolatileRow left, PredicateVolatileRow right) : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new GenericSchema<PredicateVolatileRow, PredicateVolatileTable>(
        new[] { schema == "#A" ? left : right }, new Dictionary<string, int> { { "Id", 0 }, { "Score", 1 } },
        new Dictionary<int, Func<PredicateVolatileRow, object?>> { { 0, row => row.Id }, { 1, row => row.Score } });
}
