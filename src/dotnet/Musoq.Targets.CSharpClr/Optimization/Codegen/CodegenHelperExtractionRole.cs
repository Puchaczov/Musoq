namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal enum CodegenHelperExtractionRole
{
    StoredTableBuild,
    HashJoinBuild,
    HashJoinProbe,
    KeySetBuild,
    KeySetProbe,
    WindowAppendRows,
    WindowRankingKeyExtraction,
    WindowSortedCopy,
    AggregatePopulate,
    AggregateFinalize
}
