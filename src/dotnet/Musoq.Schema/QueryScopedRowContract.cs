namespace Musoq.Schema;

/// <summary>
/// Metadata identity for authoritative query-scoped columns. Providers deliver the
/// concrete carrier selected by IQueryRowMaterializer, never instances of this marker.
/// </summary>
public sealed class QueryScopedRowContract;
