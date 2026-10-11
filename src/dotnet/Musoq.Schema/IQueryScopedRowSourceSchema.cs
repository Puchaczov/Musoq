using Musoq.Schema.DataSources;

namespace Musoq.Schema;

/// <summary>
/// Optional schema contract for materializing rows into a compiled query's private CLR type.
/// </summary>
public interface IQueryScopedRowSourceSchema
{
    RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name,
        QueryScopedRowSourceRequest request,
        TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments;
}
