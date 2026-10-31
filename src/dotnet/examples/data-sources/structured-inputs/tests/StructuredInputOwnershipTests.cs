using Musoq.Tests.Common;
using System.Collections;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputOwnershipTests
{
    [TestMethod]
    public void Sql_HostSnapshotIsIndependentAndEachRunRecapturesInputs()
    {
        StructuredInputSourceCounters.Reset();
        using var compiled = Compile(
            "param(patterns: (Id: string, Pattern: string)[]) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: $patterns) m",
            "structured-input-ownership");
        var record = new Dictionary<string, object?>
        {
            ["Id"] = "todo",
            ["Pattern"] = "TODO"
        };
        var host = new List<Dictionary<string, object?>> { record };
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "patterns", host);

        string firstMatch;
        using (var first = compiled.Run())
        {
            firstMatch = (string)first.Rows.Single().ReadValue(1)!;
        }
        record["Pattern"] = "FIXME";
        compiled.TypedParameters.SetJson("patterns", System.Text.Json.JsonSerializer.SerializeToElement(host));
        string secondMatch;
        using (var second = compiled.Run())
        {
            secondMatch = (string)second.Rows.Single().ReadValue(1)!;
        }

        Assert.AreEqual("TODO", firstMatch);
        Assert.AreEqual("FIXME", secondMatch);
        Assert.AreEqual(2, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_InvalidHostValueDoesNotOpenTheProvider()
    {
        var query =
            "param(patterns: (Id: string, Pattern: string)[]) " +
            "select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
        using var compiled = Compile(query, "structured-input-invalid-runtime");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "patterns", new List<Dictionary<string, object?>>
        {
            new()
            {
                ["Id"] = "todo",
                ["Unknown"] = "TODO"
            }
        });

        StructuredInputSourceCounters.Reset();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => RunAndMaterialize(compiled));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ7006_UnknownScriptParameter, exception.Envelope!.Code);
        StringAssert.Contains(exception.Message, "$patterns[0].Unknown");
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_CancellationDuringNativeCollectionCaptureDoesNotOpenTheProvider()
    {
        const string sql = "param(values: int[]) select n.Value from #inputs.numbers(values: $values) n";
        using var compiled = Compile(sql, "structured-input-cancel-native-capture");
        using var cancellation = new CancellationTokenSource();
        compiled.TypedParameters.SetCollection("values", new CancelOnReadList(cancellation, 1));
        StructuredInputSourceCounters.Reset();
        Assert.ThrowsExactly<OperationCanceledException>(() => compiled.Run(cancellation.Token));
        Assert.AreEqual(0, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_CancellationBeforeExecutionDoesNotConstructAProvider()
    {
        using var compiled = Compile(
            "select m.PatternId from #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') }) m",
            "structured-input-cancel-before");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        StructuredInputSourceCounters.Reset();

        Assert.ThrowsExactly<OperationCanceledException>(() => compiled.Run(cancellation.Token));
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_ProviderDomainErrorsAreWrappedAfterTypedConstruction()
    {
        using var compiled = Compile(
            "select m.PatternId from #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO', Mode: 'glob') }) m",
            "structured-input-provider-error");
        StructuredInputSourceCounters.Reset();

        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => RunAndMaterialize(compiled));
        StringAssert.Contains(exception.ToString(), "glob");
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_EmptyTypedCollectionsStillDemandTheProvider()
    {
        using var compiled = Compile(
            "select n.Value from #inputs.numbers(values: array {}) n",
            "structured-input-empty-demand");
        StructuredInputSourceCounters.Reset();

        var table = compiled.Run();

        Assert.AreEqual(0, table.Rows.Count);
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_MutableReceivingObjectsAreFreshForEverySourceInvocation()
    {
        using var compiled = Compile(
            "select m.Value from #inputs.mutable(items: array { (Value: 1) }) m",
            "structured-input-mutable-identity");
        StructuredInputSourceCounters.Reset();

        using (var first = compiled.Run())
            Assert.AreEqual(2, first.Rows.Single().ReadValue(0));
        using (var second = compiled.Run())
            Assert.AreEqual(2, second.Rows.Single().ReadValue(0));

        Assert.AreEqual(2, StructuredInputSourceCounters.MutableConstructed);
    }

    private static void RunAndMaterialize(CompiledQuery compiled)
    {
        using var table = compiled.Run();
        _ = table.Count;
    }
    private static CompiledQuery Compile(string query, string queryId)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            queryId,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return build.CompiledQuery!;
    }

    private sealed class CancelOnReadList(
        CancellationTokenSource cancellation,
        int value) : IReadOnlyList<int>
    {
        public int Count => 1;

        public int this[int index]
        {
            get
            {
                cancellation.Cancel();
                return value;
            }
        }

        public IEnumerator<int> GetEnumerator() =>
            new List<int> { value }.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public Microsoft.Extensions.Logging.ILogger ResolveLogger() =>
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        public Microsoft.Extensions.Logging.ILogger<T> ResolveLogger<T>() =>
            Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
    }
}
