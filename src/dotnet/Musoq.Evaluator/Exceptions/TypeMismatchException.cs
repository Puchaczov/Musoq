using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when two expression types cannot be used together in the current query context.
/// </summary>
public sealed class TypeMismatchException : Exception, IDiagnosticException
{
    public TypeMismatchException(Type expectedType, Type actualType, TextSpan span)
        : base(CreateMessage(expectedType, actualType))
    {
        ExpectedType = expectedType;
        ActualType = actualType;
        Span = span;
    }

    public TypeMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
        ExpectedType = typeof(object);
        ActualType = typeof(object);
    }

    public TypeMismatchException(string message)
        : base(message)
    {
        ExpectedType = typeof(object);
        ActualType = typeof(object);
    }

    public TypeMismatchException()
    {
        ExpectedType = typeof(object);
        ActualType = typeof(object);
    }

    public Type ExpectedType { get; }

    public Type ActualType { get; }

    public DiagnosticCode Code { get; } = DiagnosticCode.MQ3005_TypeMismatch;

    public TextSpan? Span { get; }

    private static string CreateMessage(Type expectedType, Type actualType)
    {
        ArgumentNullException.ThrowIfNull(expectedType);
        ArgumentNullException.ThrowIfNull(actualType);
        return $"Type mismatch: cannot convert '{actualType.Name}' to '{expectedType.Name}'.";
    }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return SemanticDiagnosticFactory.Create(
            Code,
            Message,
            Span,
            sourceText,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["expectedType"] = ExpectedType.Name,
                ["actualType"] = ActualType.Name
            });
    }
}
