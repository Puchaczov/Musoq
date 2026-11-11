namespace Musoq.Schema.Interpreters;

/// <summary>Optional typed status protocol used by generated binary interpreters.</summary>
public interface IBytesInterpreterStatus<T>
{
    /// <summary>Reads at an offset and returns expected input errors as data.</summary>
    ParseAttempt<T> ReadAt(ReadOnlySpan<byte> data, int offset);
}
