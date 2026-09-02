using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class NamedWindowDefinitionValidator
{
    private readonly Stack<HashSet<string>> _scopes = new();

    internal void Precollect(
        WindowNode? window,
        Action<DiagnosticCode, string, TextSpan> report)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (window != null)
        {
            foreach (var definition in window.Definitions)
            {
                if (names.Add(definition.Name))
                    continue;

                report(
                    DiagnosticCode.MQ3105_DuplicateNamedWindow,
                    $"Window definition '{definition.Name}' is declared more than once in this query.",
                    definition.SpanOrEmpty());
            }
        }

        _scopes.Push(names);
    }

    internal void EndScope()
    {
        if (_scopes.Count > 0)
            _scopes.Pop();
    }

    internal void Validate(
        WindowFunctionNode node,
        Action<DiagnosticCode, string, TextSpan> report)
    {
        if (!node.IsNamedWindowReference || node.WindowName is not { } windowName)
            return;

        if (_scopes.Count > 0 && _scopes.Peek().Contains(windowName))
            return;

        report(
            DiagnosticCode.MQ3104_UnknownNamedWindow,
            $"Named window '{windowName}' is not defined in the current query.",
            node.SpanOrEmpty());
    }
}
