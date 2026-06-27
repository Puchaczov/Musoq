using System.Collections.Generic;
using System.Linq;

namespace Musoq.Plugins;

/// <summary>
///     Marker input for aggregate kernels that count rows rather than values.
/// </summary>
public readonly record struct AggregateUnit
{
    /// <summary>Singleton marker value.</summary>
    public static readonly AggregateUnit Value;
}
