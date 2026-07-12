namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Result of attempting to render an execution query method. Carries either the
/// rendered method or the reason the query shape is unsupported, replacing the
/// previous nullable-return-plus-<c>out</c>-parameter pattern.
/// </summary>
public readonly record struct ExecutionQueryRenderOutcome
{
    private ExecutionQueryRenderOutcome(QueryMethodRenderResult? method, string? unsupportedReason)
    {
        Method = method;
        UnsupportedReason = unsupportedReason;
    }

    public QueryMethodRenderResult? Method { get; }

    public string? UnsupportedReason { get; }

    public bool IsSupported => Method.HasValue;

    public static ExecutionQueryRenderOutcome Rendered(QueryMethodRenderResult method)
    {
        return new ExecutionQueryRenderOutcome(method, null);
    }

    public static ExecutionQueryRenderOutcome Unsupported(string? reason)
    {
        return new ExecutionQueryRenderOutcome(null, reason);
    }
}
