#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when two expression types cannot be used together in the current query context.
/// </summary>
public sealed class TypeMismatchException : Exception, IDiagnosticException
{
    public TypeMismatchException(Type expectedType, Type actualType, TextSpan span)
        : base($"Type mismatch: cannot convert '{actualType.Name}' to '{expectedType.Name}'.")
    {
        ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
        ActualType = actualType ?? throw new ArgumentNullException(nameof(actualType));
        Span = span;
        Code = DiagnosticCode.MQ3005_TypeMismatch;
    }

    public Type ExpectedType { get; }

    public Type ActualType { get; }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
