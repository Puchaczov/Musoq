namespace Musoq.Plugins;

/// <summary>
///     Provides the function and type shape requested by the execution planner.
/// </summary>
/// <param name="FunctionName">The SQL-facing window function name.</param>
/// <param name="InputType">The value expression type.</param>
/// <param name="ResultType">The registered window result type.</param>
public sealed record WindowAggregateCapabilityContext(
    string FunctionName,
    Type InputType,
    Type ResultType);
