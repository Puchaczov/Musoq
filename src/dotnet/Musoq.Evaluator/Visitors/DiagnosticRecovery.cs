using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class DiagnosticRecovery
{
    internal static (MethodInfo Method, bool CanSkipInjectSource) ResolveMethod(
        Func<(MethodInfo Method, bool CanSkipInjectSource)> resolver,
        DiagnosticContext? context,
        ArgsListNode args)
    {
        try
        {
            return resolver();
        }
        catch (CannotResolveMethodException exception)
            when (SuppressDependentCallableDiagnostics(exception, context, args))
        {
            throw;
        }
    }

    internal static bool HasErrorsWithin(DiagnosticContext? context, Node value)
    {
        if (context is null)
            return false;

        var span = GetSpan(value);
        return !span.IsEmpty && context.Errors.Any(diagnostic => !diagnostic.Span.IsEmpty && span.Contains(diagnostic.Span));
    }

    private static bool SuppressDependentCallableDiagnostics(
        CannotResolveMethodException exception,
        DiagnosticContext? context,
        ArgsListNode args)
    {
        if (exception.Code is not (DiagnosticCode.MQ3086_UnknownCallable or DiagnosticCode.MQ3087_InvalidCallableArity) ||
            IsDialectConversion(exception))
            return false;

        context?.SuppressDiagnostics(diagnostic =>
            diagnostic.IsError && !diagnostic.Span.IsEmpty && args.SpanOrEmpty().Contains(diagnostic.Span));
        return true;
    }

    private static bool IsDialectConversion(CannotResolveMethodException exception)
    {
        return exception.Arguments.TryGetValue("callable", out var callable) &&
               callable.Equals("CONVERT", StringComparison.OrdinalIgnoreCase);
    }

    private static TextSpan GetSpan(Node value)
    {
        var pending = new Stack<Node>();
        pending.Push(value);
        var start = int.MaxValue;
        var end = int.MinValue;
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            var span = current.Span.IsEmpty ? current.FullSpan : current.Span;
            if (!span.IsEmpty)
            {
                start = Math.Min(start, span.Start);
                end = Math.Max(end, span.End);
            }

            foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(current))
                pending.Push(child);
        }

        return start == int.MaxValue ? TextSpan.Empty : TextSpan.FromBounds(start, end);
    }
}

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly HashSet<string> _diagnosticRecoveryCteNames = new(StringComparer.Ordinal);
}
