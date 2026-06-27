using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables;

/// <summary>
///     Represents a transient variable source used for transition schemas during query execution.
///     This source acts as a placeholder and typically returns empty rows.
/// </summary>
internal class TransientVariableSource(string name) : RowSource<object>
{
    /// <summary>
    ///     Gets the name of this transient variable source.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Gets the chunks from this source. Returns an empty enumerable as this is a placeholder source.
    /// </summary>
    public override IEnumerable<IReadOnlyList<object>> Chunks => [];
}
