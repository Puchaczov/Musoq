using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.CSharpClr;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionTypeRefTests
{
    [TestMethod]
    public void FromClr_WhenCreatedRepeatedly_ShouldExposeStablePortableIdentity()
    {
        var first = ExecutionTypeRef.FromClr(typeof(Dictionary<string, int?>));
        var second = ExecutionTypeRef.FromClr(typeof(Dictionary<string, int?>));

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.StableId, second.StableId);
        Assert.AreEqual(typeof(Dictionary<string, int?>), first.ClrType);
        Assert.DoesNotContain(", Version=", first.StableId);
    }

    [TestMethod]
    public void CSharpCompatibility_WhenTypeRefIsProvided_ShouldRequireClrSidecar()
    {
        var typeRef = ExecutionTypeRef.FromClr(typeof(decimal?));

        Assert.AreEqual(typeof(decimal?), typeRef.RequireClrType());
    }

    [TestMethod]
    public void ExpressionAndVariablePublicSurface_ShouldNotExposeClrType()
    {
        var expressionTypes = typeof(ExecutionExpression).Assembly
            .GetTypes()
            .Where(static type => typeof(ExecutionExpression).IsAssignableFrom(type))
            .Append(typeof(ExecutionVariable))
            .ToArray();

        var propertyOffenders = expressionTypes
            .SelectMany(static type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(static property => IsReflectionType(property.PropertyType))
            .Select(static property => $"{property.DeclaringType?.FullName}.{property.Name}")
            .ToArray();
        var constructorOffenders = expressionTypes
            .SelectMany(static type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .Where(static constructor => constructor.GetParameters().Any(parameter => IsReflectionType(parameter.ParameterType)))
            .Select(static constructor => constructor.ToString())
            .ToArray();

        Assert.IsEmpty(propertyOffenders, string.Join(Environment.NewLine, propertyOffenders));
        Assert.IsEmpty(constructorOffenders, string.Join(Environment.NewLine, constructorOffenders));
    }

    [TestMethod]
    public void ExecutionPlanPublicContractGraph_ShouldNotExposeClrTypeOrAssembly()
    {
        var assembly = typeof(ExecutionPlan).Assembly;
        var visited = new HashSet<Type>();
        var pending = new Queue<(Type Type, string Path)>();
        var offenders = new List<string>();
        pending.Enqueue((typeof(ExecutionPlan), nameof(ExecutionPlan)));

        while (pending.Count > 0)
        {
            var (candidate, path) = pending.Dequeue();

            if (IsReflectionType(candidate))
            {
                offenders.Add(path);
                continue;
            }

            if (candidate.Assembly != assembly ||
                candidate.Namespace?.StartsWith("Musoq.Evaluator.IR.Execution", StringComparison.Ordinal) != true ||
                !visited.Add(candidate))
            {
                continue;
            }

            if (candidate.IsAbstract || candidate.IsInterface)
            {
                foreach (var implementation in assembly.GetTypes()
                             .Where(type => type.IsPublic && !type.IsAbstract && candidate.IsAssignableFrom(type)))
                {
                    pending.Enqueue((implementation, $"{path}->{implementation.Name}"));
                }
            }

            foreach (var property in candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var propertyType in ExpandContractTypes(property.PropertyType))
                    pending.Enqueue((propertyType, $"{path}.{property.Name}"));
            }

            foreach (var constructor in candidate.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    foreach (var parameterType in ExpandContractTypes(parameter.ParameterType))
                        pending.Enqueue((parameterType, $"{path}.ctor({parameter.Name})"));
                }
            }
        }

        Assert.IsEmpty(offenders, string.Join(Environment.NewLine, offenders));
    }

    private static IEnumerable<Type> ExpandContractTypes(Type type)
    {
        if (type.IsArray)
        {
            yield return type.GetElementType()!;
            yield break;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                yield return argument;

            yield break;
        }

        yield return type;
    }

    private static bool IsReflectionType(Type type) =>
        type == typeof(Type) ||
        typeof(MemberInfo).IsAssignableFrom(type) ||
        type == typeof(Assembly);
}
