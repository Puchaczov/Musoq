namespace Musoq.Evaluator.Helpers;

public readonly struct NativeEqualityKey<T>(bool hasValue, T value) where T : notnull
{
    public bool HasValue { get; } = hasValue;
    public T Value { get; } = value;
}
