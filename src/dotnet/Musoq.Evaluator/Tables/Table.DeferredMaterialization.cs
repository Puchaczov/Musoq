namespace Musoq.Evaluator.Tables;

public partial class Table
{
    private Action<Table>? _deferredMaterializer;

    internal void DeferMaterialization(Action<Table> materializer)
    {
        ArgumentNullException.ThrowIfNull(materializer);

        lock (_guard)
        {
            if (_deferredMaterializer != null)
                throw new InvalidOperationException("Table already has deferred materialization.");

            _deferredMaterializer = materializer;
            _hasPendingRows = true;
        }
    }
}
