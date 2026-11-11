namespace Musoq.Schema.Interpreters;

/// <summary>Supplies immutable field slots without adding state to interpreter or payload instances.</summary>
public interface IParserCompletionLayoutSource
{
    /// <summary>Gets the declaration-order layout shared by the generated schema.</summary>
    ParserCompletionLayout CompletionLayout { get; }
}
