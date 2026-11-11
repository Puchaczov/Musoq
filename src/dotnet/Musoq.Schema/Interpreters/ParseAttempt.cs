namespace Musoq.Schema.Interpreters;

/// <summary>A typed parse outcome; expected input failures need no thrown exception.</summary>
public readonly struct ParseAttempt<T>
{
    private ParseAttempt(T? value, ParseFailure? failure)
    {
        Value = value;
        Failure = failure;
    }

    /// <summary>Gets the successful or partially completed payload.</summary>
    public T? Value { get; }

    /// <summary>Gets the structured failure, or null on success.</summary>
    public ParseFailure? Failure { get; }

    /// <summary>Gets whether parsing completed successfully.</summary>
    public bool Succeeded => Failure is null;

    /// <summary>Creates a successful outcome.</summary>
    public static ParseAttempt<T> Success(T value) => new(value, null);

    /// <summary>Creates a failed outcome while retaining an optional partial payload.</summary>
    public static ParseAttempt<T> Failed(T? value, ParseFailure failure) =>
        new(value, failure ?? throw new ArgumentNullException(nameof(failure)));

    /// <summary>Preserves a throwing strict parsing boundary.</summary>
    public T GetValueOrThrow()
    {
        if (Failure is { } failure)
            throw failure.ToException();
        return Value!;
    }
}
