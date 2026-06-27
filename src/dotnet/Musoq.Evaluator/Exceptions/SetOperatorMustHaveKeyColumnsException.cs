using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Legacy compatibility exception for the MQ3031 set-operator missing-key diagnostic.
/// </summary>
public class SetOperatorMustHaveKeyColumnsException : Exception, IDiagnosticException
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public SetOperatorMustHaveKeyColumnsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance with the set operator name.
    /// </summary>
    public SetOperatorMustHaveKeyColumnsException(string setOperator)
        : base(CreateMessage(setOperator))
    {
        SetOperator = setOperator;
    }

    /// <summary>
    ///     Initializes a new instance with the set operator name and span.
    /// </summary>
    public SetOperatorMustHaveKeyColumnsException(string setOperator, TextSpan span)
        : base(CreateMessage(setOperator))
    {
        SetOperator = setOperator;
        Span = span;
    }

    internal static string CreateMessage(string setOperator)
    {
        var syntaxExample = GetSyntaxExample(setOperator);
        var normalizedOperator = GetDisplayName(setOperator);

        return
            $"Explicit key columns are optional for {normalizedOperator}. Omit the key list or use '()' to compare all projected values; use '{syntaxExample}' only when comparing an explicit subset.";
    }

    private static string GetSyntaxExample(string setOperator)
    {
        if (Comparer.Equals(setOperator, "UnionAll"))
            return "UNION ALL (<key_columns>)";

        if (Comparer.Equals(setOperator, "Union"))
            return "UNION (<key_columns>)";

        if (Comparer.Equals(setOperator, "Except"))
            return "EXCEPT (<key_columns>)";

        if (Comparer.Equals(setOperator, "Intersect"))
            return "INTERSECT (<key_columns>)";

        return $"{setOperator.ToUpperInvariant()} (<key_columns>)";
    }

    private static string GetDisplayName(string setOperator)
    {
        if (Comparer.Equals(setOperator, "UnionAll"))
            return "UNION ALL";

        if (Comparer.Equals(setOperator, "Union"))
            return "UNION";

        if (Comparer.Equals(setOperator, "Except"))
            return "EXCEPT";

        if (Comparer.Equals(setOperator, "Intersect"))
            return "INTERSECT";

        return setOperator.ToUpperInvariant();
    }

    /// <summary>
    ///     Gets the name of the set operator.
    /// </summary>
    public string SetOperator { get; } = string.Empty;

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code => DiagnosticCode.MQ3031_SetOperatorMissingKeys;

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }
}
