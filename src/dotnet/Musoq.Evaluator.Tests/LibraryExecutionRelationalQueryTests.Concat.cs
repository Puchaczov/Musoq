using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionConcatQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void ConcatQuery_WithDifferentConcreteTypes_ShouldPreserveNullsAndValues()
    {
        const string query = "select Position, Concat(Label, Value) as Pair, " +
            "Concat(Label, ':', Value) as Triple, Concat(Label, ':', Value, '!') as Four " +
            "from #schema.first() order by Position";
        ConcatInput[] input = [new(1, "A", 42), new(2, "B", null), new(3, null, 7)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Pair", typeof(string)),
            ("Triple", typeof(string)), ("Four", typeof(string)));
        Assert.HasCount(3, rows);
        Assert.AreEqual("A42", rows[0].GetValue<string>(1));
        Assert.AreEqual("A:42", rows[0].GetValue<string>(2));
        Assert.AreEqual("A:42!", rows[0].GetValue<string>(3));
        for (var index = 1; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.IsNull(rows[index].GetValue<string>(1));
            Assert.IsNull(rows[index].GetValue<string>(2));
            Assert.IsNull(rows[index].GetValue<string>(3));
        }
    }

    [TestMethod]
    public void GenericScalarQuery_WhenATypeParameterIsUninferred_ShouldReportBindingDiagnostic()
    {
        const string query = "select Uninferred(Value) as Output from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<ConcatInput, UninferredLibrary>(query, [new(1, "A", 42)]));
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, error.PrimaryEnvelope.Code);
        Assert.Contains("cannot be inferred", error.Message);
    }

    public sealed class UninferredLibrary : LibraryBase
    {
        [BindableMethod]
        public string Uninferred<T, TResult>(T value) => throw new System.InvalidOperationException("Uninferred declarations must not execute.");
    }

    public sealed record ConcatInput(int Position, string? Label, int? Value);
}
