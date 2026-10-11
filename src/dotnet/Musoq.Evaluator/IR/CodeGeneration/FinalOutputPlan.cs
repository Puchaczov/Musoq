using System.Collections.Frozen;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal enum FinalOutputRowRepresentation
{
    SelectShape,
    SelectValueShape,
    GeneratedRow
}

/// <summary>Independent final-output decisions shared by method emission, class emission and plan printing.</summary>
internal sealed record FinalOutputPlan(
    TableViaRowsResultInfo Result,
    bool IsSupported,
    FinalOutputRowRepresentation RowRepresentation,
    bool BufferFinalRows,
    bool PreserveEnumerationBoundary,
    bool ReuseStoredRows,
    FrozenDictionary<string, string> SourceBuffers,
    FrozenDictionary<string, string> SortedRowBufferSources)
{
    public bool UsesGeneratedRow => RowRepresentation == FinalOutputRowRepresentation.GeneratedRow;
    public string RowTypeName => UsesGeneratedRow ? Result.RowTypeName : Result.ShapeTypeName;
}
