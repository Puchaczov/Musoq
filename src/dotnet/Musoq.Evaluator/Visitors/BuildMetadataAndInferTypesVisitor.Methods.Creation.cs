using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static MethodInfo ProcessGenericMethodIfNeeded(MethodInfo method, ArgsListNode args, Type entityType)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregateFunctionAttribute>() != null;

        if (!isAggregateMethod && method.IsGenericMethod && TryReduceDimensions(method, args, out var reducedMethod))
            method = reducedMethod;

        if (!isAggregateMethod &&
            method is { IsGenericMethod: true, IsConstructedGenericMethod: false } &&
            TryConstructGenericMethod(method, args, entityType, out var constructedMethod))
            method = constructedMethod;

        return method;
    }

    private AccessMethodNode CreateAccessMethod(
        AccessMethodNode node,
        ArgsListNode args,
        MethodInfo method,
        MethodResolutionContext context,
        bool canSkipInjectSource,
        Func<FunctionToken, ArgsListNode, ArgsListNode?, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var isAggregateMethod = method.GetCustomAttribute<AggregateFunctionAttribute>() != null;

        if (isAggregateMethod && _queryState.QueryPart == QueryPart.Where)
            throw new CannotResolveMethodException(
                $"Aggregate method {node.Name} cannot be used in WHERE. Use HAVING for aggregate predicates.",
                node.SpanOrEmpty());

        if (node is { HasFilter: true, IsPivotGenerated: true } && !isAggregateMethod)
            ThrowPivotUsingOnNonAggregate(node);
        else if (node.HasFilter && !isAggregateMethod)
            ThrowFilterOnNonAggregate(node);

        if (isAggregateMethod) return ProcessAggregateMethod(node, args, method, context, func);

        return func(node.FunctionToken, args, new ArgsListNode([]), method, context.Alias, canSkipInjectSource);
    }

    private AccessMethodNode ProcessAggregateMethod(
        AccessMethodNode node,
        ArgsListNode args,
        MethodInfo method,
        MethodResolutionContext context,
        Func<FunctionToken, ArgsListNode, ArgsListNode?, MethodInfo, string, bool, AccessMethodNode> func)
    {
        if (!IsAggregateDeclarationMethod(method))
            throw new CannotResolveMethodException(
                $"Aggregate method {node.Name} must declare a typed AggregateFunctionAttribute.",
                node.SpanOrEmpty());

        return ProcessAggregateDeclarationMethod(node, args, method, context, func);
    }

    private static bool TryResolveAggregateDeclarationMethod(
        string methodName,
        Type[] argTypes,
        ArgsListNode args,
        MethodResolutionContext context,
        [NotNullWhen(true)] out MethodInfo? method)
    {
        if (TryResolveWildcardAggregateAsArgumentless(methodName, args, context, out method))
            return true;

        if (context.SchemaTablePair.Schema.TryResolveAggregationMethod(
                methodName,
                argTypes,
                context.EntityType,
                candidate => IsAggregateDeclarationMethod(candidate) &&
                             CanUseAggregateDeclarationForArguments(candidate, args),
                out method))
        {
            return true;
        }

        method = null;
        return false;
    }

    private static bool TryResolveWildcardAggregateAsArgumentless(
        string methodName,
        ArgsListNode args,
        MethodResolutionContext context,
        [NotNullWhen(true)] out MethodInfo? method)
    {
        if (args.Args is [AllColumnsNode] &&
            context.SchemaTablePair.Schema.TryResolveAggregationMethod(
                methodName,
                Type.EmptyTypes,
                context.EntityType,
                IsAggregateDeclarationMethod,
                out method))
        {
            return true;
        }

        method = null;
        return false;
    }

    private static bool CanUseAggregateDeclarationForArguments(MethodInfo method, ArgsListNode args)
    {
        var parameters = method.GetParameters();
        var parentParameterIndex = Array.FindIndex(
            parameters,
            static parameter => parameter.GetCustomAttribute<AggregateParentAttribute>() is not null);

        if (parentParameterIndex < 0 || parentParameterIndex >= args.Args.Length)
            return true;

        return args.Args[parentParameterIndex] is IntegerNode integer &&
               Convert.ToInt64(integer.ObjValue, System.Globalization.CultureInfo.InvariantCulture) >= 0L;
    }

    private AccessMethodNode ProcessAggregateDeclarationMethod(
        AccessMethodNode node,
        ArgsListNode args,
        MethodInfo method,
        MethodResolutionContext context,
        Func<FunctionToken, ArgsListNode, ArgsListNode?, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var accessMethod = func(node.FunctionToken, args, node.ExtraAggregateArguments, method, context.Alias, false);
        accessMethod.HasFilter = node.HasFilter;
        accessMethod.FilterExpression = node.FilterExpression;
        accessMethod.FilterExpressionText = node.FilterExpressionText;
        accessMethod.IsPivotGenerated = node.IsPivotGenerated;
        accessMethod.IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper;
        var identifier = CreateAggregateIdentifier(accessMethod, node.IsDistinct);
        var displayName = CreateAggregateDisplayName(accessMethod, node.IsDistinct);
        var refreshArgs = new List<Node> { new AggregateIdentifierNode(identifier, displayName) };
        refreshArgs.AddRange(GetAggregateDeclarationValueArguments(method, args));

        var refreshMethodNode = func(
            new FunctionToken(method.Name, TextSpan.Empty),
            new ArgsListNode(refreshArgs.ToArray()),
            null,
            method,
            context.Alias,
            false);
        refreshMethodNode.HasFilter = node.HasFilter;
        refreshMethodNode.FilterExpression = node.FilterExpression;
        refreshMethodNode.FilterExpressionText = node.FilterExpressionText;
        refreshMethodNode.IsPivotGenerated = node.IsPivotGenerated;
        refreshMethodNode.IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper;

        _methodResolution.RefreshMethods.Add(refreshMethodNode);
        var result = func(
            node.FunctionToken,
            new ArgsListNode(CreateAggregateDeclarationResultArguments(method, args, identifier, displayName)),
            null,
            method,
            context.Alias,
            false);
        result.MarkAsAggregate();
        return result;
    }

    private static Node[] CreateAggregateDeclarationResultArguments(
        MethodInfo method,
        ArgsListNode args,
        string identifier,
        string displayName)
    {
        if (!TryGetAggregateDeclarationParentArgument(method, args, out var parentArgument))
            return [new AggregateIdentifierNode(identifier, displayName)];

        return [new AggregateIdentifierNode(identifier, displayName), parentArgument];
    }

    private static IEnumerable<Node> GetAggregateDeclarationValueArguments(MethodInfo method, ArgsListNode args)
    {
        var parameters = method.GetParameters();
        for (var index = 0; index < parameters.Length && index < args.Args.Length; index++)
        {
            if (parameters[index].GetCustomAttribute<AggregateParentAttribute>() is null)
                yield return args.Args[index];
        }
    }

    private static bool TryGetAggregateDeclarationParentArgument(
        MethodInfo method,
        ArgsListNode args,
        [NotNullWhen(true)] out Node? parentArgument)
    {
        var parameters = method.GetParameters();
        for (var index = 0; index < parameters.Length && index < args.Args.Length; index++)
        {
            if (parameters[index].GetCustomAttribute<AggregateParentAttribute>() is not null)
            {
                parentArgument = args.Args[index];
                return true;
            }
        }

        parentArgument = null;
        return false;
    }

    private static bool IsAggregateDeclarationMethod(MethodInfo method)
    {
        return method.GetCustomAttribute<AggregateFunctionAttribute>() is not null;
    }

    private static string CreateAggregateIdentifier(AccessMethodNode accessMethod, bool isDistinct)
    {
        return AggregateCallIdentity.Create(accessMethod, isDistinct);
    }

    private static string CreateAggregateDisplayName(AccessMethodNode accessMethod, bool isDistinct)
    {
        var identifier = accessMethod.ToString();

        if (isDistinct)
        {
            var argumentsStart = identifier.IndexOf('(', StringComparison.Ordinal);
            if (argumentsStart >= 0 && argumentsStart < identifier.Length - 1)
                identifier = $"{identifier[..(argumentsStart + 1)]}distinct {identifier[(argumentsStart + 1)..]}";
        }

        if (accessMethod.FilterExpression == null)
            return identifier;

        var filterExpressionText = !string.IsNullOrWhiteSpace(accessMethod.FilterExpressionText)
            ? accessMethod.FilterExpressionText
            : accessMethod.FilterExpression.Id;

        return $"{identifier} filter (where {filterExpressionText})";
    }

    private void FinalizeMethodVisit(MethodInfo method, AccessMethodNode accessMethod)
    {
        if (method.DeclaringType == null)
            throw new InvalidOperationException("Method must have a declaring type.");

        AddAssembly(method.DeclaringType.Assembly);
        AddAssembly(method.ReturnType.Assembly);

        PushSemanticNode(accessMethod);
    }
}
