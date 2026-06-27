using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool TryCreateAggregateLibraries(
        IReadOnlyList<AggregateBinding> bindings,
        string resultTableName,
        out IReadOnlyDictionary<Type, ExecutionVariable> libraries,
        out IReadOnlyList<ExecutionNode> nodes,
        out string unsupportedReason)
    {
        var libraryNodes = new List<ExecutionNode>();
        var librariesByType = new Dictionary<Type, ExecutionVariable>();

        foreach (var type in bindings
                     .SelectMany(EnumerateAggregateLibraryTypes)
                     .Distinct())
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                libraries = new Dictionary<Type, ExecutionVariable>();
                nodes = [];
                unsupportedReason = $"Execution IR aggregate lowering cannot instantiate aggregate library {type.FullName}.";
                return false;
            }

            var library = new ExecutionVariable(
                CreateScopedAggregateName(
                    resultTableName,
                    CreateAggregateLibraryIdentifier(type, librariesByType.Count)),
                type);
            librariesByType[type] = library;
            libraryNodes.Add(new ExecutionCreateAggregateLibrary(library, type));
        }

        libraries = librariesByType;
        nodes = libraryNodes;
        unsupportedReason = string.Empty;
        return true;
    }

    private static IEnumerable<Type> EnumerateAggregateLibraryTypes(AggregateBinding binding)
    {
        foreach (var argument in binding.SetArguments)
        {
            foreach (var type in EnumerateReusableMethodTargetTypes(argument))
                yield return type;
        }
    }

    private static IEnumerable<Type> EnumerateReusableMethodTargetTypes(IrExpression expression)
    {
        switch (expression)
        {
            case MethodCall methodCall:
                if (ExecutionMethodTargetReuse.TryGetReusableTargetType(methodCall.Method, out var targetType))
                    yield return targetType;

                foreach (var argument in methodCall.Arguments)
                {
                    foreach (var type in EnumerateReusableMethodTargetTypes(argument))
                        yield return type;
                }
                break;
            case StrictCast strictCast:
                if (StrictCastLibraryConversionFacts.NeedsLibraryTarget(strictCast.Expression.ReturnType, strictCast.ReturnType))
                    yield return typeof(Musoq.Plugins.LibraryBase);
                foreach (var type in EnumerateReusableMethodTargetTypes(strictCast.Expression))
                    yield return type;
                break;
            case BinaryOp binary:
                foreach (var type in EnumerateReusableMethodTargetTypes(binary.Left))
                    yield return type;
                foreach (var type in EnumerateReusableMethodTargetTypes(binary.Right))
                    yield return type;
                break;
            case UnaryOp unary:
                foreach (var type in EnumerateReusableMethodTargetTypes(unary.Operand))
                    yield return type;
                break;
            case ArrayAccess arrayAccess:
                foreach (var type in EnumerateReusableMethodTargetTypes(arrayAccess.Array))
                    yield return type;
                foreach (var type in EnumerateReusableMethodTargetTypes(arrayAccess.Index))
                    yield return type;
                break;
            case IsNullCheck isNull:
                foreach (var type in EnumerateReusableMethodTargetTypes(isNull.Expression))
                    yield return type;
                break;
            case InCheck inCheck:
                foreach (var type in EnumerateReusableMethodTargetTypes(inCheck.Expression))
                    yield return type;
                foreach (var value in inCheck.Values)
                {
                    foreach (var type in EnumerateReusableMethodTargetTypes(value))
                        yield return type;
                }
                break;
            case PatternMatch patternMatch:
                foreach (var type in EnumerateReusableMethodTargetTypes(patternMatch.Expression))
                    yield return type;
                foreach (var type in EnumerateReusableMethodTargetTypes(patternMatch.Pattern))
                    yield return type;
                break;
            case Between between:
                foreach (var type in EnumerateReusableMethodTargetTypes(between.Expression))
                    yield return type;
                foreach (var type in EnumerateReusableMethodTargetTypes(between.Low))
                    yield return type;
                foreach (var type in EnumerateReusableMethodTargetTypes(between.High))
                    yield return type;
                break;
            case CaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    foreach (var type in EnumerateReusableMethodTargetTypes(branch.Condition))
                        yield return type;
                    foreach (var type in EnumerateReusableMethodTargetTypes(branch.Result))
                        yield return type;
                }
                if (caseWhen.ElseExpression != null)
                {
                    foreach (var type in EnumerateReusableMethodTargetTypes(caseWhen.ElseExpression))
                        yield return type;
                }
                break;
            case Coalesce coalesce:
                foreach (var value in coalesce.Expressions)
                {
                    foreach (var type in EnumerateReusableMethodTargetTypes(value))
                        yield return type;
                }
                break;
        }
    }
    private static string CreateAggregateLibraryIdentifier(Type type, int ordinal)
    {
        var baseName = type.Name;
        var genericMarkerIndex = baseName.IndexOf('`', StringComparison.Ordinal);
        if (genericMarkerIndex >= 0)
            baseName = baseName[..genericMarkerIndex];
        if (string.IsNullOrWhiteSpace(baseName))
            return $"aggregateLibrary{ordinal.ToString(CultureInfo.InvariantCulture)}";
        return $"{char.ToLowerInvariant(baseName[0])}{baseName[1..]}{ordinal.ToString(CultureInfo.InvariantCulture)}";
    }
    private static ExecutionVariable CreateAggregateVariable(string resultTableName, string baseName, Type type)
    {
        return new ExecutionVariable(CreateScopedAggregateName(resultTableName, baseName), type);
    }
    private static ExecutionVariable CreateAggregateGroupVariable(string resultTableName, string baseName)
    {
        return CreateAggregateVariable(resultTableName, baseName, typeof(object));
    }
    private static string CreateAggregateScopeName(string resultTableName, bool scopeAggregateVariables)
    {
        return scopeAggregateVariables ? resultTableName : DefaultAggregateScopeName;
    }
    private static string CreateScopedAggregateName(string resultTableName, string baseName)
    {
        if (string.Equals(resultTableName, DefaultAggregateScopeName, StringComparison.Ordinal))
            return baseName;
        if (string.IsNullOrWhiteSpace(baseName))
            return resultTableName;
        return $"{resultTableName}{char.ToUpperInvariant(baseName[0])}{baseName[1..]}";
    }
    private static string CreateScopedHashName(string resultTableName, string baseName)
    {
        return CreateScopedAggregateName(resultTableName, baseName);
    }
}
