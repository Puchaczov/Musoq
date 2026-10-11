using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class EvaluationHelperNestedValueTests : GenericEntityTestBase
{
    [TestMethod]
    public void NestedClrProperties_ShouldResolveAndPreserveNulls()
    {
        const string query = "select Position, Child.Value as Value from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<OuterEntity>(query,
            [new(1, new("resolved")), new(2, null)]);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Value", typeof(string)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(1, rows[0].GetValue<int>(0));
        Assert.AreEqual("resolved", rows[0].GetValue<string>(1));
        Assert.AreEqual(2, rows[1].GetValue<int>(0));
        Assert.IsNull(rows[1].GetValue<string>(1));
    }

    [TestMethod]
    public void Dictionaries_ShouldResolveConcreteNestedValues()
    {
        using var compiled = CreateAndRunVirtualMachine<DictionaryEntity>(
            "select Children['Child'].Value as Value from #schema.first()",
            [new(new Dictionary<string, InnerEntity> { ["Child"] = new("dictionary") })]);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        Assert.AreEqual("dictionary", table.Single().GetValue<string>(0));
    }

    [TestMethod]
    public void DynamicObjects_ShouldRejectUnresolvedNestedValues()
    {
        var error = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine<DynamicEntity>(
            "select Child.Value from #schema.first()", [new(new TestDynamicObject())]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Child", error.Message);
    }

    [TestMethod]
    public void Indexers_ShouldResolveNumericAndStringPaths()
    {
        using var compiled = CreateAndRunVirtualMachine<IndexedEntity>(
            "select Items[0].Value as Numeric, Values['child'].Value as Named from #schema.first()",
            [new([new("numeric")], new Dictionary<string, InnerEntity> { ["child"] = new("string") })]);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Numeric", typeof(string)), ("Named", typeof(string)));
        var row = table.Single();
        Assert.AreEqual("numeric", row.GetValue<string>(0));
        Assert.AreEqual("string", row.GetValue<string>(1));
    }

    [TestMethod]
    public void MissingMembers_ShouldReportTheBindingDiagnostic()
    {
        var error = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine<OuterEntity>(
            "select Missing from #schema.first()", [new(1, null)]));
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, error.PrimaryEnvelope.Code);
        Assert.Contains("Missing", error.Message);
    }

    [TestMethod]
    public void ThrowingGetters_ShouldPropagateTheGetterException()
    {
        using var compiled = CreateAndRunVirtualMachine<ThrowingEntity>(
            "select Value from #schema.first()", [new()]);
        var error = Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() =>
        {
            using var table = compiled.Run(TestContext.CancellationToken);
            _ = table.ToArray();
        });
        Assert.IsInstanceOfType<InvalidOperationException>(error.GetBaseException());
        Assert.AreEqual("getter", error.GetBaseException().Message);
    }

    public sealed record OuterEntity(int Position, InnerEntity? Child);
    public sealed record InnerEntity(string Value);
    public sealed record DictionaryEntity(IReadOnlyDictionary<string, InnerEntity> Children);
    public sealed record IndexedEntity(IReadOnlyList<InnerEntity> Items, IReadOnlyDictionary<string, InnerEntity> Values);
    public sealed record DynamicEntity(TestDynamicObject Child);
    public sealed class ThrowingEntity
    {
        public string Value => throw new InvalidOperationException("getter");
    }
    public sealed class TestDynamicObject : DynamicObject;
}
