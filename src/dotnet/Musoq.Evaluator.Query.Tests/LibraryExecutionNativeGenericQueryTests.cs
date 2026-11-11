using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTestsFixtures;
using GenericLibrary = Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTestsFixtures.GenericLibrary;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed partial class LibraryExecutionNativeGenericQueryTests : GenericEntityTestBase
{
    [TestMethod]
    [DataRow("NativeLength($items)")]
    [DataRow("NativeLength(NativeEcho($items))")]
    [DataRow("StructElements($items)")]
    [DataRow("StructElements(NativeEcho($items))")]
    [DataRow("UnmanagedElements($items)")]
    [DataRow("ConstructorElements($items)")]
    public void NestedGeneratedArguments_ShouldBindConcreteCollectionElement(string expression)
    {
        var sql = $"param(items: (Value: int)[]) select {expression} as Length from #schema.first()";
        using var query = CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)]);
        using var document = System.Text.Json.JsonDocument.Parse("[{\"Value\":1},{\"Value\":2}]");
        query.TypedParameters.SetJson("items", document.RootElement);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Length", typeof(int)));
        var rows = table.Select(row => row.GetValue<int>(0)).ToArray();
        CollectionAssert.AreEqual(new[] { 2 }, rows);
    }

    [TestMethod]
    public void GenericConstraintFailure_ShouldReportCallableDiagnosticBeforeExecution()
    {
        const string sql = "select NeedsStruct(Label) as Value from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)])
        );
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, error.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void GeneratedRecordConstraintFailure_ShouldReportCallableDiagnosticBeforeRendering()
    {
        const string sql =
            "param(items: (Value: int)[]) select ReferenceElements($items) as Length from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)])
        );
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, error.PrimaryEnvelope.Code);
        Assert.Contains("generated record carrier is a value type", error.Message);
    }

    [TestMethod]
    [DataRow("NativeEcho", false)]
    [DataRow("StructEcho", false)]
    [DataRow("StructEcho", true)]
    public void GeneratedGenericResult_ShouldExposeItsConcreteCollectionThroughColumnVisitation(
        string function,
        bool throughCte
    )
    {
        var sql = throughCte
            ? $"param(items: (Value: int)[]) with echoed as (select {function}($items) as Echo from #schema.first()) select e.Echo as Echo from echoed e"
            : $"param(items: (Value: int)[]) select {function}($items) as Echo from #schema.first()";
        using var query = CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)]);
        using var document = System.Text.Json.JsonDocument.Parse("[{\"Value\":1},{\"Value\":2}]");
        query.TypedParameters.SetJson("items", document.RootElement);
        using var table = query.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        var column = table.Columns.Single();
        Assert.AreEqual("Echo", column.ColumnName);
        Assert.IsNull(Musoq.Schema.ConcreteValueShape.FindUnresolvedMember(column.ColumnType));
        var visitor = new JsonColumnVisitor();
        rows[0].VisitColumns(ref visitor);
        Assert.AreEqual(column.ColumnType, visitor.Type);
        Assert.AreEqual("[{\"Value\":1},{\"Value\":2}]", visitor.Json);
    }
}
