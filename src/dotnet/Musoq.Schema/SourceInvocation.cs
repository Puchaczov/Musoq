using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Schema;

/// <summary>Infers the native argument carrier while retaining the compiled row type.</summary>
public static class SourceInvocation<TRow>
{
    public static RowSource<TRow> Open<TArguments>(ISchema schema, string name,
        SourceExecutionContext context, TArguments arguments)
        where TArguments : struct, ISourceArguments
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);
        return schema.GetRowSource<TRow, TArguments>(name, context, arguments);
    }
}

/// <summary>Infers native arguments for query-scoped materialization.</summary>
public static class QuerySourceInvocation<TRow, TMaterializer>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    public static RowSource<TRow> Open<TArguments>(IQueryScopedRowSourceSchema schema, string name,
        QueryScopedRowSourceRequest request, TArguments arguments)
        where TArguments : struct, ISourceArguments
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(request);
        return schema.GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(name, request, arguments);
    }
}
