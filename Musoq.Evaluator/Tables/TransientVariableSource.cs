using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables;

/// <summary>
///     Represents a transient variable source used for transition schemas during query execution.
///     This source acts as a placeholder and typically returns empty rows.
/// </summary>
internal class TransientVariableSource : RowSource
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TransientVariableSource" /> class.
    /// </summary>
    /// <param name="name">The name of the transient variable source.</param>
    public TransientVariableSource(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets the name of this transient variable source.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the rows from this source. Returns an empty enumerable as this is a placeholder source.
    /// </summary>
    public override IEnumerable<IObjectResolver> Rows => Enumerable.Empty<IObjectResolver>();
}
