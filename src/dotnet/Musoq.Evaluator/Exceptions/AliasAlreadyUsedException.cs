#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an alias is already in use.
/// </summary>
public class AliasAlreadyUsedException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with node and alias.
    /// </summary>
    public AliasAlreadyUsedException(SchemaFromNode node, string alias)
        : base(
            $"Alias {alias} is already used in query. Please, use different alias. Problem occurred in schema from node {node}")
    {
        Alias = alias;
        Code = DiagnosticCode.MQ3021_DuplicateAlias;
    }

    /// <summary>
    ///     Initializes a new instance with message and span.
    /// </summary>
    public AliasAlreadyUsedException(string alias, TextSpan span)
        : base($"Alias '{alias}' is already used in query. Please use a different alias.")
    {
        Alias = alias;
        Code = DiagnosticCode.MQ3021_DuplicateAlias;
        Span = span;
    }

    /// <summary>
    ///     Gets the duplicate alias name.
    /// </summary>
    public string? Alias { get; }

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
