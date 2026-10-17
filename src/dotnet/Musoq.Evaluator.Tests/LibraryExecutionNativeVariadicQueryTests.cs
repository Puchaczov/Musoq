using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNativeVariadicQueryTests : GenericEntityTestBase
{
    internal const string Query = "select Position, NativeTotal(Value, 1, 2, 3, 4, 5) as Total, " +
        "Concat(Position, Position, Position, Position, Position, Position) as Repeated " +
        "from #schema.first() order by Position";

    [TestMethod]
    public void NativeVariadicQuery_ShouldBindConcreteSpanAndConsumeExpectedResults()
    {
        using var query = CreateAndRunVirtualMachine<Input, SpanLibrary>(Query, [new(1, 10), new(2, -20)]);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table,
            ("Position", typeof(int)), ("Total", typeof(int)), ("Repeated", typeof(string)));
        var rows = table.Select(row => (row.GetValue<int>(0), row.GetValue<int>(1), row.GetValue<string>(2))).ToArray();
        CollectionAssert.AreEqual(new[] { (1, 25, "111111"), (2, -5, "222222") }, rows);
    }

    [TestMethod]
    public void LegacyExpandedArrayDeclaration_ShouldReportNativeInvocationDiagnostic()
    {
        const string sql = "select OldArrayTotal(Value, 1) as Total from #schema.first()";
        var error = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<Input, SpanLibrary>(sql, [new(1, 10)]));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("params ReadOnlySpan<T>", error.Message);
    }

    [TestMethod]
    public void NativeGenericSpanConstraint_ShouldRejectIncompatibleSqlArguments()
    {
        const string sql = "select NativeNumberTotal('text', 'other') as Total from #schema.first()";
        var error = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<Input, SpanLibrary>(sql, [new(1, 10)]));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3088_NoMatchingCallableOverload, error.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void NativeGenericSpanConstraint_ShouldExecuteClosedNumericArguments()
    {
        const string sql = "select NativeNumberTotal(Value, 1, 2) as Total from #schema.first() order by Position";
        using var query = CreateAndRunVirtualMachine<Input, SpanLibrary>(sql, [new(1, 10), new(2, -20)]);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(int)));
        CollectionAssert.AreEqual(new[] { 13, -17 }, table.Select(row => row.GetValue<int>(0)).ToArray());
    }

    [TestMethod]
    public void NativeVariadicDiscovery_ShouldExposeSqlArgumentsWithoutSpanStorage()
    {
        using var query = CreateAndRunVirtualMachine<Input, SpanLibrary>("desc functions #schema", [new(1, 10)]);
        using var table = query.Run(TestContext.CancellationToken);
        var signatures = table.Select(row => row.GetValue<string>(0)).ToArray();
        Assert.Contains("int NativeTotal(int... values)", signatures);
        Assert.IsFalse(signatures.Any(signature => signature.Contains("ReadOnlySpan", StringComparison.Ordinal)));
    }

    public sealed record Input(int Position, int Value);
    public sealed class SpanLibrary : LibraryBase
    {
        [BindableMethod]
        public T NativeNumberTotal<T>(params ReadOnlySpan<T> values) where T : struct, System.Numerics.INumber<T>
        {
            var total = T.Zero;
            foreach (var value in values)
                total = checked(total + value);
            return total;
        }
        [BindableMethod]
        public int OldArrayTotal(params int[] values) => values.Sum();
        [BindableMethod]
        public int NativeTotal(params ReadOnlySpan<int> values)
        {
            var total = 0;
            foreach (var value in values)
                total = checked(total + value);
            return total;
        }
    }
}
