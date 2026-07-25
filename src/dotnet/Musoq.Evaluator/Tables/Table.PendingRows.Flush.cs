using System.Runtime.CompilerServices;
namespace Musoq.Evaluator.Tables;
public partial class Table
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPendingRows()
    {
        if (!_hasPendingRows)
            return;
        IDisposable? lease = null;
        try
        {
            lock (_guard)
            {
                if (!_hasPendingRows)
                    return;
                try
                {
                    var materializer = _deferredMaterializer;
                    _deferredMaterializer = null;
                    materializer?.Invoke(this);
                    while (_pendingRows.TryDequeue(out var row)) base.Rows.Add(row);
                    FlushPendingDirectRows();
                }
                finally
                {
                    _hasPendingRows = false;
                    lease = DetachLifetimeLease();
                }
            }
        }
        finally
        {
            lease?.Dispose();
        }
    }
}
