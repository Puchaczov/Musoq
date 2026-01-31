#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a property cannot be found on a type.
/// </summary>
public class UnknownPropertyException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of UnknownPropertyException.
    /// </summary>
    public UnknownPropertyException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3014_InvalidPropertyAccess;
    }

    /// <summary>
    ///     Initializes a new instance of UnknownPropertyException with property information.
    /// </summary>
    public UnknownPropertyException(string propertyName, string typeName, TextSpan span)
        : base($"Property '{propertyName}' not found on type '{typeName}'")
    {
        PropertyName = propertyName;
        TypeName = typeName;
        Code = DiagnosticCode.MQ3014_InvalidPropertyAccess;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the property that was not found.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    ///     Gets the name of the type on which the property was searched.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

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
