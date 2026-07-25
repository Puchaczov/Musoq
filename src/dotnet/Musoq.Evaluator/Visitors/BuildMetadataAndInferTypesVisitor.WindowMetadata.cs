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
    public override void Visit(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
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

        var (returnType, resolvedFactory) = InferWindowFunctionReturnType(node.FunctionCall.Name, funcArgs);
        var argsListNode = new ArgsListNode(funcArgs);

        var functionCall = new AccessMethodNode(
            node.FunctionCall.FunctionToken,
            argsListNode,
            null,
            false,
            resolvedFactory,
            node.FunctionCall.Alias,
            default,
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
        PushSemanticNode(result);
    }

    public override void Visit(WindowSpecificationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
        for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
            orderByFields[i] = (FieldOrderedNode)PopSemanticNode("Visit(WindowSpecificationNode).OrderBy");

        var partitionFields = new FieldNode[node.PartitionFields.Length];
        for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
            partitionFields[i] = (FieldNode)PopSemanticNode("Visit(WindowSpecificationNode).Partition");

        if (node.Frame is { FrameType: WindowFrameType.Range } && orderByFields.Length == 0)
            ThrowRangeFrameRequiresOrderBy(node);

        if (node.Frame != null)
            ValidateWindowFrameBounds(node);

        PushSemanticNode(new WindowSpecificationNode(partitionFields, orderByFields, node.Frame));
    }

    public override void Visit(WindowDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var spec = (WindowSpecificationNode)PopSemanticNode("Visit(WindowDefinitionNode).Spec");
        PushSemanticNode(new WindowDefinitionNode(node.Name, spec));
    }

    public override void Visit(WindowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)PopSemanticNode("Visit(WindowNode).Definition");

        PushSemanticNode(new WindowNode(definitions));
    }

    private (Type ReturnType, MethodInfo? ResolvedFactory) InferWindowFunctionReturnType(string functionName, Node[] args)
    {
        var normalizedName = functionName.Replace("_", "", StringComparison.Ordinal).ToUpperInvariant();

        MethodInfo? resolvedFactory = null;
        var isBuiltInOffset = normalizedName is "LAG" or "LEAD";
        var isBuiltInRanking = normalizedName is "ROWNUMBER" or "RANK" or "DENSERANK";

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

        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "ValidateQualifyReferencesWindowFunction",
            "QUALIFY clause requires at least one window function in its expression.",
            DiagnosticCode.MQ3050_QualifyRequiresWindowFunction,
            span);

        if (TryReportException(exception, qualify))
            return;

        throw exception;
    }

    private void ThrowRangeFrameRequiresOrderBy(WindowSpecificationNode node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        const string message = "A RANGE window frame requires an ORDER BY clause in the window specification.";

        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "Visit(WindowSpecificationNode)",
            message,
            DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy,
            span);

        if (TryReportException(exception, node))
            return;

        throw exception;
    }

    private void ValidateWindowFrameBounds(WindowSpecificationNode node)
    {
        var frame = node.Frame ??
                    throw new InvalidOperationException("Window frame validation requires a frame.");
        var startRank = GetBoundRank(frame.Start.BoundType);
        var endRank = GetBoundRank(frame.End.BoundType);

        if (startRank > endRank)
            ThrowInvalidWindowFrameBounds(node);
    }

    private static int GetBoundRank(WindowFrameBoundType boundType)
    {
        return boundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => 0,
            WindowFrameBoundType.OffsetPreceding => 1,
            WindowFrameBoundType.CurrentRow => 2,
            WindowFrameBoundType.OffsetFollowing => 3,
            WindowFrameBoundType.UnboundedFollowing => 4,
            _ => throw new InvalidOperationException($"Unknown bound type: {boundType}")
        };
    }

    private void ThrowInvalidWindowFrameBounds(WindowSpecificationNode node)
    {
        var frame = node.Frame ??
                    throw new VisitorException(
                        nameof(BuildMetadataAndInferTypesVisitor),
                        "Visit(WindowSpecificationNode)",
                        "Window frame validation requires a frame.",
                        DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
                        node.HasSpan ? node.Span : TextSpan.Empty);
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Invalid window frame: start bound '{frame.Start}' is logically after end bound '{frame.End}'.";

        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "Visit(WindowSpecificationNode)",
            message,
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
            span);

        if (TryReportException(exception, node))
            return;

        throw exception;
    }

    private sealed class QualifyWindowFunctionTraverser(IExpressionVisitor visitor)
        : RawTraverseVisitor<IExpressionVisitor>(visitor);

    private sealed class QualifyWindowFunctionDetector : NoOpExpressionVisitor
    {
        public bool Found { get; private set; }

        public override void Visit(WindowFunctionNode node) => Found = true;
    }
}
