using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StructuralInputFailureTests
{
    [TestMethod]
    public void CaptureRejectsNonGenericIListAtTheHostBoundary()
    {
        var definition = Definition(IntArray(), new StructuralInputLimits(4, 8, 64));

        var exception = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            definition,
            new ArrayList { 1, 2 }));

        StringAssert.Contains(exception.InnerException?.Message ?? exception.Message, "IReadOnlyList");
    }

    [TestMethod]
    public void CaptureRejectsNegativeAndChangingIndexedCollections()
    {
        var definition = Definition(IntArray(), new StructuralInputLimits(4, 8, 64));

        var negative = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            definition,
            new NegativeCountList()));
        StringAssert.Contains(negative.InnerException?.Message ?? negative.Message, "negative");

        var changing = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            definition,
            new ThrowingItemList()));
        StringAssert.Contains(changing.InnerException?.Message ?? changing.Message, "could not read element");
    }

    [TestMethod]
    public void CaptureUnwrapsCancellationFromIndexedCollectionGetter()
    {
        using var cancellation = new CancellationTokenSource();
        var definition = Definition(IntArray(), new StructuralInputLimits(4, 8, 64));

        Assert.ThrowsExactly<OperationCanceledException>(() => StructuralParameterSnapshotter.Capture(
            new[] { definition },
            new Dictionary<string, object?>
            {
                ["value"] = new CancellingList(cancellation, 1)
            },
            cancellation.Token));
    }

    [TestMethod]
    public void CaptureChecksCancellationBeforeReadingAnyHostValue()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var definition = Definition(IntArray(), new StructuralInputLimits(4, 8, 64));

        Assert.ThrowsExactly<OperationCanceledException>(() => StructuralParameterSnapshotter.Capture(
            new[] { definition },
            new Dictionary<string, object?> { ["value"] = new[] { 1 } },
            cancellation.Token));
    }

    [TestMethod]
    public void CaptureDetectsCyclesButAllowsRepeatedReferencesInSeparateBranches()
    {
        var child = Record(
            new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int)), required: true));
        var root = Record(
            new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int)), required: true),
            new StructuralFieldDescriptor("Child", child, required: true));
        var definition = Definition(root, new StructuralInputLimits(8, 32, 128));

        var cyclic = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Value"] = 7
        };
        cyclic["Child"] = cyclic;
        var cycleException = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(definition, cyclic));
        StringAssert.Contains(cycleException.InnerException?.Message ?? cycleException.Message, "cyclic");

        var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Value"] = 9 };
        var collectionDefinition = Definition(
            StructuralTypeDescriptor.Collection(typeof(StructuralValue[]), child),
            new StructuralInputLimits(8, 32, 128));
        var snapshot = Capture(collectionDefinition, new List<Dictionary<string, object?>> { item, item });
        var values = (StructuralValue[])snapshot["value"]!;
        Assert.AreEqual(2, values.Length);
        Assert.AreEqual(9, values[0].Fields["Value"].Scalar);
        Assert.AreEqual(9, values[1].Fields["Value"].Scalar);
        Assert.AreNotSame(values[0], values[1]);
    }

    [TestMethod]
    public void CapturePreservesNullabilityAndRejectsNullRequiredValues()
    {
        var nullable = Definition(
            StructuralTypeDescriptor.Scalar(typeof(int), nullable: true),
            new StructuralInputLimits(2, 4, 32));
        var nullSnapshot = Capture(nullable, null);
        Assert.IsNull(nullSnapshot["value"]);

        var required = Definition(
            StructuralTypeDescriptor.Scalar(typeof(int)),
            new StructuralInputLimits(2, 4, 32));
        Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(required, null));

        var record = Record(
            new StructuralFieldDescriptor("Optional", StructuralTypeDescriptor.Scalar(typeof(int), nullable: true), required: true));
        var recordSnapshot = Capture(Definition(record, new StructuralInputLimits(4, 8, 32)),
            new Dictionary<string, object?> { ["Optional"] = null });
        var value = (StructuralValue)recordSnapshot["value"]!;
        Assert.IsTrue(value.Fields.ContainsKey("Optional"));
        Assert.IsNull(value.Fields["Optional"].Scalar);
    }

    [TestMethod]
    public void CaptureEnforcesDepthNodeAndStringBoundaries()
    {
        var child = Record(new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int)), true));
        var root = Record(new StructuralFieldDescriptor("Child", child, true));
        var input = new Dictionary<string, object?>
        {
            ["Child"] = new Dictionary<string, object?> { ["Value"] = 1 }
        };

        Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(root, new StructuralInputLimits(2, 16, 64)), input));
        Assert.IsNotNull(Capture(Definition(root, new StructuralInputLimits(3, 16, 64)), input)["value"]);

        Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(IntArray(), new StructuralInputLimits(4, 1, 64)), new[] { 1 }));
        var nodeBoundary = Capture(Definition(IntArray(), new StructuralInputLimits(4, 2, 64)), new[] { 1 });
        CollectionAssert.AreEqual(new[] { 1 }, (int[])nodeBoundary["value"]!);

        var stringType = StructuralTypeDescriptor.Scalar(typeof(string));
        Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(stringType, new StructuralInputLimits(2, 4, 1)), "x"));
        Assert.AreEqual("x", Capture(Definition(stringType, new StructuralInputLimits(2, 4, 2)), "x")["value"]);
    }

    [TestMethod]
    public void CaptureMaterializesDeclaredDefaultsAsPresentFields()
    {
        var descriptor = Record(new StructuralFieldDescriptor(
            "Name",
            StructuralTypeDescriptor.Scalar(typeof(string)),
            required: false,
            @default: StructuralDefaultDescriptor.Create("default")));

        Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(descriptor, new StructuralInputLimits(4, 1, 64)),
            new Dictionary<string, object?>()));

        var snapshot = Capture(Definition(descriptor, new StructuralInputLimits(4, 2, 64)),
            new Dictionary<string, object?>());
        var record = (StructuralValue)snapshot["value"]!;
        Assert.IsTrue(record.Fields.ContainsKey("Name"));
        Assert.AreEqual("default", record.Fields["Name"].Scalar);
    }

    [TestMethod]
    public void CaptureRejectsNonStringAndDuplicateLogicalDictionaryKeys()
    {
        var descriptor = Record(new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int)), true));

        var keyException = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(descriptor, new StructuralInputLimits(4, 8, 64)),
            new Dictionary<int, object?> { [1] = 1 }));
        StringAssert.Contains(keyException.InnerException?.Message ?? keyException.Message, "string");

        var duplicateException = Assert.ThrowsExactly<ScriptParameterBindingException>(() => Capture(
            Definition(descriptor, new StructuralInputLimits(4, 8, 64)),
            new DuplicateReadOnlyDictionary()));
        StringAssert.Contains(duplicateException.InnerException?.Message ?? duplicateException.Message, "duplicate");
    }

    [TestMethod]
    public void CaptureOwnsTheSnapshotAndDoesNotReuseMutableHostState()
    {
        var descriptor = StructuralTypeDescriptor.Collection(
            typeof(StructuralValue[]),
            Record(new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(string)), true)));
        var definition = Definition(descriptor, new StructuralInputLimits(4, 16, 64));
        var hostRecord = new Dictionary<string, object?> { ["Value"] = "before" };
        var host = new List<Dictionary<string, object?>> { hostRecord };

        var snapshot = Capture(definition, host);
        hostRecord["Value"] = "after";
        host.Add(new Dictionary<string, object?> { ["Value"] = "new" });

        var values = (StructuralValue[])snapshot["value"]!;
        Assert.AreEqual(1, values.Length);
        Assert.AreEqual("before", values[0].Fields["Value"].Scalar);
    }

    [TestMethod]
    public void CaptureWithMetricsCountsPresentNodesStringsAndAppliedDefaultsPerRoot()
    {
        var nullableString = StructuralTypeDescriptor.Scalar(typeof(string), nullable: true);
        var descriptor = Record(
            new StructuralFieldDescriptor("Text", nullableString, required: true),
            new StructuralFieldDescriptor("Optional", StructuralTypeDescriptor.Scalar(typeof(int), nullable: true), required: false),
            new StructuralFieldDescriptor(
                "Items",
                StructuralTypeDescriptor.Collection(typeof(string[]), nullableString),
                required: true),
            new StructuralFieldDescriptor(
                "Mode",
                nullableString,
                required: false,
                @default: StructuralDefaultDescriptor.Create("xy")));
        var definition = Definition(descriptor, new StructuralInputLimits(8, 32, 128));

        var snapshot = StructuralParameterSnapshotter.CaptureWithMetrics(
            new[] { definition },
            new Dictionary<string, object?>
            {
                ["value"] = new Dictionary<string, object?>
                {
                    ["Text"] = "abc",
                    ["Optional"] = null,
                    ["Items"] = new string?[] { "d", null }
                }
            },
            CancellationToken.None);

        Assert.AreEqual(new StructuralInputMetrics(3, 7, 12), snapshot.Metrics["value"]);
        Assert.AreEqual(snapshot.Metrics["value"], snapshot.AggregateMetrics);
        var value = (StructuralValue)snapshot.Values["value"]!;
        Assert.IsTrue(value.Fields.ContainsKey("Mode"));
        Assert.IsTrue(value.Fields.ContainsKey("Optional"));
        Assert.IsNull(value.Fields["Optional"].Scalar);
    }

    [TestMethod]
    public void CaptureWithMetricsSharesNodeAndStringBudgetsAcrossParameterRoots()
    {
        var stringType = StructuralTypeDescriptor.Scalar(typeof(string));
        var first = Definition(stringType, StructuralInputLimits.Default);
        var second = new ScriptParameterDefinition(ScriptParameterContract.CreateStructural(
            "second",
            "string",
            stringType,
            hasDefaultValue: false,
            defaultValue: null));

        var snapshot = StructuralParameterSnapshotter.CaptureWithMetrics(
            new[] { first, second },
            new Dictionary<string, object?>
            {
                ["value"] = "a",
                ["second"] = "bb"
            },
            CancellationToken.None);

        Assert.AreEqual(new StructuralInputMetrics(1, 1, 2), snapshot.Metrics["value"]);
        Assert.AreEqual(new StructuralInputMetrics(1, 1, 4), snapshot.Metrics["second"]);
        Assert.AreEqual(new StructuralInputMetrics(1, 2, 6), snapshot.AggregateMetrics);
    }

    [TestMethod]
    public void CaptureWithMetricsLeavesMissingOptionalFieldsAbsent()
    {
        var descriptor = Record(
            new StructuralFieldDescriptor(
                "Optional",
                StructuralTypeDescriptor.Scalar(typeof(string), nullable: true),
                required: false));

        var snapshot = StructuralParameterSnapshotter.CaptureWithMetrics(
            new[] { Definition(descriptor, StructuralInputLimits.Default) },
            new Dictionary<string, object?>
            {
                ["value"] = new Dictionary<string, object?>()
            },
            CancellationToken.None);

        Assert.AreEqual(new StructuralInputMetrics(1, 1, 0), snapshot.Metrics["value"]);
        var value = (StructuralValue)snapshot.Values["value"]!;
        Assert.IsFalse(value.Fields.ContainsKey("Optional"));
    }

    [TestMethod]
    public void CaptureWithMetricsSharesTheGlobalNodeBudgetAcrossLargeRoots()
    {
        var descriptor = IntArray();
        var first = Definition(descriptor, StructuralInputLimits.Default);
        var second = new ScriptParameterDefinition(ScriptParameterContract.CreateStructural(
            "second",
            "int[]",
            descriptor,
            hasDefaultValue: false,
            defaultValue: null));
        var firstValues = Enumerable.Range(0, 50_000).ToArray();
        var secondValues = Enumerable.Range(50_000, 50_000).ToArray();

        var exception = Assert.ThrowsExactly<ScriptParameterBindingException>(() =>
            StructuralParameterSnapshotter.CaptureWithMetrics(
                new[] { first, second },
                new Dictionary<string, object?>
                {
                    ["value"] = firstValues,
                    ["second"] = secondValues
                },
                CancellationToken.None));

        StringAssert.Contains(exception.InnerException?.Message ?? exception.Message, "maximum value nodes");
    }

    [TestMethod]
    public void ConstantDefaultsAreMeasuredBeforeTheirStorageIsMaterialized()
    {
        var descriptor = StructuralTypeDescriptor.Collection(
            typeof(int[]),
            StructuralTypeDescriptor.Scalar(typeof(int)));
        var definition = new ScriptParameterDefinition(ScriptParameterContract.CreateStructural(
            "value",
            "int[]",
            descriptor,
            hasDefaultValue: true,
            defaultValue: StructuralValue.FromCollection([
                StructuralValue.FromScalar(1),
                StructuralValue.FromScalar(2)
            ])) with { Limits = new StructuralInputLimits(4, 2, 64) });

        var exception = Assert.ThrowsExactly<ScriptParameterBindingException>(() =>
            StructuralParameterSnapshotter.CaptureWithMetrics(
                new[] { definition },
                new Dictionary<string, object?>(),
                CancellationToken.None));

        StringAssert.Contains(exception.InnerException?.Message ?? exception.Message, "configured resource limit");
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

    [TestMethod]
    public void TypedCollectionCountUsesTheDeclaredIndexedContract()
    {
        Assert.AreEqual(
            3,
            StructuralParameterCaptureRuntime.GetCollectionCount<int>(
                new List<int> { 1, 2, 3 },
                "$.values"));

        var negative = Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralParameterCaptureRuntime.GetCollectionCount<int>(
                new NegativeCountList(),
                "$.values"));
        StringAssert.Contains(negative.Message, "negative");

        var rank = Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralParameterCaptureRuntime.GetCollectionCount<int>(
                new int[1, 1],
                "$.values"));
        StringAssert.Contains(rank.Message, "one-dimensional");

        var mismatch = Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralParameterCaptureRuntime.GetCollectionCount<long>(
                new[] { 1, 2 },
                "$.values"));
        StringAssert.Contains(mismatch.Message, "expected indexed element contract");
    }

    [TestMethod]
    public void DirectRecordReadersPreserveCaseInsensitivePresenceAndDuplicateChecks()
    {
        var value = new Dictionary<string, object?>
        {
            ["Name"] = "Ada"
        };

        StructuralParameterCaptureRuntime.ValidateRecord(value, ["Name"], "$.record");
        Assert.IsTrue(StructuralParameterCaptureRuntime.TryGetRecordField(value, "name", out var field));
        Assert.AreEqual("Ada", field);

        var duplicate = new DuplicateReadOnlyDictionary();
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralParameterCaptureRuntime.ValidateRecord(duplicate, ["Value"], "$.record"));
        StringAssert.Contains(exception.Message, "duplicate");
    }

    [TestMethod]
    public void ExecutionCaptureRejectsStructuralTargetsWithoutGeneratedProvider()
    {
        var definition = Definition(
            StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int))),
            StructuralInputLimits.Default);

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralParameterSnapshotter.CaptureForExecution(
                new[] { definition },
                new Dictionary<string, object?> { ["value"] = new[] { 1, 2 } },
                CancellationToken.None,
                generatedProvider: null));

        StringAssert.Contains(exception.Message, nameof(IStructuralParameterSnapshotProvider));
    }

    private static IReadOnlyDictionary<string, object?> Capture(ScriptParameterDefinition definition, object? value)
    {
        return StructuralParameterSnapshotter.Capture(
            new[] { definition },
            new Dictionary<string, object?> { ["value"] = value },
            CancellationToken.None);
    }

    private static ScriptParameterDefinition Definition(StructuralTypeDescriptor descriptor, StructuralInputLimits limits)
    {
        var contract = ScriptParameterContract.CreateStructural(
            "value",
            descriptor.ToCanonicalSql(),
            descriptor,
            hasDefaultValue: false,
            defaultValue: null) with { Limits = limits };
        return new ScriptParameterDefinition(contract);
    }

    private static StructuralTypeDescriptor IntArray() => StructuralTypeDescriptor.Collection(
        typeof(int[]),
        StructuralTypeDescriptor.Scalar(typeof(int)));

    private static StructuralTypeDescriptor Record(params StructuralFieldDescriptor[] fields) =>
        StructuralTypeDescriptor.Record(typeof(StructuralValue), fields);

    private sealed class NegativeCountList : IReadOnlyList<int>
    {
        public int Count => -1;
        public int this[int index] => throw new InvalidOperationException("index should not be read");
        public IEnumerator<int> GetEnumerator() => new List<int>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ThrowingItemList : IReadOnlyList<int>
    {
        public int Count => 2;
        public int this[int index] => throw new InvalidOperationException("changing collection");
        public IEnumerator<int> GetEnumerator() => new List<int>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CancellingList(CancellationTokenSource cancellation, int value) : IReadOnlyList<int>
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

        public IEnumerator<int> GetEnumerator() => new List<int> { value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DuplicateReadOnlyDictionary : IReadOnlyDictionary<string, object?>
    {
        public int Count => 2;
        public IEnumerable<string> Keys => new[] { "Value", "value" };
        public IEnumerable<object?> Values => new object?[] { 1, 2 };
        public object? this[string key] => 1;
        public bool ContainsKey(string key) => true;
        public bool TryGetValue(string key, out object? value)
        {
            value = 1;
            return true;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return new KeyValuePair<string, object?>("Value", 1);
            yield return new KeyValuePair<string, object?>("value", 2);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
