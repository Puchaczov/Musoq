using Musoq.Schema.DataSources;

namespace Musoq.Schema;

/// <summary>
/// Optional schema contract for materializing rows into a compiled query's private CLR type.
/// </summary>
public interface IQueryScopedRowSourceSchema
{
    RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>;
}
