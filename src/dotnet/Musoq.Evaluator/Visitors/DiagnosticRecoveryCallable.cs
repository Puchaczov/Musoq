using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryResolveMethodWithRecovery(
        AccessMethodNode node,
        ArgsListNode args,
        MethodResolutionContext methodContext,
        out MethodInfo method,
        out bool canSkipInjectSource)
    {
        try
        {
            (method, canSkipInjectSource) = DiagnosticRecovery.ResolveMethod(
                () => ResolveMethod(node, args, methodContext),
                DiagnosticContext,
                args);
            return true;
        }
        catch (CannotResolveMethodException exception) when (TryRecoverUnresolvedMethod(node, args, exception))
        {
            method = null!;
            canSkipInjectSource = false;
            return false;
        }
    }

    private bool TryRecoverUnresolvedMethod(
        AccessMethodNode node,
        ArgsListNode args,
        CannotResolveMethodException exception)
    {
        if (DiagnosticContext == null)
            return false;

        if (!DiagnosticRecovery.HasErrorsWithin(DiagnosticContext, args) &&
            !ReferencesDiagnosticRecoveryAlias(args) &&
            !HasSameCallableRoot(exception))
            DiagnosticContext.ReportException(exception);

        PushSemanticNode(new NullNode(typeof(object), node.SpanOrEmpty()));
        return true;
    }

    private bool ReferencesDiagnosticRecoveryAlias(Node value)
    {
        var pending = new Stack<Node>();
        pending.Push(value);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is AccessColumnNode accessColumn && IsDiagnosticRecoveryAlias(accessColumn))
                return true;
            foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(current))
                pending.Push(child);
        }

        return false;
    }

    private bool IsDiagnosticRecoveryAlias(AccessColumnNode node)
    {
        var primaryIdentifier = _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId)
            ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
            : _sourceBinding.Identifier;
        var identifier = string.IsNullOrEmpty(primaryIdentifier) ? node.Alias : primaryIdentifier;
        return _diagnosticRecoveryAliases.Contains(string.IsNullOrEmpty(node.Alias) ? identifier : node.Alias);
    }

    private bool HasSameCallableRoot(CannotResolveMethodException exception)
    {
        return DiagnosticContext!.Errors.Any(diagnostic =>
            diagnostic.Code == exception.Code &&
            (exception.Arguments.TryGetValue("callable", out var callable) &&
             diagnostic.Arguments.TryGetValue("callable", out var priorCallable)
                ? string.Equals(callable, priorCallable, StringComparison.OrdinalIgnoreCase)
                : string.Equals(diagnostic.Message, exception.Message, StringComparison.Ordinal)));
    }
}
