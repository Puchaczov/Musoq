using System.Threading;

namespace Musoq.Targets.Abstractions;

internal abstract record TargetFinalizationOptions
{
    public virtual CancellationToken CancellationToken => CancellationToken.None;

    public static TargetFinalizationOptions Empty { get; } = new EmptyTargetFinalizationOptions();
}

internal sealed record EmptyTargetFinalizationOptions : TargetFinalizationOptions;
