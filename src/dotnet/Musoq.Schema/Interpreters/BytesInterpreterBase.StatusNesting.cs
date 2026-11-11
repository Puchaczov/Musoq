namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    /// <summary>Reads a nested schema through its typed status or legacy fallback.</summary>
    protected ParseAttempt<TChild> InterpretNestedStatus<TChild>(IBytesInterpreter<TChild> interpreter,
        ReadOnlySpan<byte> data, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        if (AlignWithStatus(8) is { } alignment)
            return ParseAttempt<TChild>.Failed(default, alignment);
        var attempt = InterpretationStatus.ReadAt(interpreter, data, ParsePosition);
        ResumeAfterNested(interpreter);
        return attempt.Failure is { } failure
            ? ParseAttempt<TChild>.Failed(default, failure.WithContext(fieldName, true)) : attempt;
    }

    /// <summary>Reads a bounded nested region and rebases failure positions.</summary>
    protected ParseAttempt<TChild> InterpretNestedAtStatus<TChild>(IBytesInterpreter<TChild> interpreter,
        ReadOnlySpan<byte> data, int offset, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        var origin = ParsePosition;
        var attempt = InterpretationStatus.ReadAt(interpreter, data, offset);
        ResumeAfterSubstream(interpreter, origin, offset);
        if (attempt.Failure is not { } failure)
            return attempt;
        var rebased = Math.Clamp((long)origin + failure.Position - offset, int.MinValue, int.MaxValue);
        return ParseAttempt<TChild>.Failed(default, failure.WithContext(fieldName, true, (int)rebased));
    }

    /// <summary>Parses embedded text without converting ordinary child failures into thrown exceptions.</summary>
    protected ParseAttempt<TChild> ParseNestedStatus<TChild>(ITextInterpreter<TChild> interpreter,
        string text, string fieldName)
    {
        var attempt = InterpretationStatus.ReadAt(interpreter, text.AsSpan(), 0);
        return attempt.Failure is { } failure
            ? ParseAttempt<TChild>.Failed(default, failure.WithContext(fieldName, true)) : attempt;
    }
}
