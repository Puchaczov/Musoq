namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>Parses a nested schema and preserves its cursor and field path on failure.</summary>
    protected ParseAttempt<TChild> ParseNestedStatus<TChild>(
        ITextInterpreter<TChild> interpreter,
        ref TextParseInput input,
        string fieldName
    )
    {
        var attempt = InterpretationStatus.ReadAt(interpreter, ref input, ParsePosition);
        ParsePosition = interpreter.CharsConsumed;
        return attempt.Failure is { } failure
            ? ParseAttempt<TChild>.Failed(default, failure.WithContext(fieldName, false))
            : attempt;
    }

    /// <summary>Checks the declared text repeat limit without throwing an expected input error.</summary>
    protected ParseFailure? CheckRepeatIterationStatus(string fieldName, int iteration, int maxIterations) =>
        iteration >= maxIterations
            ? ParseFailure.ForText(
                ParseErrorCode.MaxIterationsExceeded,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Repeat field '{fieldName}' exceeded the maximum of {maxIterations} iterations."
            )
            : null;

    /// <summary>Checks that a repeated element consumed characters.</summary>
    protected ParseFailure? CheckRepeatProgressStatus(string fieldName, int startPosition) =>
        ParsePosition > startPosition
            ? null
            : ParseFailure.ForText(
                ParseErrorCode.MaxIterationsExceeded,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Repeat field '{fieldName}' made no progress reading an element; the element type consumes zero characters."
            );
}
