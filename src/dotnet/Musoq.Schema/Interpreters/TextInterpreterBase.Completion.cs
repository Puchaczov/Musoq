using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private ISet<string> CreateCompletionSet() =>
        this is IParserCompletionLayoutSource source
            ? new CompactCompletionSet(source.CompletionLayout)
            : new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Records a completed generated field without hashing its name.</summary>
    protected void RecordParsedFieldSlot(int index)
    {
        if (_partialParsedFields is CompactCompletionSet fields)
            fields.Mark(index);
    }
}
