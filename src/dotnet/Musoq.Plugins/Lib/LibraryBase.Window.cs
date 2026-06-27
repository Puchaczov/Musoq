using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(WindowDecimalAggregateKernel),
        Name = nameof(Window),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom)]
    public IEnumerable<decimal> Window(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<IEnumerable<decimal>>();
}
