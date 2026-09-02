using System;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateRecursiveCteSamples()
    {
        return
        [
            Basic(
                "Q187_CteColumnListOrdinary",
                "RecursiveCte",
                @"WITH places (Name, Nation) AS (
                      SELECT City, Country
                      FROM #A.entities()
                  )
                  SELECT Name, Nation
                  FROM places"),
            Recursive("Q188_RecursiveUnionAllCounter"),
            Recursive("Q189_RecursiveUnionAllPredicateTermination"),
            Recursive("Q190_RecursiveEmptyAnchor"),
            Recursive("Q191_RecursiveMultipleRoots"),
            Recursive("Q192_RecursiveUnionFullRowCycle"),
            Recursive("Q193_RecursiveUnionSingleKeyCycle"),
            Recursive("Q194_RecursiveUnionCompositeKey"),
            Recursive("Q195_RecursiveKeyedNonKeyPayload"),
            Recursive("Q196_RecursiveAnchorDuplicates"),
            Recursive("Q197_RecursiveDuplicateEdges"),
            Recursive("Q198_RecursiveInnerJoinEdges"),
            Recursive("Q199_RecursiveCrossJoinFilter"),
            Recursive("Q200_RecursiveCrossApplyNeighbors"),
            Recursive("Q201_RecursiveOuterApplyNeighbors"),
            Recursive("Q202_RecursiveInvariantSourceSnapshot"),
            Recursive("Q203_RecursiveInvariantHashLookup"),
            Recursive("Q204_RecursivePriorValuesCteEdges"),
            Recursive("Q205_RecursivePriorMaterializedCte"),
            Recursive("Q206_RecursiveDependentOrdinaryCte"),
            Recursive("Q207_RecursiveTwoIndependentCtes"),
            Recursive("Q208_RecursiveDependsOnEarlierRecursive"),
            Recursive("Q209_RecursiveUnusedDefinition"),
            Recursive("Q210_RecursiveProjectionPrunedState"),
            Recursive("Q211_RecursiveOuterFilterOrder"),
            Recursive("Q212_RecursiveOuterJoin"),
            Recursive("Q213_RecursiveOuterAggregate"),
            Recursive("Q214_RecursiveOuterWindowAndSet"),
            Recursive("Q215_RecursiveNullableColumns"),
            Recursive("Q216_RecursiveExplicitDecimalCast"),
            Recursive("Q217_RecursiveCaseAndScalarExpressions"),
            Recursive("Q218_RecursiveWidePayload"),
            Recursive("Q219_RecursiveLimitDefaultCodeShape"),
            Recursive("Q220_RecursiveLimitOverrideCodeShape"),
            Recursive("Q221_RecursiveSidecarDisabled"),
            Recursive("Q222_RecursiveCteParallelSiblings"),
            Recursive("Q223_RecursiveUncorrelatedApplySnapshot"),
            Recursive("Q224_RecursiveCompositeInvariantSubplan"),
            Recursive("Q225_RecursiveMutableSourceValueSnapshot"),
            Recursive("Q226_RecursiveSnapshotLimitCodeShape")
        ];
    }

    private static GeneratedCodeSample Recursive(string name)
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName(name);
        var options = string.Equals(name, "Q217_RecursiveCaseAndScalarExpressions", StringComparison.Ordinal)
            ? testCase.CompilationOptions
            : testCase.CompilationOptions.WithStabilityAwareScalarReuse();

        return Basic(name, "RecursiveCte", testCase.Query) with
        {
            CompilationOptions = options,
            CreateSchemaProvider = testCase.CreateSchemaProvider ?? CreateBasicSchemaProvider
        };
    }
}
