namespace Musoq.Playground;

public static class ExpensiveCteCounter
{
    private static int _counter;

    public static int Increment()
    {
        return Interlocked.Increment(ref _counter);
    }

    public static void Reset()
    {
        _counter = 0;
    }

    public static int GetCount()
    {
        return _counter;
    }
}
