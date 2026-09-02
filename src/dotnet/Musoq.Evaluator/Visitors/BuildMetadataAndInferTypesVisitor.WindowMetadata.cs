using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

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

        WindowFunctionArgumentValidation.Validate(node.FunctionCall, funcArgs, ReportWindowArgumentError);
        var (returnType, resolvedFactory) = InferWindowFunctionReturnType(node.FunctionCall.Name, funcArgs);
        var argsListNode = new ArgsListNode(funcArgs);

        var normalizedName = node.FunctionCall.Name.Replace("_", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        ValidateWindowEnumArguments(normalizedName, funcArgs, node);
        var requiresOrderBy = normalizedName is
            "ROWNUMBER" or "RANK" or "DENSERANK" or "PERCENTRANK" or "CUMEDIST" or "NTILE" or "LAG" or "LEAD";

        if (requiresOrderBy && spec != null && spec.OrderByFields.Length == 0)
        {
            ReportWindowException(
                DiagnosticCode.MQ3099_WindowOrderByRequired,
                $"Window function '{node.FunctionCall.Name}' requires ORDER BY inside its OVER specification.",
                spec.SpanOrEmpty());
        }

        var nestedWindowDetector = new QualifyWindowFunctionDetector();
        var nestedWindowTraverser = new QualifyWindowFunctionTraverser(nestedWindowDetector);
        foreach (var argument in funcArgs)
            argument.Accept(nestedWindowTraverser);
        filterExpression?.Accept(nestedWindowTraverser);

        if (nestedWindowDetector.Found)
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
            IsScalarSubqueryValueWrapper = node.FunctionCall.IsScalarSubqueryValueWrapper
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

    private (Type ReturnType, MethodInfo? ResolvedFactory) InferWindowFunctionReturnType(string functionName, Node[] args)
    {
        var normalizedName = functionName.Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();

        MethodInfo? resolvedFactory = null;
        var isBuiltInOffset = normalizedName is "LAG" or "LEAD";
        var isBuiltInRanking = normalizedName is "ROWNUMBER" or "RANK" or "DENSERANK" or "PERCENTRANK" or "CUMEDIST";

        if (!isBuiltInOffset && !isBuiltInRanking)
            TryResolveWindowFunctionFactory(functionName, out resolvedFactory);

        Type returnType;
        if (resolvedFactory != null)
        {
            returnType = IsValueAccessWindowFunction(normalizedName) && args.Length > 0
                ? MakeNullableIfValueType(args[0].ReturnType ?? typeof(object))
                : IsMinMaxWindowFunction(normalizedName) && args.Length > 0
                    ? MakeNullableIfValueType(args[0].ReturnType ?? typeof(object))
                    : ExtractWindowFunctionResultType(resolvedFactory) ?? typeof(object);
        }
        else
        {
            returnType = normalizedName switch
            {
                "LAG" or "LEAD" => MakeNullableIfValueType(
                    args.Length > 0 ? args[0].ReturnType ?? typeof(object) : typeof(object)),
                "ROWNUMBER" or "RANK" or "DENSERANK" => typeof(long),
                "PERCENTRANK" or "CUMEDIST" => typeof(double),
                _ => typeof(object)
            };
        }

        return (returnType, resolvedFactory);
    }

    private static bool IsValueAccessWindowFunction(string normalizedName)
    {
        return normalizedName is "FIRSTVALUE" or "LASTVALUE" or "NTHVALUE";
    }

    private static bool IsMinMaxWindowFunction(string normalizedName)
    {
        return normalizedName is "MIN" or "MAX";
    }

    private bool TryResolveWindowFunctionFactory(string functionName, out MethodInfo? factoryMethod)
    {
        foreach (var schemaFrom in _sourceBinding.AliasToSchemaFromNodeMap.Values)
        {
            var schema = SchemaProviderBoundary.Invoke(() => _provider.GetSchema(schemaFrom.Schema));
            if (schema.TryResolveWindowFunction(functionName, out var resolved))
            {
                factoryMethod = resolved;
                return true;
            }
        }

        foreach (var schemaName in _sourceBinding.AllUsedSchemaNames)
        {
            var schema = SchemaProviderBoundary.Invoke(() => _provider.GetSchema(schemaName));
            if (schema.TryResolveWindowFunction(functionName, out var resolved))
            {
                factoryMethod = resolved;
                return true;
            }
        }

        factoryMethod = null;
        return false;
    }

    private static Type? ExtractWindowFunctionResultType(MethodInfo factoryMethod)
    {
        var returnType = factoryMethod.ReturnType;
        var windowFunctionInterface = returnType.GetInterfaces()
            .Concat([returnType])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWindowFunction<,>));

        return windowFunctionInterface?.GetGenericArguments()[1];
    }

    private static Type MakeNullableIfValueType(Type type)
    {
        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            return typeof(Nullable<>).MakeGenericType(type);

        return type;
    }

    private void ValidateQualifyReferencesWindowFunction(QualifyNode qualify)
    {
        var detector = new QualifyWindowFunctionDetector();
        var traverser = new QualifyWindowFunctionTraverser(detector);
        qualify.Expression.Accept(traverser);

        if (detector.Found)
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

    private void ReportWindowArgumentError(DiagnosticCode code, string message, Node context)
    {
        var span = context.SpanOrEmpty();
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, span);
            return;
        }

        throw new CannotResolveMethodException(message, code, span);
    }

    private sealed class QualifyWindowFunctionTraverser(IExpressionVisitor visitor)
        : RawTraverseVisitor<IExpressionVisitor>(visitor);

    private sealed class QualifyWindowFunctionDetector : NoOpExpressionVisitor
    {
        public bool Found { get; private set; }

        public override void Visit(WindowFunctionNode node) => Found = true;
    }
}
