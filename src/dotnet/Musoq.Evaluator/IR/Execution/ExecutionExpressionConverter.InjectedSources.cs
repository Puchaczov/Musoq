using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    public static ExecutionExpression? CreateInjectedSourceExpression(
        MethodInfo method,
        string? alias,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(sourceShapes);
        var parameter = FindInjectedSourceParameter(method);
        if (parameter == null)
            return null;

        if (!string.IsNullOrWhiteSpace(alias))
        {
            var aliasedExpression = CreateAliasedInjectedSourceExpression(alias, parameter.ParameterType, sourceShapes);
            if (aliasedExpression != null)
                return aliasedExpression;
        }

        return CreateUniqueCompatibleInjectedSourceExpression(parameter.ParameterType, sourceShapes);
    }

    private static ParameterInfo? FindInjectedSourceParameter(MethodInfo method)
    {
        return method.GetParameters()
            .FirstOrDefault(static parameter => parameter.GetCustomAttributes(true)
                .OfType<InjectTypeAttribute>()
                .Any(static attribute => IsSourceInjectionAttribute(attribute)));
    }

    private static bool IsSourceInjectionAttribute(InjectTypeAttribute attribute)
    {
        return attribute.GetType().Name is nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute";
    }

    private static ExecutionExpression? CreateAliasedInjectedSourceExpression(
        string alias,
        Type parameterType,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (sourceShapes.TryGetValue(alias, out var expandoShape) &&
            expandoShape is ExpandoAdapterShape expando &&
            CanUseSourceType(expando.RuntimeType.ClrType, parameterType))
        {
            return new ExecutionVariableRead(new ExecutionVariable(
                CreateResolverVariableName(alias),
                expando.RuntimeType));
        }

        if (sourceShapes.TryGetValue(alias, out var shape) && shape is not TableRowShape)
            return new ExecutionVariableRead(new ExecutionVariable(alias, RowShapeLookup.ResolveSourceRuntimeType(shape)));

        foreach (var tableRow in sourceShapes.Values.OfType<TableRowShape>())
        {
            var context = FindContextBinding(tableRow, alias, parameterType);
            if (context != null)
                return new ExecutionFieldRead(tableRow.Alias, context.Name, context.Type, context.AccessStrategy);
        }

        return null;
    }

    private static ExecutionExpression? CreateUniqueCompatibleInjectedSourceExpression(
        Type parameterType,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var candidates = sourceShapes.Values
            .SelectMany(shape => CreateInjectedSourceCandidates(shape, parameterType))
            .Take(2)
            .ToArray();

        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static IEnumerable<ExecutionExpression> CreateInjectedSourceCandidates(
        RowShape shape,
        Type parameterType)
    {
        if (shape is ExpandoAdapterShape expando)
        {
            if (CanUseSourceType(expando.RuntimeType.ClrType, parameterType))
            {
                yield return new ExecutionVariableRead(new ExecutionVariable(
                    CreateResolverVariableName(expando.Alias),
                    expando.RuntimeType));
            }

            yield break;
        }

        if (shape is TableRowShape tableRow)
        {
            foreach (var context in tableRow.Contexts.Where(context => CanUseContextBinding(context, parameterType)))
                yield return new ExecutionFieldRead(tableRow.Alias, context.Name, context.Type, context.AccessStrategy);

            yield break;
        }

        var runtimeType = RowShapeLookup.ResolveSourceRuntimeType(shape);
        if (CanUseSourceType(runtimeType, parameterType) &&
            RowShapeLookup.TryResolveSourceAlias(shape, out var alias))
        {
            yield return new ExecutionVariableRead(new ExecutionVariable(alias, runtimeType));
        }
    }

    private static FieldBinding? FindContextBinding(
        TableRowShape tableRow,
        string alias,
        Type parameterType)
    {
        return tableRow.Contexts.FirstOrDefault(context =>
            IsContextAlias(context, alias) &&
            CanUseContextBinding(context, parameterType));
    }

    private static bool IsContextAlias(FieldBinding context, string alias)
    {
        return string.Equals(context.Name, alias, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(context.QualifiedName, alias, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanUseContextBinding(FieldBinding context, Type parameterType)
    {
        return context.AccessStrategy is ContextAccess or GeneratedFieldAccess or GeneratedRowContextAccess &&
               CanUseSourceType(context.Type.ClrType, parameterType);
    }

    private static bool CanUseSourceType(Type sourceType, Type parameterType)
    {
        return parameterType == typeof(object) ||
               parameterType.IsAssignableFrom(sourceType);
    }

    private static string CreateResolverVariableName(string alias)
    {
        return $"{alias}Resolver";
    }
}
