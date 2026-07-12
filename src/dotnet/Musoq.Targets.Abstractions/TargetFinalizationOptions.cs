namespace Musoq.Targets.Abstractions;

internal abstract record TargetFinalizationOptions
{
    public static TargetFinalizationOptions Empty { get; } = new EmptyTargetFinalizationOptions();
}

internal sealed record EmptyTargetFinalizationOptions : TargetFinalizationOptions;
