using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputCteTests
{
    [TestMethod]
    public void Sql_CteFilterPreservesOrderDuplicatesAndConstructorDefaults()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "with patterns as (" +
            "select p.Id, p.Pattern from values { (Id: 'todo', Pattern: 'TODO'), (Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') } p " +
            "where p.Id = 'todo') " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO TODO', patterns: patterns) m";
        using var compiled = Compile(query, "structured-input-cte-filter");
        var table = compiled.Run();
        Assert.AreEqual(4, table.Rows.Count);
        Assert.AreEqual("todo", table.Rows[0][0]);
        Assert.AreEqual("todo", table.Rows[1][0]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_EmptyCteStillDemandsTypedSourceAndReturnsNoRows()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "with numbers as (" +
            "select p.Value from values { (Value: 1), (Value: 2) } p where p.Value > 100) " +
            "select n.Value from #inputs.numbers(values: numbers) n";
        using var compiled = Compile(query, "structured-input-cte-empty");
        var table = compiled.Run();
        Assert.AreEqual(0, table.Rows.Count);
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_GroupedCteBindsAsCompleteRelation()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "with numbers as (" +
            "select p.Value from values { (Value: 1), (Value: 2) } p) " +
            "select n.Value from #inputs.numbers((numbers)) n";
        using var compiled = Compile(query, "structured-input-cte-grouped");
        var table = compiled.Run();
        CollectionAssert.AreEqual(new[] { 1, 2 }, table.Rows.Select(static row => (int)row[0]!).ToArray());
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_UnionAllCteUsesOneTypedStoredRowRepresentationForStructuralArguments()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "with patterns as (" +
            "select p.Id, p.Pattern, p.Mode from values { (Id: 'todo', Pattern: 'TODO', Mode: 'literal') } p " +
            "union all (Id, Pattern, Mode) " +
            "select q.Id, q.Pattern, q.Mode from values { (Id: 'fixme', Pattern: 'FIXME', Mode: 'literal') } q) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: patterns) m";

        var inspection = InstanceCreator.CompileForInspection(
            query,
            "structured-input-cte-union-all-typed",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        StringAssert.Contains(inspection.ExecutionPlanText, "StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0LeftRow0>]");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "var cte0 = new List<Cte0LeftRow0>");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "new Cte0LeftRow0((string)cte0RightRow.q_Id");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("CastGeneratedRows<Cte0LeftRow0>", StringComparison.Ordinal));

        using var compiled = Compile(query, "structured-input-cte-union-all-typed-execution");
        using var table = compiled.Run();
        CollectionAssert.AreEqual(
            new[] { "todo:TODO", "fixme:FIXME" },
            table.Rows.Select(static row => $"{row[0]}:{row[1]}").ToArray());
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    private static CompiledQuery Compile(string query, string name)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(System.Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return build.CompiledQuery!;
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public Microsoft.Extensions.Logging.ILogger ResolveLogger() => NullLogger.Instance;
        public Microsoft.Extensions.Logging.ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
