#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public class AmbiguousAggregateOwnerException : Exception, IDiagnosticException
{
    public AmbiguousAggregateOwnerException(string methodCall, IReadOnlyCollection<string> candidateAliases)
        : base(CreateMessage(methodCall, candidateAliases))
    {
        Code = DiagnosticCode.MQ3034_AmbiguousAggregateOwner;
    }

    public AmbiguousAggregateOwnerException(string methodCall, IReadOnlyCollection<string> candidateAliases, TextSpan span)
        : base(CreateMessage(methodCall, candidateAliases))
    {
        Code = DiagnosticCode.MQ3034_AmbiguousAggregateOwner;
        Span = span;
    }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }

    public static string CreateMessage(string methodCall, IReadOnlyCollection<string> candidateAliases)
    {
        var aliases = string.Join(", ", candidateAliases.Select(alias => $"'{alias}'"));
        return $"Aggregate call '{methodCall}' is ambiguous because multiple source aliases expose different implementations: {aliases}. Prefix the aggregate with the intended source alias to choose the schema library implementation explicitly.";
    }
}