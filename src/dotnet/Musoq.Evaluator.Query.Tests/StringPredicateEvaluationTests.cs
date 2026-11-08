using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StringPredicateEvaluationTests
{
    [TestMethod]
    [DataRow("Contains", "ToString(1)", "'1'", true)]
    [DataRow("StartsWith", "ToString(1)", "'1'", true)]
    [DataRow("Contains", "FromBytesToString(FromHexToBytes('414243'))", "'B'", true)]
    [DataRow("StartsWith", "FromBytesToString(FromHexToBytes('414243'))", "'A'", true)]
    [DataRow("Contains", "'Ä猫Β'", "'猫'", true)]
    [DataRow("StartsWith", "'Ä猫Β'", "'ä'", true)]
    [DataRow("Contains", "'ABC'", "'Q'", false)]
    [DataRow("Contains", "null", "'B'", null)]
    [DataRow("Contains", "'ABC'", "null", null)]
    [DataRow("StartsWith", "null", "null", null)]
    public void StringPredicate_ShouldPreserveTypedReceiverAndNullSemantics(string function, string value, string pattern, bool? expected)
    {
        using var query = Compile($"select {function}({value}, {pattern}) as P from #schema.first() d");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { expected });
    }

    [TestMethod]
    [DataRow("Contains", "'B'")]
    [DataRow("StartsWith", "'A'")]
    public void DecodedStringPredicate_InWhere_ShouldReturnTheMatchingRow(string function, string pattern)
    {
        using var query = Compile($"select d.Dummy from #schema.first() d where {function}(FromBytesToString(FromHexToBytes('414243')), {pattern})");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["x"]);
    }

    [TestMethod]
    [DataRow("Contains", false, false)]
    [DataRow("Contains", true, false)]
    [DataRow("Contains", false, true)]
    [DataRow("Contains", true, true)]
    [DataRow("StartsWith", false, false)]
    [DataRow("StartsWith", true, false)]
    [DataRow("StartsWith", false, true)]
    [DataRow("StartsWith", true, true)]
    public void VolatileArguments_ShouldRunOnceInOrderEvenWhenReceiverIsNull(string function, bool nullReceiver, bool nullPattern)
    {
        using var trace = new TraceScope();
        var left = trace.Argument("left", nullReceiver ? null : "AB");
        var right = trace.Argument("right", nullPattern ? null : "A");
        using var query = Compile($"select {function}({left}, {right}) as P from #schema.first() d");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { nullReceiver || nullPattern ? null : (bool?)true });
        CollectionAssert.AreEqual(new[] { "left", "right" }, trace.Values);
    }

    [TestMethod]
    [DataRow("Contains")]
    [DataRow("StartsWith")]
    public void NullReceiver_InWhere_ShouldStillEvaluateThePattern(string function)
    {
        using var trace = new TraceScope();
        using var query = Compile($"select d.Dummy from #schema.first() d where {function}({trace.Argument("left", null)}, {trace.Argument("right", "A")})");
        using var table = query.Run();
        Assert.HasCount(0, table);
        CollectionAssert.AreEqual(new[] { "left", "right" }, trace.Values);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CaseRegion_ShouldKeepVolatileArgumentEvaluationInsideTheSelectedBranch(bool selected)
    {
        using var trace = new TraceScope();
        var expression = $"Contains({trace.Argument("left", "AB")}, {trace.Argument("right", "B")})";
        using var query = Compile($"select case when {(selected ? "1 = 1" : "1 = 0")} then {expression} else false end as P from #schema.first() d");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [selected]);
        CollectionAssert.AreEqual(selected ? new[] { "left", "right" } : Array.Empty<string>(), trace.Values);
    }

    [TestMethod]
    public void NestedPredicates_ShouldKeepArgumentOrderAndDistinctTemporaryBindings()
    {
        using var trace = new TraceScope();
        var inner = $"StartsWith({trace.Argument("inner-left", "AB")}, {trace.Argument("inner-right", "A")})";
        var value = $"case when {inner} then {trace.Argument("chosen", "BC")} else {trace.Argument("unused", "Q")} end";
        using var query = Compile($"select Contains({value}, {trace.Argument("outer-right", "B")}) as P from #schema.first() d");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true]);
        CollectionAssert.AreEqual(new[] { "inner-left", "inner-right", "chosen", "outer-right" }, trace.Values);
    }

    [TestMethod]
    public void ComputedPredicate_ShouldHaveTypedGeneratedExecutionWithoutBoxing()
    {
        const string sql = "select Contains(ToString(1), '1') as P from #schema.first() d";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    [DataRow("__musoqStringPredicate1Value")]
    [DataRow("__musoqStringPredicate1Pattern")]
    public void CaptureNames_ShouldNotReserveLegalSourceAliases(string alias)
    {
        using var query = Compile($"select Contains(ToString(1), '1') as P from #schema.first() {alias}");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true]);
    }

    [TestMethod]
    public void RepeatedVolatileArguments_ShouldRemainSeparateOrderedCalls()
    {
        using var trace = new TraceScope();
        var arguments = trace.Argument("left", "AB") + ", " + trace.Argument("right", "A");
        using var query = Compile($"select Contains({arguments}) as A, StartsWith({arguments}) as B from #schema.first() d");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, true]);
        CollectionAssert.AreEqual(new[] { "left", "right", "left", "right" }, trace.Values);
    }

    [TestMethod]
    public void ComputedArguments_ShouldUseTypedLocalCapturesWithoutRuntimeWrappers()
    {
        const string sql = "select Contains(ToString(1), '1') as P from #schema.first() d";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), CreateProvider(),
            new TestsLoggerResolver(), new CompilationOptions());
        Assert.Contains("__musoqStringPredicate", inspection.GeneratedCSharpCode);
        Assert.Contains("switch", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain(".DynamicInvoke(", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("new object[", inspection.GeneratedCSharpCode);
    }

    private static CompiledQuery Compile(string sql) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), CreateProvider(), new TestsLoggerResolver(), new CompilationOptions());

    private static ISchemaProvider CreateProvider() =>
        LibraryExecutionFixture.CreateProvider<PredicateRow, Functions>([new("x")]);

    public readonly record struct PredicateRow(string Dummy);

    public sealed class Functions : LibraryBase
    {
        internal static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> Traces = new();

        [BindableMethod]
        [NonDeterministic]
        public string? Mark(string key, string label, string? value)
        {
            Traces[key].Enqueue(label);
            return value;
        }
    }

    private sealed class TraceScope : IDisposable
    {
        private readonly string _key = Guid.NewGuid().ToString("N");
        private readonly ConcurrentQueue<string> _values = new();
        internal TraceScope() => Functions.Traces[_key] = _values;
        internal string[] Values => _values.ToArray();
        internal string Argument(string label, string? value) => $"Mark('{_key}', '{label}', {Quote(value)})";
        private static string Quote(string? value) => value is null ? "null" : "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
        public void Dispose() => Functions.Traces.TryRemove(_key, out _);
    }
}
