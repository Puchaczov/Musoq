using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Injects <see cref="Group" /> type into query.
/// </summary>
public sealed class InjectQueryStatsAttribute : InjectTypeAttribute
{
    /// <summary>
    ///     Injects <see cref="Group" /> type into query.
    /// </summary>
    public override Type InjectType => typeof(QueryStats);
}
