using System.Reflection;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Plugins.Attributes;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly NamedWindowDefinitionValidator _namedWindowValidator = new();

    internal void PrecollectCurrentQueryWindowDefinitions(WindowNode? window) =>
        _namedWindowValidator.Precollect(window, ReportWindowException);

    internal void EndCurrentQueryWindowDefinitionScope() => _namedWindowValidator.EndScope();

    private void ValidateNamedWindowReference(WindowFunctionNode node) =>
        _namedWindowValidator.Validate(node, ReportWindowException);

    public override void Visit(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ValidateNamedWindowReference(node);
        var spec = node.WindowSpecification != null
            ? PopSemanticNode("Visit(WindowFunctionNode).WindowSpec") as WindowSpecificationNode
            : null;
        var filterExpression = node.FunctionCall.FilterExpression != null
            ? PopSemanticNode("Visit(WindowFunctionNode).FilterExpression")
            : null;

        var funcArgCount = node.FunctionCall.Arguments?.Args.Length ?? 0;
        var funcArgs = new Node[funcArgCount];
        for (var i = funcArgCount - 1; i >= 0; i--)
            funcArgs[i] = PopSemanticNode("Visit(WindowFunctionNode).FuncArg");

        if (funcArgs is [AllColumnsNode] && node.FunctionCall.Name.Equals("Count", StringComparison.OrdinalIgnoreCase))
            funcArgs = [];
        var (returnType, resolvedFactory) = InferWindowFunctionReturnType(node.FunctionCall, funcArgs);
        var argsListNode = new ArgsListNode(funcArgs);

        var normalizedName = node.FunctionCall.Name.Replace("_", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        ValidateWindowEnumArguments(normalizedName, funcArgs, node);
        var requiresOrderBy = normalizedName is
            "ROWNUMBER" or "RANK" or "DENSERANK" or "PERCENTRANK" or "CUMEDIST" or "NTILE" or "LAG" or "LEAD";
        if (resolvedFactory?.GetCustomAttribute<WindowFunctionAttribute>() is { KernelType: not null } declaration)
            requiresOrderBy = declaration.RequiresOrderBy;

        if (requiresOrderBy && spec != null && spec.OrderByFields.Length == 0)
        {
            ReportWindowException(
                DiagnosticCode.MQ3099_WindowOrderByRequired,
                $"Window function '{node.FunctionCall.Name}' requires ORDER BY inside its OVER specification.",
                spec.SpanOrEmpty());
        }

        if (Array.Exists(funcArgs, WindowExpressionPresence.Contains) ||
            filterExpression != null && WindowExpressionPresence.Contains(filterExpression))
        {
            ReportWindowException(
                DiagnosticCode.MQ3100_NestedWindowFunction,
                "Window functions cannot be nested inside another window function. Move the inner expression into a CTE or derived query.",
                node.SpanOrEmpty());
        }

        if (_queryState.QueryPart is QueryPart.Where or QueryPart.Having)
        {
            var clause = _queryState.QueryPart == QueryPart.Where ? "WHERE" : "HAVING";
            ReportWindowException(
                DiagnosticCode.MQ3101_WindowFunctionInFilter,
                $"Window functions are not allowed in {clause}; use QUALIFY to filter window results.",
                node.SpanOrEmpty());
        }

        var functionCall = new AccessMethodNode(
            node.FunctionCall.FunctionToken,
            argsListNode,
            null,
            false,
            resolvedFactory,
            node.FunctionCall.Alias,
            node.FunctionCall.Span,
            node.FunctionCall.IsDistinct)
        {
            HasFilter = node.FunctionCall.HasFilter,
            FilterExpression = filterExpression,
            FilterExpressionText = node.FunctionCall.FilterExpressionText,
            IsPivotGenerated = node.FunctionCall.IsPivotGenerated,
            IsScalarSubqueryValueWrapper = node.FunctionCall.IsScalarSubqueryValueWrapper,
            IsCompilerGenerated = node.FunctionCall.IsCompilerGenerated
        };

        WindowFunctionNode result;
        if (node.IsNamedWindowReference)
            result = new WindowFunctionNode(
                functionCall,
                node.WindowName ?? throw new InvalidOperationException("Named window reference requires a window name."));
        else
            result = new WindowFunctionNode(
                functionCall,
                spec ?? throw new InvalidOperationException("Window function requires a window specification."));

        result.SetReturnType(returnType);
        result.WithSpan(node.Span);
        result.WithFullSpan(node.FullSpan);
        PushSemanticNode(result);
    }

    public override void Visit(WindowSpecificationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
        for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
            orderByFields[i] = (FieldOrderedNode)PopSemanticNode("Visit(WindowSpecificationNode).OrderBy");

        foreach (var orderByField in orderByFields)
        {
            if (!TryGetEnumExpressionType(orderByField.Expression, out var enumType))
                continue;

            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"Window ORDER BY is not supported for enum type '{enumType.DisplayName}' in v1.",
                orderByField);
        }

        var partitionFields = new FieldNode[node.PartitionFields.Length];
        for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
            partitionFields[i] = (FieldNode)PopSemanticNode("Visit(WindowSpecificationNode).Partition");

        if (node.Frame is { FrameType: WindowFrameType.Range } && orderByFields.Length == 0)
            ThrowRangeFrameRequiresOrderBy(node);

        WindowFrameSemanticValidator.Validate(node, orderByFields, ReportWindowException);

        var result = (WindowSpecificationNode)new WindowSpecificationNode(partitionFields, orderByFields, node.Frame)
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan);
        PushSemanticNode(result);
    }

    public override void Visit(WindowDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var spec = (WindowSpecificationNode)PopSemanticNode("Visit(WindowDefinitionNode).Spec");
        PushSemanticNode(((WindowDefinitionNode)new WindowDefinitionNode(node.Name, spec))
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(WindowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)PopSemanticNode("Visit(WindowNode).Definition");

        PushSemanticNode(((WindowNode)new WindowNode(definitions))
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    private (Type ReturnType, MethodInfo? ResolvedFactory) InferWindowFunctionReturnType(AccessMethodNode call, Node[] args)
    {
        var functionName = call.Name;
        var normalizedName = functionName.Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();
        var libraries = EnumerateWindowLibraries(call.Alias).ToArray();
        WindowDeclarationBinding.ValidateArguments(call, args, libraries);
        var resolvedFactory = WindowDeclarationBinding.Bind(functionName,
            args.Select(static argument => argument.ReturnType ?? typeof(object)).ToArray(), libraries);
        return (WindowFunctionReturnTypeResolver.Resolve(normalizedName, args, resolvedFactory), resolvedFactory);
    }

    private IEnumerable<ISchema> EnumerateWindowLibraries(string? owner)
    {
        foreach (var source in _sourceBinding.AliasToSchemaFromNodeMap)
        {
            if (!string.IsNullOrEmpty(owner) && source.Key != owner)
                continue;
            ThrowIfCancellationRequested();
            var schema = SchemaProviderBoundary.Invoke(() => _provider.GetSchema(source.Value.Schema));
            ThrowIfCancellationRequested();
            yield return schema;
        }
        if (!string.IsNullOrEmpty(owner))
            yield break;
        foreach (var schemaName in _sourceBinding.AllUsedSchemaNames)
        {
            ThrowIfCancellationRequested();
            var schema = SchemaProviderBoundary.Invoke(() => _provider.GetSchema(schemaName));
            ThrowIfCancellationRequested();
            yield return schema;
        }
        foreach (var tableName in _sourceBinding.AliasMapToInMemoryTableMap.Values)
        {
            ThrowIfCancellationRequested();
            var table = FindTableSymbolInScopeHierarchy(tableName).FullTable;
            yield return new TransitionSchema(tableName, table);
        }
    }
    private void ValidateQualifyReferencesWindowFunction(QualifyNode qualify)
    {
        if (WindowExpressionPresence.Contains(qualify.Expression))
            return;

        var span = qualify.Expression.HasSpan ? qualify.Expression.Span : TextSpan.Empty;

        ReportWindowException(
            DiagnosticCode.MQ3050_QualifyRequiresWindowFunction,
            "QUALIFY clause requires at least one window function in its expression.",
            span);
    }

    private void ThrowRangeFrameRequiresOrderBy(WindowSpecificationNode node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        const string message = "A RANGE window frame requires an ORDER BY clause in the window specification.";

        ReportWindowException(DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy, message, span);
    }

    private void ReportWindowException(DiagnosticCode code, string message, TextSpan span)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, span);
            return;
        }

        throw new CannotResolveMethodException(message, code, span);
    }
}
