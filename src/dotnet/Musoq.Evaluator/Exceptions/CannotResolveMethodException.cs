using System.Linq;
using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a method cannot be resolved.
/// </summary>
public class CannotResolveMethodException : Exception, IDiagnosticException
{

    public CannotResolveMethodException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }

    public CannotResolveMethodException()
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }
    /// <summary>
    ///     Initializes a new instance with a message.
    /// </summary>
    public CannotResolveMethodException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }

    /// <summary>
    ///     Initializes a new instance with a message and span.
    /// </summary>
    public CannotResolveMethodException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
        Span = span;
    }

    /// <summary>
    ///     Initializes a new instance with a message, diagnostic code, and span.
    /// </summary>
    public CannotResolveMethodException(string message, DiagnosticCode code, TextSpan span)
        : base(message)
    {
        Code = code;
        Span = span;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    ///     Initializes an exception with a structured callable-resolution payload.
    /// </summary>
    internal CannotResolveMethodException(
        string message,
        DiagnosticCode code,
        TextSpan span,
        IReadOnlyDictionary<string, string>? arguments,
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null)
        : base(message)
    {
        Code = code;
        Span = span;
        Arguments = arguments is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(arguments, StringComparer.Ordinal);
        SuggestedFixes = suggestedFixes is null
            ? []
            : [..suggestedFixes];
    }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Gets stable facts describing the failed callable resolution.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    ///     Gets safe structured actions associated with this callable-resolution failure.
    /// </summary>
    public IReadOnlyList<DiagnosticAction> SuggestedFixes { get; } = [];

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var actions = SuggestedFixes.Count > 0
            ? SuggestedFixes
            : CreateCallableReplacement(sourceText, span);
        return SemanticDiagnosticFactory.Create(Code, Message, Span, sourceText, Arguments, actions);
    }

    private IReadOnlyList<DiagnosticAction>? CreateCallableReplacement(SourceText? sourceText, TextSpan span)
    {
        if (sourceText == null || span.IsEmpty || Code != DiagnosticCode.MQ3086_UnknownCallable ||
            !Arguments.TryGetValue("callable", out var callable) ||
            !Arguments.TryGetValue("suggestion", out var suggestion) ||
            !Arguments.TryGetValue("candidateCallables", out var candidateCallables))
            return null;

        var candidates = candidateCallables.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length != 1 || !string.Equals(candidates[0], suggestion, StringComparison.OrdinalIgnoreCase))
            return null;

        var start = Math.Max(0, span.Start);
        var end = Math.Min(sourceText.Length, span.End);
        if (start >= end)
            return null;

        var index = sourceText.Text.IndexOf(
            callable,
            start,
            end - start,
            StringComparison.OrdinalIgnoreCase);
        return index < 0
            ? null
            :
            [
                DiagnosticAction.QuickFix(
                    $"Replace '{callable}' with '{suggestion}'",
                    new TextSpan(index, callable.Length),
                    suggestion)
            ];
    }

    /// <summary>
    ///     Creates an exception for null arguments.
    /// </summary>
    public static CannotResolveMethodException CreateForNullArguments(string methodName)
    {
        return new CannotResolveMethodException($"Method {methodName} cannot be resolved because of null arguments");
    }

    /// <summary>
    ///     Creates an exception for unmatched method name or arguments.
    /// </summary>
    public static CannotResolveMethodException CreateForCannotMatchMethodNameOrArguments(string methodName, Node[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var types = args.Length > 0
            ? string.Join(", ", args.Select(f => f.ReturnType?.ToString() ?? "null"))
            : string.Empty;

        return new CannotResolveMethodException(
            $"Method {methodName} with argument types {types} cannot be resolved");
    }
}
