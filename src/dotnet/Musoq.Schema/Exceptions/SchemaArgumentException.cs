using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Exceptions;

/// <summary>
///     Exception thrown when invalid arguments are provided to schema operations.
///     Provides detailed information about what went wrong with the arguments.
/// </summary>
public class SchemaArgumentException : ArgumentException, IDiagnosticException
{

    public SchemaArgumentException(string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = "invalid-argument";
    }

    public SchemaArgumentException(string message)
        : base(message)
    {
        Reason = "invalid-argument";
    }

    public SchemaArgumentException()
    {
        Reason = "invalid-argument";
    }

    public SchemaArgumentException(string argumentName, string message)
        : this(argumentName, message, "invalid-argument")
    {
    }

    public SchemaArgumentException(string argumentName, string message, string? reason)
        : base(message, argumentName)
    {
        Reason = reason ?? "invalid-argument";
    }

    public SchemaArgumentException(string argumentName, string message, Exception innerException)
        : base(message, argumentName, innerException)
    {
        Reason = "invalid-argument";
    }

    public string Reason { get; }

    /// <summary>
    ///     Gets the legacy diagnostic classification for a structured schema argument failure.
    ///     Method-name failures are source-resolution errors; other invalid schema arguments remain generic
    ///     semantic failures until the v18 schema-resolution contract is applied.
    /// </summary>
    public DiagnosticCode Code => string.Equals(ParamName, "methodName", StringComparison.Ordinal)
        ? DiagnosticCode.MQ3085_UnknownSource
        : DiagnosticCode.MQ2030_UnsupportedSyntax;

    /// <inheritdoc />
    public TextSpan? Span => null;

    /// <inheritdoc />
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, TextSpan.Empty)
            .WithArgument("reason", Reason)
            .WithArgument("argumentName", ParamName ?? string.Empty);
    }

    public static SchemaArgumentException ForNullArgument(string argumentName, string operationContext)
    {
        return new SchemaArgumentException(
            argumentName,
            $"The argument '{argumentName}' cannot be null when {operationContext}. Please provide a valid value."
        );
    }

    public static SchemaArgumentException ForEmptyString(string argumentName, string operationContext)
    {
        return new SchemaArgumentException(
            argumentName,
            $"The argument '{argumentName}' cannot be empty or whitespace when {operationContext}. Please provide a non-empty value."
        );
    }

    public static SchemaArgumentException ForInvalidMethodName(
        string methodName,
        string availableMethods)
    {
        return ForInvalidMethodName(methodName, availableMethods, "unknown-source");
    }

    public static SchemaArgumentException ForInvalidMethodName(
        string methodName,
        string availableMethods,
        string reason)
    {
        return new SchemaArgumentException(
            nameof(methodName),
            $"The method '{methodName}' is not recognized. Available methods are: {availableMethods}. Please check the method name and try again.",
            reason
        );
    }
}
