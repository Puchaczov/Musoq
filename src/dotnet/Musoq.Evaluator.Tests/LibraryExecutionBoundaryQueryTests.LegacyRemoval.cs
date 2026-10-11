using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionBoundaryQueryTests
{
    [TestMethod]
    public void TableQuery_WhenNumericSourceIsErased_ShouldReportTheProviderContract()
    {
        const string query = "select Label from #schema.first() where Value > 10";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<ErasedNumericInput, LibraryBase>(query, [new("object", 42)]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Value", error.Message);
        Assert.Contains("concrete value type", error.Message);
    }

    [TestMethod]
    public void TableQuery_WhenNumericSourceIsConcrete_ShouldConsumeTheExpectedTypedRows()
    {
        const string query = "select Label, Value from #schema.first() where Value > 10 order by Label";
        ConcreteNumericInput[] input = [new("above", 42m), new("below", 5m), new("null", null)];
        using var compiled = CreateAndRunVirtualMachine<ConcreteNumericInput, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)), ("Value", typeof(decimal?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual("above", rows[0].GetValue<string>(0));
        Assert.AreEqual(42m, rows[0].GetValue<decimal?>(1));
    }

    [TestMethod]
    public void TableCouple_WhenDeclaredFieldIsObject_ShouldRejectTheErasedExecutionContract()
    {
        const string query = "table Erased { Value: object }; " +
            "couple #schema.first with table Erased as Typed; select Value from Typed()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<ErasedNumericInput, LibraryBase>(query, [new("object", 42)]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Value", error.Message);
        Assert.Contains("object-valued", error.Message);
    }

    [TestMethod]
    public void TableQuery_WhenNestedSourceHasNoConcreteShape_ShouldRejectDynamicExecution()
    {
        const string query = "select Complex.Id from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<UnresolvedNestedInput, LibraryBase>(query, [new(new ExpandoObject())]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Complex", error.Message);
    }

    [TestMethod]
    public void NestedDictionary_WhenItsValuesAreErased_ShouldRejectTheProviderShape()
    {
        const string query = "select Values['amount'] from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<ErasedDictionaryInput, LibraryBase>(query,
                [new(new Dictionary<string, object> { ["amount"] = 42m })]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Values", error.Message);
    }

    [TestMethod]
    public void ReadOnlyTableContract_WhenConsumed_ShouldExposeConcreteNullableValues()
    {
        const string query = "select Label, Value from #schema.first() order by Label";
        using var compiled = CreateAndRunVirtualMachine<ConcreteNumericInput, LibraryBase>(query,
            [new("first", 42m), new("second", null)]);
        using var result = compiled.Run(TestContext.CancellationToken);
        IReadOnlyTable contract = result;
        Assert.HasCount(2, contract.Rows);
        Assert.AreEqual(2, contract.Rows[0].Count);
        Assert.AreEqual("first", contract.Rows[0].GetValue<string>(0));
        Assert.AreEqual(42m, contract.Rows[0].GetValue<decimal?>(1));
        Assert.AreEqual("second", contract.Rows[1].GetValue<string>(0));
        Assert.IsNull(contract.Rows[1].GetValue<decimal?>(1));
    }

    [TestMethod]
    public void RowContract_ShouldRequireConcreteVisitationWithoutAValuesBridge()
    {
        Assert.IsNull(typeof(Musoq.Evaluator.Tables.Row).GetProperty("Values"));
        Assert.IsTrue(typeof(Musoq.Evaluator.Tables.Row).GetMethod("VisitColumns")!.IsAbstract);
    }

    public sealed record ErasedNumericInput(string Label, object Value);
    public sealed record ErasedDictionaryInput(IReadOnlyDictionary<string, object> Values);
    public sealed record UnresolvedNestedInput(ExpandoObject Complex);
    public sealed record ConcreteNumericInput(string Label, decimal? Value);
}
