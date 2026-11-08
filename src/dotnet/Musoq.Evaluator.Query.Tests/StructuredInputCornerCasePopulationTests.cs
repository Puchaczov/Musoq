using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema.Managers;
using Musoq.Evaluator.Visitors;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Deterministic binding/type/default population for structural inputs.
/// The fixed seed and population size make any failure reproducible and reducible.
/// </summary>
[TestClass]
public sealed class StructuredInputCornerCasePopulationTests
{
    private const int CaseCount = 512;
    private const int Seed = 0xB17D_20;
    private const int MinimumCoverageCells = 12;
    private const int MatrixWidth = 16;

    private static readonly string[] RequiredMatrixCells =
    [
        "scalar|int",
        "scalar|decimal",
        "scalar|char",
        "record|nullable-field",
        "record|reordered-casing",
        "record|explicit-null",
        "record|nested",
        "collection|int-array",
        "collection|nullable-array",
        "collection|nested-array",
        "collection|empty-typed",
        "collection|all-null-typed",
        "inference|record-missing",
        "inference|numeric-common",
        "declaration|let",
        "declaration|param",
        "overload|exact-widening",
        "collection|receiver-whitelist",
        "near-miss|unknown-field"
    ];

    [TestMethod]
    public void BindingPopulation_ShouldPreservePresenceDefaultsNullsAndTypedConstruction()
    {
        var random = new Random(Seed);
        var receiver = StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput));
        var expectedType = receiver.ToCanonicalSql();
        var normalizedHashes = new HashSet<string>(StringComparer.Ordinal);
        var coverageCells = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < CaseCount; index++)
        {
            var idName = index % 2 == 0 ? "Id" : "id";
            var patternName = index % 3 == 0 ? "PATTERN" : "Pattern";
            var fields = new List<KeyValuePair<string, StructuralValue>>
            {
                new(patternName, StructuralValue.FromScalar($"pattern-{index}")),
                new(idName, StructuralValue.FromScalar($"id-{index}"))
            };

            var modeWasSupplied = index % 4 != 0;
            string? expectedMode;
            if (modeWasSupplied)
            {
                expectedMode = index % 5 == 0 ? null : $"mode-{random.Next(0, 10_000)}";
                fields.Add(new KeyValuePair<string, StructuralValue>(
                    index % 2 == 0 ? "Mode" : "mode",
                    StructuralValue.FromScalar(expectedMode)));
            }
            else
            {
                expectedMode = "literal";
            }

            if (index % 7 == 0)
                fields.Reverse();

            var raw = StructuralValue.FromRecord(fields);
            Assert.IsTrue(
                normalizedHashes.Add(HashStructuralValue(raw)),
                $"Binding case {index} duplicated its normalized input hash.");
            coverageCells.Add($"record|{index % 2}|{index % 3}|{modeWasSupplied}|{expectedMode is null}");
            Assert.IsTrue(
                StructuralValueBinder.TryNormalize(raw, receiver, $"case{index}", out var normalized, out var error),
                $"Case {index} failed normalization as {expectedType}: {error}");

            var value = ReadReceiver((StructuralValue)normalized!);
            Assert.AreEqual($"id-{index}", value.Id, $"Case {index} ID");
            Assert.AreEqual($"pattern-{index}", value.Pattern, $"Case {index} pattern");
            Assert.AreEqual(expectedMode, value.Mode, $"Case {index} mode");

            var secondFields = new List<KeyValuePair<string, StructuralValue>>
            {
                new("ID", StructuralValue.FromScalar($"other-{index}")),
                new("Pattern", StructuralValue.FromScalar($"other-pattern-{index}"))
            };
            if (modeWasSupplied && expectedMode == null)
                secondFields.Add(new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("fallback")));

            var inferredInput = StructuralValue.FromCollection(
            [
                raw,
                StructuralValue.FromRecord(secondFields)
            ]);
            Assert.IsTrue(
                StructuralValueInference.TryInfer(inferredInput, out var inferred, out var inferenceError),
                $"Case {index} inference failed: {inferenceError}");
            Assert.AreEqual(StructuralTypeKind.Collection, inferred.Kind);
            Assert.AreEqual(StructuralTypeKind.Record, inferred.ElementType!.Kind);
            Assert.IsTrue(inferred.ToCanonicalSql().EndsWith("[]", StringComparison.Ordinal), $"Case {index} inferred collection type: {inferred.ToCanonicalSql()}");
            var inferredMode = inferred.ElementType.Fields.FirstOrDefault(field =>
                string.Equals(field.Name, "Mode", StringComparison.OrdinalIgnoreCase));
            if (modeWasSupplied)
            {
                Assert.IsNotNull(inferredMode, $"Case {index} should retain the supplied Mode field.");
                Assert.AreEqual(expectedMode == null, inferredMode!.Required, $"Case {index} Mode presence metadata.");
            }
            else
            {
                Assert.IsNull(inferredMode, $"Case {index} should not invent an omitted Mode field.");
            }

            if (index % 32 == 0)
            {
                Assert.IsFalse(
                    StructuralValueInference.TryInfer(StructuralValue.FromCollection([]), out _, out _),
                    $"Case {index} empty inference unexpectedly succeeded.");
                Assert.IsFalse(
                    StructuralValueInference.TryInfer(
                        StructuralValue.FromCollection([StructuralValue.FromScalar(null)]), out _, out _),
                    $"Case {index} all-null inference unexpectedly succeeded.");

                var typedEmptyDescriptor = StructuralTypeDescriptor.Collection(
                    typeof(StructuralValue[]),
                    StructuralTypeDescriptor.Scalar(typeof(int)));
                Assert.IsTrue(
                    StructuralValueBinder.TryNormalize(
                        StructuralValue.FromCollection([]), typedEmptyDescriptor, $"empty{index}", out var empty, out var emptyError),
                    $"Case {index} typed empty binding failed: {emptyError}");
                Assert.IsTrue(empty is Array typedEmptyArray && typedEmptyArray.Length == 0, $"Case {index} typed empty binding should retain an empty collection.");
            }
        }

        Assert.AreEqual(CaseCount, normalizedHashes.Count, "Every binding case must have a distinct normalized input hash.");
        Assert.IsGreaterThanOrEqualTo(MinimumCoverageCells, coverageCells.Count, "Binding population coverage cells are too narrow.");
    }

    [TestMethod]
    public void BindingMetamorphicCases_ShouldTreatFieldOrderAndCasingAsEquivalent()
    {
        var receiver = StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput));
        var first = StructuralValue.FromRecord(
        [
            new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("todo")),
            new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO"))
        ]);
        var reordered = StructuralValue.FromRecord(
        [
            new KeyValuePair<string, StructuralValue>("pattern", StructuralValue.FromScalar("TODO")),
            new KeyValuePair<string, StructuralValue>("id", StructuralValue.FromScalar("todo"))
        ]);

        Assert.IsTrue(StructuralValueBinder.TryNormalize(first, receiver, "first", out var firstNormalized, out var firstError), firstError);
        Assert.IsTrue(StructuralValueBinder.TryNormalize(reordered, receiver, "reordered", out var reorderedNormalized, out var reorderedError), reorderedError);
        Assert.AreEqual(
            ReadReceiver((StructuralValue)firstNormalized!),
            ReadReceiver((StructuralValue)reorderedNormalized!));

        var second = StructuralValue.FromRecord(
        [
            new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("fixme")),
            new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("FIXME"))
        ]);
        var reorderedArray = StructuralValue.FromCollection([second, first]);
        var original = StructuralValue.FromCollection([first, second]);
        var collection = StructuralTypeDescriptor.Collection(typeof(StructuralValue[]), receiver);
        Assert.IsTrue(StructuralValueBinder.TryNormalize(original, collection, "original", out var originalNormalized, out var originalError), originalError);
        Assert.IsTrue(StructuralValueBinder.TryNormalize(reorderedArray, collection, "reordered-array", out var reorderedArrayNormalized, out var reorderedArrayError), reorderedArrayError);
        var normalizedOriginalArray = (Array)originalNormalized!;
        var normalizedReorderedArray = (Array)reorderedArrayNormalized!;
        Assert.AreEqual(normalizedOriginalArray.Length, normalizedReorderedArray.Length);
        Assert.AreNotEqual(
            ReadReceiver((StructuralValue)normalizedOriginalArray.GetValue(0)!).Id,
            ReadReceiver((StructuralValue)normalizedReorderedArray.GetValue(0)!).Id,
            "Array order is observable and must not be treated as a metamorphic equivalent.");
    }

    [TestMethod]
    public void BindingPopulation_ShouldCoverIndependentShapeMatrixAndNearMisses()
    {
        var random = new Random(Seed ^ 0x31);
        var normalizedHashes = new HashSet<string>(StringComparer.Ordinal);
        var coverageCells = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < CaseCount; index++)
        {
            var caseKind = index % MatrixWidth;
            var id = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var raw = CreateMatrixValue(caseKind, id, random);
            Assert.IsTrue(
                normalizedHashes.Add(HashBindingCase(raw, caseKind, id)),
                $"Binding matrix case {index} duplicated a normalized structural value.");

            switch (caseKind)
            {
                case 0:
                    AssertNormalized(raw, StructuralTypeDescriptor.Scalar(typeof(int)), index, static value => value is int);
                    coverageCells.Add("scalar|int");
                    break;
                case 1:
                    AssertNormalized(raw, StructuralTypeDescriptor.Scalar(typeof(decimal)), index, static value => value is decimal);
                    coverageCells.Add("scalar|decimal");
                    break;
                case 2:
                    AssertNormalized(raw, StructuralTypeDescriptor.Scalar(typeof(char)), index, static value => value is char);
                    coverageCells.Add("scalar|char");
                    break;
                case 3:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(NullableInput)), index, static value =>
                        value is StructuralValue record &&
                        record.Fields["Value"].Scalar is null);
                    coverageCells.Add("record|nullable-field");
                    break;
                case 4:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput)), index, static value =>
                        value is StructuralValue record &&
                        record.Fields["Id"].Scalar is string id && id.StartsWith("id-", StringComparison.Ordinal) &&
                        record.Fields["Pattern"].Scalar is string pattern && pattern.StartsWith("pattern-", StringComparison.Ordinal));
                    coverageCells.Add("record|reordered-casing");
                    break;
                case 5:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput)), index, static value =>
                        value is StructuralValue record &&
                        record.Fields.ContainsKey("Mode") &&
                        record.Fields["Mode"].Scalar is null);
                    coverageCells.Add("record|explicit-null");
                    break;
                case 6:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(OptionsInput)), index, static value =>
                        value is StructuralValue record &&
                        record.Fields["Codes"].Elements.Count == 2 &&
                        record.Fields["Window"].Fields["Before"].Scalar is int);
                    coverageCells.Add("record|nested");
                    break;
                case 7:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(int[])), index, static value =>
                        value is Array array && array.Length == 3);
                    coverageCells.Add("collection|int-array");
                    break;
                case 8:
                    AssertNormalized(raw, StructuralTypeDescriptor.FromClrType(typeof(int?[])), index, static value =>
                        value is Array array && array.Length == 2 && array.GetValue(1) is null);
                    coverageCells.Add("collection|nullable-array");
                    break;
                case 9:
                    var nestedDescriptor = StructuralTypeDescriptor.Collection(
                        typeof(int[][]),
                        StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int))));
                    AssertNormalized(raw, nestedDescriptor, index, static value =>
                        value is Array outer && outer.Length == 2 && outer.GetValue(0) is Array);
                    coverageCells.Add("collection|nested-array");
                    break;
                case 10:
                    AssertNormalized(raw, EmptyArrayDescriptor(index), index, static value =>
                        value is Array array && array.Length == 0);
                    coverageCells.Add("collection|empty-typed");
                    break;
                case 11:
                    AssertNormalized(raw, AllNullArrayDescriptor(index), index, static value =>
                        value is Array array && array.Length == 2 && array.GetValue(0) is null && array.GetValue(1) is null);
                    coverageCells.Add("collection|all-null-typed");
                    break;
                case 12:
                    Assert.IsTrue(StructuralValueInference.TryInfer(raw, out var inferredRecord, out var inferenceError), inferenceError);
                    var mode = inferredRecord.ElementType!.Fields.Single(field =>
                        string.Equals(field.Name, "Mode", StringComparison.OrdinalIgnoreCase));
                    Assert.IsFalse(mode.Required, $"Case {index} invented a required Mode field.");
                    Assert.IsFalse(((StructuralValue)raw.Elements[0]).Fields.ContainsKey("Mode"));
                    coverageCells.Add("inference|record-missing");
                    coverageCells.Add("declaration|let");
                    break;
                case 13:
                    Assert.IsTrue(StructuralValueInference.TryInfer(raw, out var inferredNumeric, out var numericError), numericError);
                    Assert.AreEqual(StructuralTypeKind.Collection, inferredNumeric.Kind);
                    Assert.AreEqual(StructuralTypeKind.Scalar, inferredNumeric.ElementType!.Kind);
                    Assert.IsTrue(
                        SchemaConversionClassifier.TryGetCost(typeof(int), inferredNumeric.ElementType.ScalarType!, out _),
                        $"Case {index} did not retain a common numeric target.");
                    coverageCells.Add("inference|numeric-common");
                    coverageCells.Add("declaration|param");
                    break;
                case 14:
                    Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(int), typeof(decimal), out var wideningCost));
                    Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(decimal), typeof(decimal), out var exactCost));
                    Assert.IsTrue(exactCost < wideningCost, $"Case {index} lost exact-over-widening cost ordering.");
                    coverageCells.Add("overload|exact-widening");
                    break;
                default:
                    var receiverTypes = new[] { typeof(int[]), typeof(IReadOnlyList<int>), typeof(IEnumerable<int>) };
                    foreach (var receiverType in receiverTypes)
                    {
                        var receiverDescriptor = StructuralTypeDescriptor.FromClrType(receiverType);
                        AssertNormalized(raw, receiverDescriptor, index, static value => value is Array array && array.Length == 2);
                    }

                    Assert.IsFalse(StructuralCollectionContract.IsSupportedReceiver(typeof(List<int>)));
                    coverageCells.Add("collection|receiver-whitelist");

                    var invalid = StructuralValue.FromRecord([
                        new KeyValuePair<string, StructuralValue>("Unknown", StructuralValue.FromScalar(index))
                    ]);
                    Assert.IsFalse(
                        StructuralValueBinder.TryNormalize(
                            invalid,
                            StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput)),
                            $"near-miss{index}",
                            out _,
                            out var error),
                        $"Case {index} unexpectedly accepted an unknown field: {error}");
                    coverageCells.Add("near-miss|unknown-field");
                    break;
            }
        }

        Assert.AreEqual(CaseCount, normalizedHashes.Count, "Every binding matrix case must have a distinct normalized hash.");
        Assert.IsGreaterThanOrEqualTo(MinimumCoverageCells, coverageCells.Count);
        foreach (var requiredCell in RequiredMatrixCells)
            Assert.IsTrue(coverageCells.Contains(requiredCell), $"Required binding coverage cell '{requiredCell}' was not exercised.");
    }

    private static StructuralValue CreateMatrixValue(int caseKind, string id, Random random)
    {
        return caseKind switch
        {
            0 => StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
            1 => StructuralValue.FromScalar(decimal.Parse($"{id}.5", System.Globalization.CultureInfo.InvariantCulture)),
            2 => StructuralValue.FromScalar((char)(0x400 + int.Parse(id, System.Globalization.CultureInfo.InvariantCulture))),
            3 => StructuralValue.FromRecord([
                new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar($"nullable-{id}")),
                new KeyValuePair<string, StructuralValue>("Value", StructuralValue.FromScalar(null))
            ]),
            4 => StructuralValue.FromRecord([
                new KeyValuePair<string, StructuralValue>("pattern", StructuralValue.FromScalar($"pattern-{id}")),
                new KeyValuePair<string, StructuralValue>("ID", StructuralValue.FromScalar($"id-{id}"))
            ]),
            5 => StructuralValue.FromRecord([
                new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar($"id-{id}")),
                new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar($"pattern-{id}")),
                new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar(null))
            ]),
            6 => StructuralValue.FromRecord([
                new KeyValuePair<string, StructuralValue>("Window", StructuralValue.FromRecord([
                    new KeyValuePair<string, StructuralValue>("After", StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) % 5)),
                    new KeyValuePair<string, StructuralValue>("Before", StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) % 4))
                ])),
                new KeyValuePair<string, StructuralValue>("Codes", StructuralValue.FromCollection([
                    StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                    StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) + 1)
                ])),
                new KeyValuePair<string, StructuralValue>("Enabled", StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) % 2 == 0))
            ]),
            7 => StructuralValue.FromCollection([
                StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                StructuralValue.FromScalar(random.Next(0, 1000)),
                StructuralValue.FromScalar(2)
            ]),
            8 => StructuralValue.FromCollection([
                StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                StructuralValue.FromScalar(null)
            ]),
            9 => StructuralValue.FromCollection([
                StructuralValue.FromCollection([
                    StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                    StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) + 1)
                ]),
                StructuralValue.FromCollection([])
            ]),
            10 => StructuralValue.FromCollection([]),
            11 => StructuralValue.FromCollection([StructuralValue.FromScalar(null), StructuralValue.FromScalar(null)]),
            12 => StructuralValue.FromCollection([
                StructuralValue.FromRecord([
                    new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar($"todo-{id}")),
                    new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO"))
                ]),
                StructuralValue.FromRecord([
                    new KeyValuePair<string, StructuralValue>("ID", StructuralValue.FromScalar($"issue-{id}")),
                    new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("ISSUE-[0-9]+")),
                    new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("regex"))
                ])
            ]),
            13 => StructuralValue.FromCollection([
                StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                StructuralValue.FromScalar((long)int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) + 1)
            ]),
            14 => StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
            _ => StructuralValue.FromCollection([
                StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)),
                StructuralValue.FromScalar(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture) + 1)
            ])
        };
    }

    private static StructuralTypeDescriptor EmptyArrayDescriptor(int index)
    {
        var field = new StructuralFieldDescriptor(
            $"Empty{index}",
            StructuralTypeDescriptor.Scalar(typeof(int)),
            required: true);
        var element = StructuralTypeDescriptor.Record(typeof(StructuralValue), [field]);
        return StructuralTypeDescriptor.Collection(typeof(StructuralValue[]), element);
    }

    private static StructuralTypeDescriptor AllNullArrayDescriptor(int index)
    {
        var field = new StructuralFieldDescriptor(
            $"Null{index}",
            StructuralTypeDescriptor.Scalar(typeof(int)),
            required: true);
        var element = StructuralTypeDescriptor.Record(typeof(StructuralValue), [field], nullable: true);
        return StructuralTypeDescriptor.Collection(typeof(StructuralValue[]), element);
    }

    private static string HashBindingCase(StructuralValue value, int caseKind, string id)
    {
        var context = caseKind switch
        {
            10 => $"empty-record-shape:{id}",
            11 => $"all-null-record-shape:{id}",
            _ => string.Empty
        };
        var text = HashStructuralValue(value) + "|" + context;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void AssertNormalized(
        StructuralValue raw,
        StructuralTypeDescriptor descriptor,
        int index,
        Func<object?, bool> assertion)
    {
        Assert.IsTrue(
            StructuralValueBinder.TryNormalize(raw, descriptor, $"matrix{index}", out var normalized, out var error),
            $"Binding matrix case {index} failed for {descriptor}: {error}");
        Assert.IsTrue(assertion(normalized), $"Binding matrix case {index} produced an unexpected value for {descriptor}.");
    }

    private static ReceiverInput ReadReceiver(StructuralValue value)
    {
        Assert.AreEqual(StructuralTypeKind.Record, value.Kind);
        var id = (string?)value.Fields["Id"].Scalar;
        var pattern = (string?)value.Fields["Pattern"].Scalar;
        var mode = value.Fields.TryGetValue("Mode", out var modeValue)
            ? (string?)modeValue.Scalar
            : "literal";
        return new ReceiverInput(id!, pattern!, mode!);
    }

    private static string HashStructuralValue(StructuralValue value)
    {
        var builder = new StringBuilder();
        AppendStructuralValue(builder, value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AppendStructuralValue(StringBuilder builder, StructuralValue value)
    {
        switch (value.Kind)
        {
            case StructuralTypeKind.Scalar:
                builder.Append("scalar:");
                if (value.Scalar is null)
                {
                    builder.Append("<null>");
                }
                else
                {
                    builder.Append(value.Scalar.GetType().AssemblyQualifiedName).Append(':').Append(value.Scalar);
                }

                break;
            case StructuralTypeKind.Record:
                builder.Append("record{");
                foreach (var field in value.Fields.OrderBy(static field => field.Key, StringComparer.OrdinalIgnoreCase))
                {
                    builder.Append(field.Key.ToUpperInvariant()).Append('=');
                    AppendStructuralValue(builder, field.Value);
                    builder.Append(';');
                }

                builder.Append('}');
                break;
            case StructuralTypeKind.Collection:
                builder.Append("collection[");
                foreach (var element in value.Elements)
                {
                    AppendStructuralValue(builder, element);
                    builder.Append(';');
                }

                builder.Append(']');
                break;
            default:
                throw new AssertFailedException($"Unknown structural kind {value.Kind}.");
        }
    }

    public readonly record struct ReceiverInput(string Id, string Pattern, string Mode = "literal");

    public readonly record struct NullableInput(string Id, int? Value);
}
