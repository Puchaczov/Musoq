using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly string[] SubquerySampleFileNames =
    [
        CorrelatedInSubquerySampleFileName,
        CorrelatedNotExistsSubquerySampleFileName,
        CorrelatedScalarAggregateSubquerySampleFileName,
        ScalarSubqueryJoinOnSampleFileName,
        CorrelatedAllSubquerySampleFileName,
        CorrelatedApplyDerivedTableSampleFileName,
        CorrelatedCompositeValueTypeSubquerySampleFileName,
        CorrelatedApplySelectiveDerivedTableSampleFileName
    ];

    [TestMethod]
    [DataRow(CorrelatedInSubquerySampleFileName, "PhysicalHashJoin [LeftSemi]")]
    [DataRow(CorrelatedNotExistsSubquerySampleFileName, "PhysicalHashJoin [LeftAntiSemi]")]
    [DataRow(CorrelatedScalarAggregateSubquerySampleFileName, "PhysicalHashJoin [LeftOuter]")]
    [DataRow(ScalarSubqueryJoinOnSampleFileName, "PhysicalHashJoin [Inner]")]
    [DataRow(CorrelatedAllSubquerySampleFileName, "PhysicalHashJoin [LeftAntiSemi]")]
    [DataRow(CorrelatedApplyDerivedTableSampleFileName, "PhysicalHashJoin [Inner]")]
    [DataRow(CorrelatedCompositeValueTypeSubquerySampleFileName, "PhysicalHashJoin [LeftSemi]")]
    [DataRow(CorrelatedApplySelectiveDerivedTableSampleFileName, "PhysicalHashJoin [Inner]")]
    public void SubquerySample_WhenCompiledForInspection_ShouldUseExpectedJoinStrategy(
        string fileName,
        string expectedJoin)
    {
        var result = CompileSampleForInspection(fileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains(expectedJoin, result.PhysicalPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow(CorrelatedInSubquerySampleFileName, "HashSet<ValueTuple<string, string>>")]
    [DataRow(CorrelatedNotExistsSubquerySampleFileName, "HashSet<ValueTuple<int, string>>")]
    [DataRow(CorrelatedAllSubquerySampleFileName, "Dictionary<ValueTuple<int, string>, HashJoinBucket<_sq_1HashPayload0>>")]
    [DataRow(CorrelatedCompositeValueTypeSubquerySampleFileName, "HashSet<ValueTuple<int, string, decimal>>")]
    [DataRow(CorrelatedApplySelectiveDerivedTableSampleFileName, "Dictionary<ValueTuple<string, string>, HashJoinBucket<DHashPayload0>>")]
    public void SubquerySample_WhenCompiledForInspection_ShouldAvoidObjectKeyHotPathPatterns(
        string fileName,
        string expectedTypedKeyContainer)
    {
        var result = CompileSampleForInspection(fileName);
        Assert.Contains(expectedTypedKeyContainer, result.GeneratedCSharpCode);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, NullableHashJoinKeyPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ObjectHashJoinBucketPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ObjectHashJoinKeyLocalPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, WindowCompositeKeyPattern));
    }

    [TestMethod]
    public void SubquerySamples_WhenCompiledForExecution_ShouldRunExecutableQueries()
    {
        foreach (var fileName in SubquerySampleFileNames)
        {
            var table = CompileSampleForExecution(fileName).Run();

            Assert.IsNotNull(table, $"{fileName} should compile and execute.");
        }
    }

    [TestMethod]
    public void SubquerySamples_WhenCheckedIn_ShouldShowDecorrelatedGeneratedCodeShapes()
    {
        var samples = ReadSamples()
            .Where(sample => SubquerySampleFileNames.Contains(sample.FileName, StringComparer.Ordinal))
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        Assert.HasCount(SubquerySampleFileNames.Length, samples);
        Assert.Contains("_sq_1_corr_0", samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", samples[CorrelatedNotExistsSubquerySampleFileName]);
        Assert.Contains(
            "var _sq_1Keys = new HashSet<ValueTuple<string, string>>();",
            samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains(
            "foreach (var bChunk in cte0_bRows)",
            samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains(
            "var _sq_1Keys = new HashSet<ValueTuple<string, string>>();",
            samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains(
            "var key0 = b.City;",
            samples[CorrelatedInSubquerySampleFileName]);
        Assert.Contains(
            "_sq_1Keys.Add(key);",
            samples[CorrelatedInSubquerySampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedInSubquerySampleFileName].Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal),
            "Q138 should use a payload-free key set for equality-only semi joins.");
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedInSubquerySampleFileName]).Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q138 should fuse the generated subquery directly into the key-set build.");
        Assert.Contains(
            "var _sq_1Keys = new HashSet<ValueTuple<int, string>>();",
            samples[CorrelatedNotExistsSubquerySampleFileName]);
        Assert.Contains(
            "foreach (var bChunk in cte0_bRows)",
            samples[CorrelatedNotExistsSubquerySampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedNotExistsSubquerySampleFileName].Contains("private static void BuildSq1Keys(", StringComparison.Ordinal));
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.City));", samples[CorrelatedNotExistsSubquerySampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedNotExistsSubquerySampleFileName]
                .Contains("AppendLeftJoinRows(aRows, _sq_1Keys, result, token);", StringComparison.Ordinal));
        Assert.IsFalse(
            samples[CorrelatedNotExistsSubquerySampleFileName].Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal),
            "Q139 should use a payload-free key set for equality-only anti-semi joins.");
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedNotExistsSubquerySampleFileName]).Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q139 should fuse the generated subquery directly into the key-set build.");
        Assert.Contains("PhysicalHashJoin [LeftOuter]", samples[CorrelatedScalarAggregateSubquerySampleFileName]);
        Assert.Contains("AggregateGroup [Cte0AggregateGroup", samples[CorrelatedScalarAggregateSubquerySampleFileName]);
        Assert.Contains("PhysicalHashJoin [Inner]", samples[ScalarSubqueryJoinOnSampleFileName]);
        Assert.Contains(
            "var a_sq_1Hash = new Dictionary<string, HashJoinBucket<Statement0Row0>>(_cteRowResults.Slot1.Count);",
            samples[ScalarSubqueryJoinOnSampleFileName]);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, b.City));", samples[ScalarSubqueryJoinOnSampleFileName]);
        Assert.IsFalse(
            samples[ScalarSubqueryJoinOnSampleFileName]
                .Contains("AppendHashJoinRows(bRows, a_sq_1Hash, result, token);", StringComparison.Ordinal));
        Assert.Contains("CtePhase [cte2]", samples[ScalarSubqueryJoinOnSampleFileName]);
        Assert.IsFalse(
            samples[ScalarSubqueryJoinOnSampleFileName]
                .Contains("BuildCte2(", StringComparison.Ordinal),
            "Q141 should fuse the final statement1 projection directly into the result table.");
        Assert.IsFalse(
            samples[ScalarSubqueryJoinOnSampleFileName]
                .Contains("StoreTable [statement1 -> _tableResults[2]]", StringComparison.Ordinal),
            "Q141 should not materialize statement1 before immediately projecting it.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[ScalarSubqueryJoinOnSampleFileName])
                .Contains("_tableResults[2]", StringComparison.Ordinal),
            "Q141 generated code should not read or write the fused statement1 slot.");
        Assert.IsFalse(
            samples[ScalarSubqueryJoinOnSampleFileName]
                .Contains("Statement1Row0", StringComparison.Ordinal),
            "Q141 should avoid generating the eliminated statement1 row type.");
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", samples[CorrelatedAllSubquerySampleFileName]);
        Assert.Contains("private readonly struct _sq_1HashPayload0", samples[CorrelatedAllSubquerySampleFileName]);
        Assert.Contains("HashJoinBucket<_sq_1HashPayload0>", samples[CorrelatedAllSubquerySampleFileName]);
        Assert.Contains("_sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(b.Population);", samples[CorrelatedAllSubquerySampleFileName]);
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedAllSubquerySampleFileName]).Contains("public readonly Musoq.Evaluator.Tests.Schema.Basic.BasicEntity __context0;", StringComparison.Ordinal), "Q142 payload should not store unused source row contexts.");
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedAllSubquerySampleFileName]).Contains("public readonly int _sq_1_key;", StringComparison.Ordinal),
            "Q142 payload should not store key-only constant fields.");
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedAllSubquerySampleFileName]).Contains("public readonly string _sq_1_corr_0;", StringComparison.Ordinal),
            "Q142 payload should not store key-only correlation fields.");
        Assert.IsFalse(
            samples[CorrelatedAllSubquerySampleFileName].Contains("private sealed class Cte0Row0 : Row", StringComparison.Ordinal),
            "Q142 should avoid generated Row payloads in the fused hash build.");
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedAllSubquerySampleFileName]).Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q142 should fuse the generated subquery directly into the residual hash build.");
        Assert.Contains("PhysicalHashJoin [Inner]", samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.Contains("_dt_1", samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.Contains(
            "foreach (var bChunk in cte0_bRows)",
            samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.City, d.b_City));", samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Contains("AppendHashJoinRows(aRows, dHash, result, token);", StringComparison.Ordinal));
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName].Contains("private static void BuildDHash(", StringComparison.Ordinal));
        Assert.Contains(
            "var dHash = new Dictionary<string, HashJoinBucket<DHashPayload0>>();",
            samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.Contains(
            "DHashPayload0 d = new DHashPayload0(b.City);",
            samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedApplyDerivedTableSampleFileName]).Contains("public readonly Musoq.Evaluator.Tests.Schema.Basic.BasicEntity __context0;", StringComparison.Ordinal), "Q143 payload should not store unused source row contexts.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedApplyDerivedTableSampleFileName])
                .Contains("public readonly string b_Country;", StringComparison.Ordinal),
            "Q143 payload should not store the key-only Country field.");
        Assert.Contains(
            "string key = b.Country;",
            samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Contains("string key = d.b_Country;", StringComparison.Ordinal),
            "Q143 should compute the hash key before constructing the fused payload.");
        Assert.Contains("private readonly struct DHashPayload0", samples[CorrelatedApplyDerivedTableSampleFileName]);
        Assert.IsFalse(samples[CorrelatedApplyDerivedTableSampleFileName].Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal));
        Assert.IsFalse(samples[CorrelatedApplyDerivedTableSampleFileName].Contains("new Cte0Row0(", StringComparison.Ordinal));
        Assert.IsFalse(samples[CorrelatedApplyDerivedTableSampleFileName].Contains("private sealed class Cte0Row0 : Row", StringComparison.Ordinal));
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Contains("BuildCte0(", StringComparison.Ordinal),
            "Q143 should fuse the single-use derived table directly into the hash build.");
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal),
            "Q143 should not materialize the derived table before building the hash.");
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Contains("private static void AppendHashJoinRows(", StringComparison.Ordinal));
        Assert.IsGreaterThanOrEqualTo(
            2,
            CountOccurrences(
                samples[CorrelatedApplyDerivedTableSampleFileName],
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]"),
            "Q143 hash join helper methods should be aggressively inlined.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedApplyDerivedTableSampleFileName])
                .Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal),
            "Q143 should build the hash from producer rows without generated-row casts.");
        Assert.IsFalse(
            samples[CorrelatedApplyDerivedTableSampleFileName]
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Contains("var dHash = new Dictionary<string, HashJoinBucket<DHashPayload0>>();\n            {\n", StringComparison.Ordinal),
            "Q143 should not wrap the dHash stored-row build loop in an extra block.");
        Assert.Contains("PhysicalHashJoin [Inner]", samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.Contains(
            "CreateHash [dHash: ValueTuple<string, string> -> Row]",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.Contains(
            "foreach (var bChunk in cte0_bRows)",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName].Contains("private static void BuildDHash(", StringComparison.Ordinal));
        Assert.Contains(
            "var dHash = new Dictionary<ValueTuple<string, string>, HashJoinBucket<DHashPayload0>>();",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.Contains(
            "DHashPayload0 d = new DHashPayload0(b.City);",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.IsFalse(ExtractGeneratedCodeSection(samples[CorrelatedApplySelectiveDerivedTableSampleFileName]).Contains("public readonly Musoq.Evaluator.Tests.Schema.Basic.BasicEntity __context0;", StringComparison.Ordinal), "Q145 payload should not store unused source row contexts.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedApplySelectiveDerivedTableSampleFileName])
                .Contains("public readonly string b_Country;", StringComparison.Ordinal),
            "Q145 payload should keep the projected City field but not the key-only Country field.");
        Assert.Contains(
            "string key0 = b.Country;",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.Contains(
            "string key1 = b.City;",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.Contains(
            "ValueTuple<string, string> key = (key0, key1);",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]
                .Contains("var key0 = d.b_Country;", StringComparison.Ordinal),
            "Q145 should compute composite hash key parts before constructing the fused payload.");
        Assert.Contains("private readonly struct DHashPayload0", samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.IsFalse(samples[CorrelatedApplySelectiveDerivedTableSampleFileName].Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal));
        Assert.IsFalse(samples[CorrelatedApplySelectiveDerivedTableSampleFileName].Contains("new Cte0Row0(", StringComparison.Ordinal));
        Assert.IsFalse(samples[CorrelatedApplySelectiveDerivedTableSampleFileName].Contains("private sealed class Cte0Row0 : Row", StringComparison.Ordinal));
        Assert.Contains(
            "foreach (var bChunk in cte0_bRows)",
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedApplySelectiveDerivedTableSampleFileName]
                .Contains("BuildCte0(", StringComparison.Ordinal),
            "Q145 should fuse the single-use selective derived table directly into the hash build.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedApplySelectiveDerivedTableSampleFileName])
                .Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q145 generated code should not read or write the fused derived table slot.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedApplySelectiveDerivedTableSampleFileName])
                .Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal),
            "Q145 should build the selective hash from producer rows without generated-row casts.");
        Assert.Contains("PhysicalHashJoin [LeftSemi]", samples[CorrelatedCompositeValueTypeSubquerySampleFileName]);
        Assert.Contains(
            "HashSet<ValueTuple<int, string, decimal>>",
            samples[CorrelatedCompositeValueTypeSubquerySampleFileName]);
        Assert.IsFalse(
            samples[CorrelatedCompositeValueTypeSubquerySampleFileName].Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal),
            "Q144 should use a payload-free key set for equality-only composite semi joins.");
        Assert.IsFalse(
            ExtractGeneratedCodeSection(samples[CorrelatedCompositeValueTypeSubquerySampleFileName])
                .Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q144 should fuse the generated subquery directly into the key-set build.");
        Assert.Contains("var key1 = a.Country;", samples[CorrelatedCompositeValueTypeSubquerySampleFileName]);

        foreach (var sample in samples.Values)
        {
            Assert.AreEqual(0, CountOccurrences(sample, SmartForEachPattern));
            Assert.AreEqual(0, CountOccurrences(sample, GetColumnValuePattern));
            Assert.AreEqual(0, CountOccurrences(sample, ConvertTableToSourceWithDiscardedContextsPattern));
            Assert.AreEqual(0, CountOccurrences(sample, ContextsAccessPattern));
        }
    }

    [TestMethod]
    public void SubquerySamples_WhenCheckedIn_ShouldAvoidObjectKeyAllocationPatternsForTypedDecorrelatedHashJoins()
    {
        var generatedCode = string.Concat(
            ReadSamples()
                .Where(sample => SubquerySampleFileNames.Contains(sample.FileName, StringComparer.Ordinal))
                .Select(static sample => ExtractGeneratedCodeSection(sample.Content)));

        Assert.AreEqual(0, CountOccurrences(generatedCode, NullableHashJoinKeyPattern));
        Assert.AreEqual(0, CountOccurrences(generatedCode, ObjectHashJoinBucketPattern));
        Assert.AreEqual(0, CountOccurrences(generatedCode, ObjectHashJoinKeyLocalPattern));
        Assert.AreEqual(0, CountOccurrences(generatedCode, WindowCompositeKeyPattern));
    }

    [TestMethod]
    public void SubquerySamples_WhenCheckedIn_ShouldUseSingleLookupHashBuilds()
    {
        var samples = ReadSamples()
            .Where(sample => SubquerySampleFileNames.Contains(sample.FileName, StringComparer.Ordinal))
            .ToArray();

        foreach (var sample in samples)
        {
            var generatedCode = ExtractGeneratedCodeSection(sample.Content);

            var usesPayloadFreeKeySet =
                sample.FileName == InSubqueryBasicSampleFileName ||
                sample.FileName == CorrelatedInSubquerySampleFileName ||
                sample.FileName == CorrelatedNotExistsSubquerySampleFileName ||
                sample.FileName == CorrelatedCompositeValueTypeSubquerySampleFileName;

            if (usesPayloadFreeKeySet)
            {
                Assert.IsTrue(
                    generatedCode.Contains(".Add(key);", StringComparison.Ordinal),
                    $"{sample.FileName} should use payload-free key-set adds.");
                Assert.IsFalse(
                    generatedCode.Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal),
                    $"{sample.FileName} should not allocate row hash buckets for payload-free semi/anti joins.");
            }
            else
            {
                Assert.IsTrue(
                    generatedCode.Contains(HashJoinSingleLookupAddPattern, StringComparison.Ordinal),
                    $"{sample.FileName} should use the single-lookup hash build path.");
            }

            Assert.IsFalse(
                generatedCode.Contains("result.EnsureCapacity(", StringComparison.Ordinal),
                $"{sample.FileName} should not pre-size hash-join result tables from streaming source chunks.");
            Assert.AreEqual(
                0,
                CountOccurrences(generatedCode, HashJoinDoubleLookupAddPattern),
                $"{sample.FileName} should not use Dictionary.Add after a separate TryGetValue in hash build paths.");
            Assert.IsFalse(
                generatedCode.Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal),
                $"{sample.FileName} should use indexed stored-row loops in generated subquery hot paths.");
        }
    }

    private static string ExtractGeneratedCodeSection(string sample)
    {
        var index = sample.IndexOf(GeneratedCodeSectionMarker, StringComparison.Ordinal);

        return index < 0 ? sample : sample[index..];
    }
}
