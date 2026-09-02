using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class UnknownAliasException : Exception, IDiagnosticException
{
    public UnknownAliasException(string alias, TextSpan span, IEnumerable<string>? availableAliases = null)
        : base(CreateMessage(alias, availableAliases))
    {
        Alias = alias;
        Span = span;
        AvailableAliases = NormalizeAliases(availableAliases);
        var candidates = ErrorCatalog.GetDidYouMeanCandidates(Alias, AvailableAliases);
        Suggestion = candidates.Count == 1 ? candidates[0] : null;
    }

    public string Alias { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3015_UnknownAlias;

    public TextSpan? Span { get; }

    public IReadOnlyList<string> AvailableAliases { get; }

    public string? Suggestion { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = GetAliasSpan(sourceText, Span ?? TextSpan.Empty, Alias);
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null;
        if (!string.IsNullOrWhiteSpace(Suggestion))
        {
            if (AvailableAliases.Count == 1)
                suggestedFixes =
                [
                    DiagnosticAction.QuickFix(
                        $"Replace '{Alias}' with '{Suggestion}'",
                        span,
                        Suggestion)
                ];
        }

        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["alias"] = Alias,
            ["availableAliases"] = string.Join(", ", AvailableAliases)
        };
        if (!string.IsNullOrWhiteSpace(Suggestion))
            arguments["suggestion"] = Suggestion;

        return SemanticDiagnosticFactory.Create(Code, Message, Span, sourceText, arguments, suggestedFixes);
    }

    private static string CreateMessage(string alias, IEnumerable<string>? availableAliases)
    {
        var candidates = NormalizeAliases(availableAliases);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(alias, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        return string.IsNullOrWhiteSpace(suggestion)
            ? closeCandidates.Count > 1
                ? $"Unknown alias '{alias}'. Possible matches: {string.Join(", ", closeCandidates.Select(static candidate => $"'{candidate}'"))}."
                : $"Unknown alias '{alias}'."
            : $"Unknown alias '{alias}'. Did you mean '{suggestion}'?";
    }

    private static string[] NormalizeAliases(IEnumerable<string>? aliases)
    {
        return aliases?
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static alias => alias, StringComparer.Ordinal)
            .ToArray() ?? [];
    }

    private static TextSpan GetAliasSpan(SourceText? sourceText, TextSpan nodeSpan, string alias)
    {
        if (sourceText == null || nodeSpan.IsEmpty || string.IsNullOrWhiteSpace(alias))
            return nodeSpan;

        var start = Math.Max(0, nodeSpan.Start);
        var end = Math.Min(sourceText.Length, nodeSpan.End);
        if (start >= end)
            return nodeSpan;

        var index = sourceText.Text.IndexOf(alias, start, end - start, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? new TextSpan(index, alias.Length) : nodeSpan;
    }
}
