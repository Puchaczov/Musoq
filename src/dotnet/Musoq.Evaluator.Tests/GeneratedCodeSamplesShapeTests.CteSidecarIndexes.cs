using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Musoq.Evaluator.Tests;
public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CteSidecarHashJoinSample_WhenCompiledForInspection_ShouldUseHashSidecar()
    {
        var result = CompileSampleForInspection(CteSidecarHashJoinSampleFileName);

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Hash", result.PlanningText);
        Assert.Contains("StoreCteIndex [", result.ExecutionPlanText);
        Assert.Contains("LoadCteIndex [", result.ExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot", result.ExecutionPlanText);
        Assert.Contains("StoreCteIndexCandidate [", result.InitialExecutionPlanText);
        Assert.Contains("LoadCteIndexCandidate [", result.InitialExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot", result.InitialExecutionPlanText);
        Assert.IsFalse(result.OptimizedExecutionPlanText.Contains("StoreCteIndexCandidate", StringComparison.Ordinal));
        Assert.IsFalse(result.OptimizedExecutionPlanText.Contains("LoadCteIndexCandidate", StringComparison.Ordinal));
        Assert.Contains("HashProbe [", result.ExecutionPlanText);
        Assert.Contains("[b.Id] ->", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [iHash", StringComparison.Ordinal));
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeDoesNotUseCteRowResults(result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte0HashPayload0", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CteSidecarKeySetSemiJoinSample_WhenCompiledForInspection_ShouldUseKeySetSidecar()
    {
        var result = CompileSampleForInspection(CteSidecarKeySetSemiJoinSampleFileName);

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> KeySet", result.PlanningText);
        Assert.Contains("StoreCteIndex [", result.ExecutionPlanText);
        Assert.Contains("LoadCteIndex [iKeys <- _cteIndexResults.Slot", result.ExecutionPlanText);
        Assert.Contains("StoreCteIndexCandidate [", result.InitialExecutionPlanText);
        Assert.Contains("LoadCteIndexCandidate [iKeys <- _cteIndexResults.Slot", result.InitialExecutionPlanText);
        Assert.IsFalse(result.OptimizedExecutionPlanText.Contains("StoreCteIndexCandidate", StringComparison.Ordinal));
        Assert.IsFalse(result.OptimizedExecutionPlanText.Contains("LoadCteIndexCandidate", StringComparison.Ordinal));
        Assert.Contains("KeySetProbe [iKeys[b.Id]]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("KeySetAdd [iKeys", StringComparison.Ordinal));
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeDoesNotUseCteRowResults(result.GeneratedCSharpCode);
        Assert.Contains("public HashSet<int> Slot0;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CteSidecarFanoutThreeHashesSample_WhenCompiledForInspection_ShouldUseThreeHashSidecars()
    {
        var result = CompileSampleForInspection(CteSidecarFanoutThreeHashesSampleFileName);

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.AreEqual(3, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        Assert.AreEqual(3, CountOccurrences(result.ExecutionPlanText, "LoadCteIndex ["));
        Assert.AreEqual(3, CountOccurrences(result.ExecutionPlanText, "CreateHash ["));
        Assert.Contains("_cteIndexResults.Slot0 Hash: int]", result.ExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot1 Hash: int]", result.ExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot2 Hash: int]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [bnNHash", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [bncCHash", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [bnccoCoHash", StringComparison.Ordinal));
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeDoesNotUseCteRowResults(result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte0HashPayload0", result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte1HashPayload1", result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte2HashPayload2", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte1HashPayload1>> Slot1;", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte2HashPayload2>> Slot2;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte1Row0> Slot1;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte2Row0> Slot2;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte1Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte2Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.Contains(
            "__musoqFinalShapeRows.Add(new ResultShape0(b.Name, n.Name, c.City, co.Country))",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ContextMaterializer.Merge", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("TryGetNonEnumeratedCount(out var cte0HashSidecar0IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("TryGetNonEnumeratedCount(out var cte1HashSidecar1IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("TryGetNonEnumeratedCount(out var cte2HashSidecar2IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement1 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Statement0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Statement1Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot3", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot4", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CteSidecarFanoutThreeHashesSample_WhenSidecarParallelizationIsEnabled_ShouldBuildIndependentCtesTogether()
    {
        var result = CompileSampleForInspection(
            CteSidecarFanoutThreeHashesSampleFileName,
            CreateParallelCteSidecarOptions());

        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Candidate", result.PlanningText);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 3, maxDegree 3]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [names -> __parallelCteLevel0Task0Result]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [cities -> __parallelCteLevel0Task1Result]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [countries -> __parallelCteLevel0Task2Result]", result.ExecutionPlanText);
        Assert.Contains("ParallelMerge", result.ExecutionPlanText);
        Assert.Contains("Parallel.Invoke(new ParallelOptions", result.GeneratedCSharpCode);
        Assert.Contains("private static object BuildCteLevel0Task0(", result.GeneratedCSharpCode);
        Assert.Contains("private static object BuildCteLevel0Task1(", result.GeneratedCSharpCode);
        Assert.Contains("private static object BuildCteLevel0Task2(", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot0 = __parallelCteLevel0Task0Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot1 = __parallelCteLevel0Task1Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot2 = __parallelCteLevel0Task2Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeDoesNotUseCteRowResults(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CteSidecarStagedGraphMixedSample_WhenCompiledForInspection_ShouldUseHashAndKeySetSidecars()
    {
        var result = CompileSampleForInspection(CteSidecarStagedGraphMixedSampleFileName);

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.AreEqual(3, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        Assert.AreEqual(3, CountOccurrences(result.ExecutionPlanText, "LoadCteIndex ["));
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "CreateHash ["));
        Assert.AreEqual(1, CountOccurrences(result.ExecutionPlanText, "CreateKeySet ["));
        Assert.Contains("_cteIndexResults.Slot0 Hash: int]", result.ExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot1 Hash: int]", result.ExecutionPlanText);
        Assert.Contains("_cteIndexResults.Slot2 KeySet: int]", result.ExecutionPlanText);
        Assert.Contains("KeySetProbe [eKeys[b.Id]]", result.ExecutionPlanText);
        AssertTextBefore("KeySetProbe [eKeys[b.Id]]", "HashProbe [bnNHash[b.Id]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [bnNHash", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("HashAdd [bncCHash", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("KeySetAdd [eKeys", StringComparison.Ordinal));
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeDoesNotUseCteRowResults(result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte1HashPayload0", result.GeneratedCSharpCode);
        Assert.Contains("private readonly struct Cte2HashPayload1", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte1HashPayload0>> Slot0;", result.GeneratedCSharpCode);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte2HashPayload1>> Slot1;", result.GeneratedCSharpCode);
        Assert.Contains("public HashSet<int> Slot2;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte1Row0> Slot1;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte2Row0> Slot2;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("public List<Cte3Row0> Slot3;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.Contains("CtePhase [cte0]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateTable [cte0:", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte0 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.Contains("ForEach [ko3iko in cte0_ko3ikoRows]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [raw in CastGeneratedRows<Cte0Row0>", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte1HashSidecar0IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte2HashSidecar1IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte3KeySetSidecar2IdCapacity)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.Contains("FusedCteProducer [cte1 -> sidecar-only, cte2 -> sidecar-only, cte3 -> sidecar-only]", result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new List<Cte1Row0>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new Dictionary<int, HashJoinBucket<Cte1HashPayload0>>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new HashSet<int>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte1Row0> BuildCte1(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte2Row0> BuildCte2(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte3Row0> BuildCte3(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte1Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte2Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot1", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot2", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot3", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.Contains(
            "__musoqFinalShapeRows.Add(new ResultShape0(b.Id, n.Name, c.City))",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ContextMaterializer.Merge", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ko3iko.Country", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte3Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte4_statement0 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte4 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte4Statement0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte4Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot4", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot5", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CteSidecarSamples_WhenCheckedIn_ShouldShowStoredSidecarIndexes()
    {
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is CteSidecarHashJoinSampleFileName or CteSidecarKeySetSemiJoinSampleFileName)
            .ToArray();

        Assert.HasCount(2, samples);

        foreach (var sample in samples)
        {
            Assert.Contains("StoreCteIndex [", sample.Content);
            Assert.Contains("LoadCteIndex [", sample.Content);
            AssertSampleUsesTypedCteIndexResults(sample.Content);
            AssertSampleDoesNotUseCteRowResults(sample.Content);
            Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
        }

        var hashSample = samples.Single(static sample => sample.FileName == CteSidecarHashJoinSampleFileName);
        Assert.Contains("LoadCteIndex [iHash <- _cteIndexResults.Slot0 Hash: int]", hashSample.Content);
        Assert.Contains("private readonly struct Cte0HashPayload0", hashSample.Content);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;", hashSample.Content);
        Assert.IsFalse(hashSample.Content.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), hashSample.Content);
        Assert.IsFalse(hashSample.Content.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), hashSample.Content);
        Assert.IsFalse(hashSample.Content.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), hashSample.Content);
        Assert.Contains("var iHash = _cteIndexResults.Slot0;", hashSample.Content);
        Assert.Contains("if (iHash.TryGetValue(key, out var iHashMatches))", hashSample.Content);
        Assert.IsFalse(hashSample.Content.Contains("BuildIHash(", StringComparison.Ordinal));
        Assert.IsFalse(hashSample.Content.Contains("HashAdd [iHash", StringComparison.Ordinal));
        Assert.IsFalse(hashSample.Content.Contains("var iHash = new Dictionary<int, HashJoinBucket<Cte0HashPayload0>>", StringComparison.Ordinal));

        var keySetSample = samples.Single(static sample => sample.FileName == CteSidecarKeySetSemiJoinSampleFileName);
        Assert.Contains("LoadCteIndex [iKeys <- _cteIndexResults.Slot0 KeySet: int]", keySetSample.Content);
        Assert.Contains("public HashSet<int> Slot0;", keySetSample.Content);
        Assert.IsFalse(keySetSample.Content.Contains("Cte0Row0", StringComparison.Ordinal), keySetSample.Content);
        Assert.Contains("var iKeys = _cteIndexResults.Slot0;", keySetSample.Content);
        Assert.Contains("if (iKeys.Contains(key))", keySetSample.Content);
        Assert.IsFalse(keySetSample.Content.Contains("BuildIKeys(", StringComparison.Ordinal));
        Assert.IsFalse(keySetSample.Content.Contains("KeySetAdd [iKeys", StringComparison.Ordinal));
        Assert.IsFalse(keySetSample.Content.Contains("var iKeys = new HashSet<int>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CteSidecarComplexSamples_WhenCheckedIn_ShouldShowMultipleStoredSidecarIndexes()
    {
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is CteSidecarFanoutThreeHashesSampleFileName or CteSidecarStagedGraphMixedSampleFileName)
            .ToDictionary(static sample => sample.FileName, StringComparer.Ordinal);

        Assert.HasCount(2, samples);

        var fanout = samples[CteSidecarFanoutThreeHashesSampleFileName].Content;
        AssertSampleUsesTypedCteIndexResults(fanout);
        AssertSampleDoesNotUseCteRowResults(fanout);
        Assert.AreEqual(3, CountOccurrences(fanout, "StoreCteIndex ["));
        Assert.AreEqual(3, CountOccurrences(fanout, "LoadCteIndex ["));
        Assert.Contains("private readonly struct Cte0HashPayload0", fanout);
        Assert.Contains("private readonly struct Cte1HashPayload1", fanout);
        Assert.Contains("private readonly struct Cte2HashPayload2", fanout);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;", fanout);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte1HashPayload1>> Slot1;", fanout);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte2HashPayload2>> Slot2;", fanout);
        Assert.IsFalse(fanout.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("public List<Cte1Row0> Slot1;", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("public List<Cte2Row0> Slot2;", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private static List<Cte1Row0> BuildCte1(", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private static List<Cte2Row0> BuildCte2(", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private sealed class Cte1Row0", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("private sealed class Cte2Row0", StringComparison.Ordinal), fanout);
        Assert.Contains("var bnNHash = _cteIndexResults.Slot0;", fanout);
        Assert.Contains("var bncCHash = _cteIndexResults.Slot1;", fanout);
        Assert.Contains("var bnccoCoHash = _cteIndexResults.Slot2;", fanout);
        Assert.IsFalse(fanout.Contains("TryGetNonEnumeratedCount(out var cte0HashSidecar0IdCapacity)", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("TryGetNonEnumeratedCount(out var cte1HashSidecar1IdCapacity)", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("TryGetNonEnumeratedCount(out var cte2HashSidecar2IdCapacity)", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("StoreTable [statement0 ->", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("StoreTable [statement1 ->", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("Statement0Row0", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("Statement1Row0", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("_cteRowResults.Slot3", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("_cteRowResults.Slot4", StringComparison.Ordinal), fanout);
        Assert.Contains(
            "__musoqFinalShapeRows.Add(new ResultShape0(b.Name, n.Name, c.City, co.Country));",
            fanout);
        Assert.IsFalse(fanout.Contains("ContextMaterializer.Merge", StringComparison.Ordinal), fanout);
        Assert.IsFalse(fanout.Contains("BuildStatement0NHash(", StringComparison.Ordinal));
        Assert.IsFalse(fanout.Contains("BuildStatement1CHash(", StringComparison.Ordinal));
        Assert.IsFalse(fanout.Contains("BuildCoHash(", StringComparison.Ordinal));

        var staged = samples[CteSidecarStagedGraphMixedSampleFileName].Content;
        AssertSampleUsesTypedCteIndexResults(staged);
        AssertSampleDoesNotUseCteRowResults(staged);
        Assert.AreEqual(3, CountOccurrences(staged, "StoreCteIndex ["));
        Assert.AreEqual(3, CountOccurrences(staged, "LoadCteIndex ["));
        Assert.Contains("private readonly struct Cte1HashPayload0", staged);
        Assert.Contains("private readonly struct Cte2HashPayload1", staged);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte1HashPayload0>> Slot0;", staged);
        Assert.Contains("public Dictionary<int, HashJoinBucket<Cte2HashPayload1>> Slot1;", staged);
        Assert.Contains("public HashSet<int> Slot2;", staged);
        Assert.IsFalse(staged.Contains("public List<Cte0Row0> Slot0;", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("public List<Cte1Row0> Slot1;", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("public List<Cte2Row0> Slot2;", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("public List<Cte3Row0> Slot3;", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), staged);
        Assert.Contains("FusedCteProducer [cte1 -> sidecar-only, cte2 -> sidecar-only, cte3 -> sidecar-only]", staged);
        Assert.Contains("CtePhase [cte0]", staged);
        Assert.Contains("ForEach [ko3iko in cte0_ko3ikoRows]", staged);
        Assert.IsFalse(staged.Contains("CreateTable [cte0:", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("StoreTable [cte0 ->", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("ForEach [raw in CastGeneratedRows<Cte0Row0>", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private static List<Cte1Row0> BuildCte1(", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private static List<Cte2Row0> BuildCte2(", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private static List<Cte3Row0> BuildCte3(", StringComparison.Ordinal), staged);
        Assert.Contains("var bnNHash = _cteIndexResults.Slot0;", staged);
        Assert.Contains("var bncCHash = _cteIndexResults.Slot1;", staged);
        Assert.Contains("var eKeys = _cteIndexResults.Slot2;", staged);
        AssertTextBefore("if (eKeys.Contains(eKeysKey))", "if (bnNHash.TryGetValue", staged);
        Assert.IsFalse(staged.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte1HashSidecar0IdCapacity)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte2HashSidecar1IdCapacity)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("cte0_ko3ikoRows.TryGetNonEnumeratedCount(out var cte3KeySetSidecar2IdCapacity)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("new List<Cte1Row0>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("new Dictionary<int, HashJoinBucket<Cte1HashPayload0>>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("new HashSet<int>(_cteRowResults.Slot0.Count)", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot1", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot2", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot3", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private sealed class Cte1Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("private sealed class Cte2Row0", StringComparison.Ordinal), staged);
        Assert.Contains(
            "__musoqFinalShapeRows.Add(new ResultShape0(b.Id, n.Name, c.City));",
            staged);
        Assert.IsFalse(staged.Contains("ContextMaterializer.Merge", StringComparison.Ordinal), staged);
        var stagedCode = ExtractGeneratedCodeSection(staged);
        Assert.IsFalse(stagedCode.Contains("ko3iko.Country", StringComparison.Ordinal), staged);
        Assert.IsFalse(stagedCode.Contains("Cte0Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(stagedCode.Contains("Cte3Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("StoreTable [cte4_statement0 ->", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("StoreTable [cte4 ->", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("Cte4Statement0Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("Cte4Row0", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot4", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("_cteRowResults.Slot5", StringComparison.Ordinal), staged);
        Assert.IsFalse(staged.Contains("BuildCte4Statement0NHash(", StringComparison.Ordinal));
        Assert.IsFalse(staged.Contains("BuildCte4CHash(", StringComparison.Ordinal));
        Assert.IsFalse(staged.Contains("BuildEKeys(", StringComparison.Ordinal));

        foreach (var sample in samples.Values)
        {
            Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
            Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
        }
    }

}
