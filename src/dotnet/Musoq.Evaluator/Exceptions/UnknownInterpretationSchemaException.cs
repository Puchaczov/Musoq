#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an interpretation schema reference cannot be resolved.
///     This typically occurs when the Interpret(), Parse(), or InterpretAt() functions
///     reference a schema that was not defined in the INTERPRET block.
/// </summary>
public class UnknownInterpretationSchemaException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UnknownInterpretationSchemaException" /> class.
    /// </summary>
    /// <param name="schemaName">The name of the unknown schema.</param>
    public UnknownInterpretationSchemaException(string schemaName)
        : base(
            $"Interpretation schema '{schemaName}' is not defined. Ensure the schema is declared in an INTERPRET block at the beginning of your query.")
    {
        SchemaName = schemaName;
        Code = DiagnosticCode.MQ3010_UnknownSchema;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnknownInterpretationSchemaException" /> class.
    /// </summary>
    /// <param name="schemaName">The name of the unknown schema.</param>
    /// <param name="message">A custom error message.</param>
    public UnknownInterpretationSchemaException(string schemaName, string message)
        : base(message)
    {
        SchemaName = schemaName;
        Code = DiagnosticCode.MQ3010_UnknownSchema;
    }

    /// <summary>
    ///     Initializes a new instance with diagnostic information.
    /// </summary>
    public UnknownInterpretationSchemaException(string schemaName, string message, TextSpan span)
        : base(message)
    {
        SchemaName = schemaName;
        Code = DiagnosticCode.MQ3010_UnknownSchema;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the schema that could not be resolved.
    /// </summary>
    public string SchemaName { get; }

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

    /// <summary>
    ///     Creates an exception for when a schema is not found in the registry.
    /// </summary>
    /// <param name="schemaName">The name of the schema that was not found.</param>
    /// <returns>A new exception instance.</returns>
    public static UnknownInterpretationSchemaException CreateForSchemaNotInRegistry(string schemaName)
    {
        return new UnknownInterpretationSchemaException(
            schemaName,
            $"Interpretation schema '{schemaName}' was not found in the schema registry. " +
            $"Possible causes:\n" +
            $"  1. The schema '{schemaName}' is not defined in an INTERPRET block\n" +
            $"  2. The schema name is misspelled (check case sensitivity)\n" +
            $"  3. The INTERPRET block compilation failed silently");
    }

    /// <summary>
    ///     Creates an exception for when a schema type could not be generated.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <returns>A new exception instance.</returns>
    public static UnknownInterpretationSchemaException CreateForTypeGenerationFailed(string schemaName)
    {
        return new UnknownInterpretationSchemaException(
            schemaName,
            $"Interpretation schema '{schemaName}' was found but its generated type is unavailable. " +
            $"This may indicate a code generation or compilation failure.");
    }
}
