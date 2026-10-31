using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

/// <summary>Structural graphs for traversal oracles; these fixtures are not executable query plans.</summary>
internal sealed class AllocationTraversalFixtures(bool includeOptional)
{
    private readonly NullabilityInfoContext _nullability = new();
    private readonly HashSet<Type> _active = [];
    private int _nextName;

    internal object Create(Type type)
    {
        if (type == typeof(ExecutionExpression))
            return new ExecutionLiteral(++_nextName, typeof(int));
        if (type == typeof(ExecutionNode))
            return new ExecutionBreak();
        if (type == typeof(ExecutionVariable))
            return new ExecutionVariable($"reference{++_nextName}", typeof(int));
        if (type == typeof(ExecutionBlock))
            return new ExecutionBlock([new ExecutionBreak(), new ExecutionContinue()]);
        if (type == typeof(RowShape))
            return new GeneratedRowShape($"shape{++_nextName}", []);
        if (type == typeof(ExecutionTypeRef))
            return ExecutionClrBindingFactory.FromClr(typeof(int));
        if (type == typeof(Type))
            return typeof(int);
        if (type == typeof(string))
            return $"name{++_nextName}";
        if (type.IsArray)
            return CreateArray(type.GetElementType()!);
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>))
                return CreateArray(type.GetGenericArguments()[0]);
            if (definition == typeof(IReadOnlyDictionary<,>))
                return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()))!;
            if (definition == typeof(IReadOnlySet<>))
                return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(type.GetGenericArguments()))!;
        }
        if (type.IsValueType)
            return Activator.CreateInstance(type)!;
        if (type.IsAbstract || type.IsInterface || typeof(Delegate).IsAssignableFrom(type))
            return null!;
        if (type.Namespace?.StartsWith("Musoq.", StringComparison.Ordinal) != true)
            return null!;
        if (!_active.Add(type))
            return null!;
        try
        {
            var result = RuntimeHelpers.GetUninitializedObject(type);
            Populate(result, type);
            return result;
        }
        finally
        {
            _active.Remove(type);
        }
    }

    private Array CreateArray(Type element)
    {
        var array = Array.CreateInstance(element, 2);
        array.SetValue(Create(element), 0);
        array.SetValue(Create(element), 1);
        return array;
    }

    private void Populate(object instance, Type type)
    {
        for (var current = type; current != typeof(object) && current is not null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!includeOptional && _nullability.Create(field).ReadState == NullabilityState.Nullable)
                    continue;
                field.SetValue(instance, Create(field.FieldType));
            }
        }
    }

    internal static IEnumerable<object[]> NodeCases() => Cases(typeof(ExecutionNode));
    internal static IEnumerable<object[]> ExpressionCases() => Cases(typeof(ExecutionExpression));

    private static IEnumerable<object[]> Cases(Type parent) => parent.Assembly.GetTypes()
        .Where(type => !type.IsAbstract && parent.IsAssignableFrom(type))
        .SelectMany(type => new[] { new object[] { type, false }, new object[] { type, true } });
}
