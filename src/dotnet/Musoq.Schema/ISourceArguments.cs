using System.Runtime.CompilerServices;
using Musoq.Schema.DataSources;

namespace Musoq.Schema;

/// <summary>Reads concrete source-constructor arguments without erased value storage.</summary>
public interface ISourceArguments
{
    int Count { get; }
    T GetValue<T>(int index);
    void Visit<TVisitor>(ref TVisitor visitor, int offset = 0)
        where TVisitor : IColumnValueVisitor, allows ref struct;
}

/// <summary>The empty native source-argument carrier.</summary>
public readonly struct EmptySourceArguments : ISourceArguments
{
    public int Count => 0;
    public T GetValue<T>(int index) => throw new ArgumentOutOfRangeException(nameof(index));
    public void Visit<TVisitor>(ref TVisitor visitor, int offset = 0)
        where TVisitor : IColumnValueVisitor, allows ref struct { }
}

/// <summary>Stores one concrete value followed by another concrete argument carrier.</summary>
public readonly struct SourceArguments<TValue, TRest>(TValue value, TRest rest) : ISourceArguments
    where TRest : struct, ISourceArguments
{
    private readonly TValue _value = value;
    private readonly TRest _rest = rest;
    public int Count => 1 + _rest.Count;

    public T GetValue<T>(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index != 0)
            return _rest.GetValue<T>(index - 1);
        if (typeof(T) != typeof(TValue))
            throw new InvalidOperationException($"Source argument {index} requires '{typeof(T)}' but its declared type is '{typeof(TValue)}'.");
        return Unsafe.As<TValue, T>(ref Unsafe.AsRef(in _value));
    }

    public void Visit<TVisitor>(ref TVisitor visitor, int offset = 0)
        where TVisitor : IColumnValueVisitor, allows ref struct
    {
        visitor.Visit(offset, in _value);
        _rest.Visit(ref visitor, offset + 1);
    }
}

/// <summary>Constructs native source arguments with compile-time generic inference.</summary>
public static class SourceArguments
{
    public static EmptySourceArguments Empty => default;
    public static SourceArguments<TValue, TRest> Create<TValue, TRest>(TValue value, TRest rest)
        where TRest : struct, ISourceArguments => new(value, rest);
}
