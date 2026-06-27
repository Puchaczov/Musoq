namespace Musoq.Schema.Optimization;

public sealed record SourceIdentity(
    string SchemaName,
    string MethodName,
    string SourceContextId,
    string Alias)
{
    public static SourceIdentity Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty);
}
