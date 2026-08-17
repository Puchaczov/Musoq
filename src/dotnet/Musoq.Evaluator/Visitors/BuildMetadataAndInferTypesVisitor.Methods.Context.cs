using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private MethodResolutionContext ResolveMethodContext(AccessMethodNode node, ArgsListNode args)
    {
        var identifier = GetCurrentMethodResolutionIdentifier();

        if (_sourceBinding.UsedSchemasQuantity > 1 && string.IsNullOrWhiteSpace(node.Alias))
        {
            if (TryResolveSingleVisibleMethodContext(identifier, out var singleVisibleContext))
                return singleVisibleContext;

            if (TryInferAggregateMethodContext(identifier, node, args, out var inferredContext))
                return inferredContext;

            if (TryReportAmbiguousAggregateOwnerFromCandidates(identifier, node, args))
                throw new VisitorException(
                    nameof(BuildMetadataAndInferTypesVisitor),
                    nameof(ResolveMethodContext),
                    "Aggregate owner resolution reported an ambiguous aggregate owner.",
                    DiagnosticCode.MQ3034_AmbiguousAggregateOwner,
                    node.SpanOrEmpty());

            if (TryInferNonAggregateMethodContext(identifier, node, args, out var nonAggContext))
                return nonAggContext;

            if (TryReportMissingAlias(node))
                throw new VisitorException(
                    nameof(BuildMetadataAndInferTypesVisitor),
                    nameof(ResolveMethodContext),
                    AliasMissingException.CreateMethodCallMessage(node.ToString()),
                    DiagnosticCode.MQ3022_MissingAlias,
                    node.SpanOrEmpty());
            var span = node.SpanOrEmpty();
            throw new AliasMissingException(AliasMissingException.CreateMethodCallMessage(node.ToString()), span);
        }

        var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : identifier;
        return CreateMethodResolutionContext(alias);
    }

    private bool TryResolveSingleVisibleMethodContext(string identifier, out MethodResolutionContext context)
    {
        context = default;

        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (tableSymbol.IsCompoundTable || tableSymbol.CompoundTables.Length != 1)
            return false;

        context = CreateMethodResolutionContext(tableSymbol.CompoundTables[0]);
        return true;
    }

    private string GetCurrentMethodResolutionIdentifier()
    {
        return _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId)
            ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
            : _sourceBinding.Identifier;
    }

    private bool TryInferNonAggregateMethodContext(string identifier, AccessMethodNode node, ArgsListNode args,
        out MethodResolutionContext context)
    {
        context = default;

        var tableSymbol = FindTableSymbolInScopeHierarchy(identifier);
        if (!tableSymbol.IsCompoundTable)
            return false;

        var argTypes = GetArgumentTypes(args);
        var methodName = node.Name;

        MethodInfo? firstMethod = null;
        MethodResolutionContext? resolvedContext = null;
        var ambiguousAliases = new List<string>();
        var allCandidatesEquivalent = true;

        foreach (var alias in tableSymbol.CompoundTables)
        {
            var candidateContext = CreateMethodResolutionContext(alias, false);
            var schema = candidateContext.SchemaTablePair.Schema;

            if (!schema.TryResolveMethod(methodName, argTypes, candidateContext.EntityType, out var candidateMethod) &&
                !schema.TryResolveRawMethod(methodName, argTypes, out candidateMethod))
                continue;

            if (firstMethod == null)
            {
                firstMethod = candidateMethod;
                resolvedContext = candidateContext;
                ambiguousAliases.Add(alias);
                continue;
            }

            ambiguousAliases.Add(alias);

            if (allCandidatesEquivalent)
            {
                allCandidatesEquivalent = AreSameMethod(firstMethod, candidateMethod) &&
                                          IsAliasIndependentInvocation(firstMethod) &&
                                          IsAliasIndependentInvocation(candidateMethod);
            }
        }

        if (resolvedContext == null)
            return false;

        if (ambiguousAliases.Count > 1 && !allCandidatesEquivalent)
        {
            ReportAmbiguousMethodOwner(node, ambiguousAliases);
            return true;
        }

        RegisterMethodContextAssemblies(resolvedContext.Value.EntityType);
        context = resolvedContext.Value;
        return true;
    }

    private static bool IsAliasIndependentInvocation(MethodInfo method)
    {
        return !method.GetParameters()
            .SelectMany(static parameter => parameter.GetCustomAttributes(true))
            .Any(static attribute => attribute.GetType().Name is
                nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute");
    }

    private void ReportAmbiguousMethodOwner(AccessMethodNode methodNode, IReadOnlyCollection<string> candidateAliases)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousMethodOwner(methodNode.ToString(), candidateAliases, methodNode);
            throw new AmbiguousMethodOwnerException(methodNode.ToString(), candidateAliases, methodNode.SpanOrEmpty());
        }

        var span = methodNode.SpanOrEmpty();
        throw new AmbiguousMethodOwnerException(methodNode.ToString(), candidateAliases, span);
    }

    private MethodResolutionContext CreateMethodResolutionContext(string alias, bool registerAssemblies = true)
    {
        var tableSymbol = FindTableSymbolInScopeHierarchy(alias);
        var schemaTablePair = tableSymbol.GetTableByAlias(alias);
        var entityType = schemaTablePair.Table.Metadata.TableEntityType;

        if (registerAssemblies)
            RegisterMethodContextAssemblies(entityType);

        return new MethodResolutionContext(alias, tableSymbol, schemaTablePair, entityType);
    }

    private void RegisterMethodContextAssemblies(Type entityType)
    {
        _methodBindingService.RegisterContextAssemblies(entityType);
    }
}
