using System.Threading;

namespace Musoq.Plugins;

/// <summary>Identifies the relational peer metadata needed by a registered ranking kernel.</summary>
public enum WindowRankingKind
{
    /// <summary>No ranking context is requested.</summary>
    None,
    /// <summary>Ordinal row position.</summary>
    RowNumber,
    /// <summary>Ordinal position of the first peer.</summary>
    Rank,
    /// <summary>Ordinal peer group number.</summary>
    DenseRank,
    /// <summary>Relative rank within the partition.</summary>
    PercentRank,
    /// <summary>Cumulative fraction through the last peer.</summary>
    CumeDist
}

/// <summary>Concrete partition position and size, plus peer bounds and dense rank when requested by the declaration.</summary>
public readonly record struct WindowRankingContext(
    int Position, int PeerStart, int PeerEnd, long DenseRank, int PartitionCount,
    CancellationToken CancellationToken = default);
