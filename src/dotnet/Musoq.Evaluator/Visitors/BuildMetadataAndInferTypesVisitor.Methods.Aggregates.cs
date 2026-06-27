using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryInferAggregateMethodContext(string identifier, AccessMethodNode node, ArgsListNode args,
        out MethodResolutionContext context)
    {
        context = default;

        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        AggregateResolutionSignature? signature = null;
        MethodResolutionContext? resolvedContext = null;
        var ambiguousAliases = new List<string>();

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);

            if (!TryResolveAggregateSignature(node, args, candidateContext, out var candidateSignature))
                continue;

            if (signature == null)
            {
                signature = candidateSignature;
                resolvedContext = candidateContext;
                continue;
            }

            if (!signature.Value.Equals(candidateSignature))
            {
                if (ambiguousAliases.Count == 0 && resolvedContext != null)
                    ambiguousAliases.Add(resolvedContext.Value.Alias);

                ambiguousAliases.Add(candidateContext.Alias);
            }
        }

        if (ambiguousAliases.Count > 0)
        {
            var inferredAlias = TryInferAliasFromArguments(args, tableSymbol);
            if (inferredAlias != null && ambiguousAliases.Contains(inferredAlias))
            {
                var directContext = CreateMethodResolutionContext(inferredAlias, false);
                if (TryResolveAggregateSignature(node, args, directContext, out _))
                {
                    RegisterMethodContextAssemblies(directContext.EntityType);
                    context = directContext;
                    return true;
                }
            }

            ReportAmbiguousAggregateOwner(node, ambiguousAliases);
            return false;
        }

        if (resolvedContext == null)
            return false;

        RegisterMethodContextAssemblies(resolvedContext.Value.EntityType);
        context = resolvedContext.Value;
        return true;
    }

    private static string? TryInferAliasFromArguments(ArgsListNode args, TableSymbol tableSymbol)
    {
        if (!tableSymbol.CompoundTables.Any(a => a.StartsWith(Helpers.Subqueries.GeneratedSubqueryContract.SubqueryPrefix, StringComparison.Ordinal)))
            return null;

        string? commonAlias = null;

        foreach (var arg in args.Args)
        {
            if (arg is not AccessColumnNode columnNode)
                continue;

            if (string.IsNullOrEmpty(columnNode.Alias))
                return null;

            if (commonAlias == null)
                commonAlias = columnNode.Alias;
            else if (commonAlias != columnNode.Alias)
                return null;
        }

        if (commonAlias == null)
            return null;

        return tableSymbol.CompoundTables.Contains(commonAlias) ? commonAlias : null;
    }

    private bool TryReportAmbiguousAggregateOwnerFromCandidates(string identifier, AccessMethodNode node,
        ArgsListNode args)
    {
        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        var inferredAlias = TryInferAliasFromArguments(args, tableSymbol);
        if (inferredAlias != null)
            return false;

        var argTypes = GetArgumentTypes(args);
        var methodName = node.IsDistinct ? $"{node.Name}Distinct" : node.Name;
        var candidateAliases = new List<string>();
        Type? schemaType = null;
        MethodInfo? resolvedMethod = null;

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);
            if (!TryResolveAggregateDeclarationMethod(methodName, argTypes, args, candidateContext, out var candidateMethod))
                continue;

            if (schemaType == null)
            {
                schemaType = candidateContext.SchemaTablePair.Schema.GetType();
                resolvedMethod = candidateMethod;
                candidateAliases.Add(alias);
                continue;
            }

            if (schemaType != candidateContext.SchemaTablePair.Schema.GetType() ||
                resolvedMethod == null ||
                !AreSameMethod(resolvedMethod, candidateMethod))
            {
                candidateAliases.Add(alias);
            }
        }

        if (candidateAliases.Count <= 1)
            return false;

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousAggregateOwner(node.ToString(), candidateAliases, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new AmbiguousAggregateOwnerException(node.ToString(), candidateAliases, span);
    }

    private void ReportAmbiguousAggregateOwner(AccessMethodNode methodNode, IReadOnlyCollection<string> candidateAliases)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousAggregateOwner(methodNode.ToString(), candidateAliases, methodNode);
            return;
        }

        var span = methodNode.SpanOrEmpty();
        throw new AmbiguousAggregateOwnerException(methodNode.ToString(), candidateAliases, span);
    }

    private static bool TryResolveAggregateSignature(AccessMethodNode node, ArgsListNode args, MethodResolutionContext context,
        out AggregateResolutionSignature signature)
    {
        signature = default;

        var argTypes = GetArgumentTypes(args);

        var methodName = node.Name;
        if (node.IsDistinct)
        {
            methodName = $"{methodName}Distinct";
            if (TryResolveAggregateDeclarationMethod(methodName, argTypes, args, context, out var distinctDeclaration))
            {
                signature = new AggregateResolutionSignature(
                    context.SchemaTablePair.Schema.GetType(),
                    distinctDeclaration,
                    distinctDeclaration);
                return true;
            }

            return false;
        }

        if (TryResolveAggregateDeclarationMethod(methodName, argTypes, args, context, out var declaration))
        {
            signature = new AggregateResolutionSignature(
                context.SchemaTablePair.Schema.GetType(),
                declaration,
                declaration);
            return true;
        }

        return false;
    }
}
