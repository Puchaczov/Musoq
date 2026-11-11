namespace Musoq.Schema.Interpreters;

/// <summary>Optional typed status protocol used by generated text interpreters.</summary>
public interface ITextInterpreterStatus<T>
{
    /// <summary>Reads at an offset and returns expected input errors as data.</summary>
    ParseAttempt<T> ReadAt(ref TextParseInput input, int offset);
}
