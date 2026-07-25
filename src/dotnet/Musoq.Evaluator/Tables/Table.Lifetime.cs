namespace Musoq.Evaluator.Tables;
public partial class Table
{
    private IDisposable? _lifetimeLease;
    public void Dispose() => DetachLifetimeLease()?.Dispose();
    internal bool HasDeferredMaterialization
    {
        get { lock (_guard) return _deferredMaterializer != null; }
    }
    internal bool TryAttachLifetimeLease(IDisposable lease)
    {
        lock (_guard)
        {
            if (_deferredMaterializer == null)
                return false;
            if (_lifetimeLease != null)
                throw new InvalidOperationException("Table already has a lifetime lease.");
            _lifetimeLease = lease;
            return true;
        }
    }
    internal IDisposable? DetachLifetimeLease()
    {
        lock (_guard)
            return System.Threading.Interlocked.Exchange(ref _lifetimeLease, null);
    }
}
