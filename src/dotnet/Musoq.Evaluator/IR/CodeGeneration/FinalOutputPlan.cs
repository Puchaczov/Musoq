using System.Collections.Frozen;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal enum FinalOutputRowRepresentation
{
    SelectShape,
    SelectValueShape,
    GeneratedRow,
    OwnedBufferRow
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
    public OwnedTerminalBuffer? OwnedBuffer { get; init; }
    public bool UsesGeneratedRow => RowRepresentation == FinalOutputRowRepresentation.GeneratedRow;
    public bool ReturnsOwnedRows => RowRepresentation == FinalOutputRowRepresentation.OwnedBufferRow;
    public string RowTypeName => OwnedBuffer?.CarrierTypeName ?? (UsesGeneratedRow ? Result.RowTypeName : Result.ShapeTypeName);
    public string TableRowTypeName => ReturnsOwnedRows ? RowTypeName : Result.RowTypeName;
}

internal sealed record OwnedTerminalBuffer(
    string SourceName,
    string CarrierTypeName,
    string? SequenceField,
    bool SupportsResultRows);
