namespace Musoq.Plugins;

/// <summary>
///     Describes how an aggregate result is exposed at query boundaries.
/// </summary>
/// <param name="PublicResultType">Type returned by the aggregate declaration.</param>
/// <param name="UnderlyingResultType">Value type with nullable wrapper removed, or the public type for reference types.</param>
/// <param name="EmptyResultBehavior">Result behavior when no input value qualifies for the aggregate.</param>
public sealed record AggregateResultDescriptor(
    Type PublicResultType,
    Type UnderlyingResultType,
    AggregateEmptyResultBehavior EmptyResultBehavior);
