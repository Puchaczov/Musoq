namespace Musoq.Schema.Optimization;

/// <summary>Describes whether a source row stream may be enumerated again safely.</summary>
public enum RowStreamReplayability
{
    /// <summary>The provider does not make a replayability guarantee.</summary>
    Unknown,
    /// <summary>The source can be enumerated again with equivalent rows.</summary>
    Replayable,
    /// <summary>The rows are already stored and can be consumed repeatedly.</summary>
    Materialized
}
