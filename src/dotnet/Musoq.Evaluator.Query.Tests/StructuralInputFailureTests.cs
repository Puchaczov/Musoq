using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class StructuralInputFailureTests
{
    [TestMethod]
    [DataRow("(Value: int)", "{\"Value\":7}", "{\"Value\":7}")]
    [DataRow("(Value: int)", "{\"value\":7}", "{\"Value\":7}")]
    [DataRow("(Value: int?)", "{\"Value\":null}", "{\"Value\":null}")]
    [DataRow("(Name: string = 'default')", "{}", "{\"Name\":\"default\"}")]
    [DataRow("(Value: int)[]", "[{\"Value\":9},{\"Value\":9}]", "[{\"Value\":9},{\"Value\":9}]")]
    public void NativeStructuredParameter_ShouldPreserveFieldsDefaultsNullsAndRepeatedValues(string type, string json, string expected)
    {
        using var compiled = CompileNative($"param(value: {type}) select $value as Value from #schema.first()");
        using var document = JsonDocument.Parse(json);
        compiled.TypedParameters.SetJson("value", document.RootElement);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual("Value", table.Columns.Single().ColumnName);
        Assert.IsNull(ConcreteValueShape.FindUnresolvedMember(table.Columns.Single().ColumnType));
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        var visitor = new NativeJsonVisitor();
        rows[0].VisitColumns(ref visitor);
        Assert.AreEqual(table.Columns.Single().ColumnType, visitor.Type);
        Assert.AreEqual(expected, visitor.Json);
    }

    [TestMethod]
    [DataRow("(Value: int)", "{\"Value\":null}", DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed)]
    [DataRow("(Value: int)", "{}", DiagnosticCode.MQ7003_RequiredScriptParameterMissing)]
    [DataRow("(Value: int)", "{\"Value\":1,\"value\":2}", DiagnosticCode.MQ7004_ScriptParameterTypeMismatch)]
    [DataRow("(Value: int)", "{\"Other\":1}", DiagnosticCode.MQ7006_UnknownScriptParameter)]
    [DataRow("(Value: int)", "[]", DiagnosticCode.MQ7004_ScriptParameterTypeMismatch)]
    [DataRow("(Value: int)[]", "{}", DiagnosticCode.MQ7004_ScriptParameterTypeMismatch)]
    public void NativeStructuredParameter_WhenShapeIsInvalid_ShouldDiagnoseBeforeResultConsumption(string type, string json, DiagnosticCode code)
    {
        using var compiled = CompileNative($"param(value: {type}) select $value as Value from #schema.first()");
        using var document = JsonDocument.Parse(json);
        compiled.TypedParameters.SetJson("value", document.RootElement);
        var error = Assert.Throws<QueryExecutionException>(() =>
        {
            using var table = compiled.Run(TestContext.CancellationToken);
            _ = table.ToArray();
        });
        var binding = Assert.IsInstanceOfType<ScriptParameterBindingException>(error.GetBaseException());
        Assert.AreEqual(code, binding.Code);
        Assert.Contains("value", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NativeStructuredJson_ShouldOwnClonedInputAfterDocumentDisposal(bool collection)
    {
        var type = collection ? "(Value: int)[]" : "(Value: int)";
        var json = collection ? "[{\"Value\":7}]" : "{\"Value\":7}";
        using var compiled = CompileNative($"param(value: {type}) select $value as Value from #schema.first()");
        using (var document = JsonDocument.Parse(json))
            compiled.TypedParameters.SetJson("value", document.RootElement);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        var visitor = new NativeJsonVisitor();
        rows[0].VisitColumns(ref visitor);
        Assert.AreEqual(json, visitor.Json);
    }

    [TestMethod]
    public void NativeParameterHostBoundary_ShouldRejectErasedAndCyclicObjects()
    {
        using var compiled = CompileNative("param(value: (Value: int)) select $value as Value from #schema.first()");
        var cycle = new Dictionary<string, object?>();
        cycle["Child"] = cycle;
        var erased = Assert.ThrowsExactly<ArgumentException>(() => compiled.TypedParameters.Set<object>("value", cycle));
        Assert.Contains("concrete scalar parameter type", erased.Message);
        var dictionary = Assert.ThrowsExactly<ArgumentException>(() => compiled.TypedParameters.Set("value", cycle));
        Assert.Contains("SetJson", dictionary.Message);
        var untyped = Assert.ThrowsExactly<ArgumentException>(() => compiled.TypedParameters.Set("value", new ArrayList { 1, 2 }));
        Assert.Contains("SetCollection", untyped.Message);
    }

    [TestMethod]
    public void NativeStructuredParameter_WhenRunIsCancelled_ShouldNotConsumeInput()
    {
        using var compiled = CompileNative("param(value: (Value: int)) select $value as Value from #schema.first()");
        using var document = JsonDocument.Parse("{\"Value\":7}");
        compiled.TypedParameters.SetJson("value", document.RootElement);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => compiled.Run(cancellation.Token));
    }

    [TestMethod]
    public void NativeParameterBudget_ShouldBeSharedAcrossStructuredRoots()
    {
        using var compiled = CompileNative("param(first: (Value: int)[], second: (Value: int)[]) select $first as First, $second as Second from #schema.first()");
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(Enumerable.Range(0, 30_000).Select(value => new { Value = value })));
        compiled.TypedParameters.SetJson("first", document.RootElement);
        compiled.TypedParameters.SetJson("second", document.RootElement);
        var error = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.ToArray();
        });
        Assert.Contains("maximum value nodes", error.ToString());
    }

    [TestMethod]
    public void CaptureStateUsesCheckedMetricsAndCollectionLowerBounds()
    {
        var state = new StructuralParameterCaptureState(CancellationToken.None);
        state.ReserveNode(1);
        Assert.ThrowsExactly<OverflowException>(() =>
            state.ReserveMetrics(new StructuralInputMetrics(1, long.MaxValue, 0), 1));

        var bounded = new StructuralParameterCaptureState(
            CancellationToken.None,
            new StructuralInputLimits(4, 2, 64));
        bounded.ReserveNode(1);
        Assert.ThrowsExactly<InvalidOperationException>(() => bounded.EnsureCollectionLowerBound(2));
    }

    [TestMethod]
    public void StructuralLimitExceptionExposesStableDiagnosticFacts()
    {
        var exception = new StructuralInputLimitExceededException(
            "parameter",
            "$.patterns[0].Pattern",
            StructuralInputLimitKind.StringBytes,
            8,
            10,
            "source-1");

        var diagnostic = exception.ToDiagnostic();
        Assert.AreEqual(DiagnosticCode.MQ7013_StructuralInputLimitExceeded, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, diagnostic.SourceKind);
        Assert.AreEqual("parameter", diagnostic.Arguments["origin"]);
        Assert.AreEqual("$.patterns[0].Pattern", diagnostic.Arguments["path"]);
        Assert.AreEqual("StringBytes", diagnostic.Arguments["limitKind"]);
        Assert.AreEqual("8", diagnostic.Arguments["configuredLimit"]);
        Assert.AreEqual("10", diagnostic.Arguments["observedValue"]);
        Assert.AreEqual("source-1", diagnostic.Arguments["sourceContextId"]);
    }


    private static CompiledQuery CompileNative(string sql)
        => InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"),
            LibraryExecutionFixture.CreateProvider(new[] { new SnapshotRow(0) }), new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

    private struct NativeJsonVisitor : IColumnValueVisitor
    {
        internal string? Json;
        internal Type? Type;
        public void Visit<T>(int columnIndex, in T value)
        {
            Assert.AreEqual(0, columnIndex);
            Type = typeof(T);
            Json = JsonSerializer.Serialize(value);
        }
    }
}
