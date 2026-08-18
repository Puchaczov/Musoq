namespace Musoq.Schema;

/// <summary>
/// Materializes a query-scoped row from a source-specific field reader.
/// </summary>
/// <typeparam name="TRow">The generated query row type.</typeparam>
public interface IQueryRowMaterializer<TRow>
{
    static abstract TRow Materialize<TReader>(scoped ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct;
}
