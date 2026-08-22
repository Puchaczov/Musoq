using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PortableExecutionIrBaselineTests
{
    [TestMethod]
    public void ExecutionIr_PublicShape_ShouldMatchPortableMigrationBaseline()
    {
        var assembly = typeof(ExecutionPlan).Assembly;
        var concreteTypes = assembly
            .GetTypes()
            .Where(static type =>
                !type.IsAbstract &&
                (typeof(ExecutionNode).IsAssignableFrom(type) ||
                 typeof(ExecutionExpression).IsAssignableFrom(type) ||
                 typeof(RowShape).IsAssignableFrom(type)))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var nodeCount = concreteTypes.Count(static type => typeof(ExecutionNode).IsAssignableFrom(type));
        var expressionCount = concreteTypes.Count(static type => typeof(ExecutionExpression).IsAssignableFrom(type));
        var rowShapeCount = concreteTypes.Count(static type => typeof(RowShape).IsAssignableFrom(type));
        var clrShapedMembers = concreteTypes
            .SelectMany(static type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(static property => IsClrShaped(property.PropertyType))
                .Select(property => $"{type.FullName}.{property.Name}:{FormatType(property.PropertyType)}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var inventory = string.Join(
            "\n",
            concreteTypes.Select(static type => type.FullName)
                .Concat(clrShapedMembers.Select(static member => $"clr:{member}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inventory)));
        Assert.AreEqual(89, nodeCount);
        Assert.AreEqual(38, expressionCount);
        Assert.AreEqual(8, rowShapeCount);
        Assert.AreEqual(0, clrShapedMembers.Length);
        Assert.AreEqual(
            "01EA23C3E6C83BCEFF02D2C9D701395D3A8A7C0FE35A28AF01239B3BA4699E55",
            hash,
            $"Current inventory hash: {hash}");
    }

    [TestMethod]
    public void ExecutionExpressionConverter_RegisteredTypes_ShouldCoverEveryConcreteIrExpression()
    {
        var concreteExpressions = typeof(IrExpression).Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && typeof(IrExpression).IsAssignableFrom(type))
            .ToHashSet();

        CollectionAssert.AreEquivalent(
            concreteExpressions.ToArray(),
            ExecutionExpressionConverter.RegisteredExpressionTypes.ToArray());
    }

    [TestMethod]
    public void ExecutionExpressionConverter_WhenExpressionIsUnregistered_ShouldRejectDeterministically()
    {
        var exception = Assert.ThrowsExactly<NotSupportedException>(
            () => ExecutionExpressionConverter.Convert(new TestUnregisteredExpression()));

        StringAssert.Contains(exception.Message, typeof(TestUnregisteredExpression).FullName!);
        StringAssert.Contains(exception.Message, "no execution expression lowering is registered");
    }

    [TestMethod]
    public void ExecutionPlan_PublicContractGraph_ShouldNotExposeClrSidecarsOrRawExpressions()
    {
        var executionAssembly = typeof(ExecutionPlan).Assembly;
        var pending = new Queue<Type>();
        var visited = new HashSet<Type>();
        var violations = new List<string>();
        EnqueueContractTypes(typeof(ExecutionPlan), executionAssembly, pending);

        while (pending.TryDequeue(out var contractType))
        {
            if (!visited.Add(contractType))
                continue;

            foreach (var property in contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsClrShaped(property.PropertyType))
                {
                    violations.Add(
                        $"{contractType.FullName}.{property.Name}:{FormatType(property.PropertyType)}");
                }

                EnqueueContractTypes(property.PropertyType, executionAssembly, pending);
            }
        }

        Assert.IsEmpty(
            violations,
            "The public ExecutionPlan contract graph must not expose Type, reflection members, Assembly, or object: " +
            string.Join(", ", violations));
        Assert.IsNull(
            executionAssembly.GetType("Musoq.Evaluator.IR.Execution.ExecutionRawExpression"),
            "Raw-expression fallback must not return to portable Execution IR.");

        AssertNoClrSidecar(typeof(ExecutionTypeRef), "ClrType");
        AssertNoClrSidecar(typeof(ExecutionCallableRef), "ClrMethod");
        AssertInternalSidecar(typeof(ExecutionConstantValue), "ClrOnlyValue", typeof(object));
    }

    private static bool IsClrShaped(Type type)
    {
        if (type == typeof(Type) ||
            type == typeof(object) ||
            typeof(MemberInfo).IsAssignableFrom(type) ||
            typeof(Assembly).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.IsArray)
            return IsClrShaped(type.GetElementType()!);

        return type.IsGenericType && type.GetGenericArguments().Any(IsClrShaped);
    }

    private static string FormatType(Type type)
    {
        if (type.IsArray)
            return $"{FormatType(type.GetElementType()!)}[]";

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var definitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
            definitionName = definitionName[..tickIndex];

        return $"{definitionName}<{string.Join(",", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static void EnqueueContractTypes(Type type, Assembly executionAssembly, Queue<Type> pending)
    {
        if (type.IsArray)
        {
            EnqueueContractTypes(type.GetElementType()!, executionAssembly, pending);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                EnqueueContractTypes(argument, executionAssembly, pending);
        }

        if (type.Assembly != executionAssembly ||
            type.Namespace?.StartsWith("Musoq.Evaluator.IR.Execution", StringComparison.Ordinal) != true)
        {
            return;
        }

        pending.Enqueue(type);
        if (!type.IsAbstract && !type.IsInterface)
            return;

        foreach (var implementation in executionAssembly
                     .GetTypes()
                     .Where(candidate => !candidate.IsAbstract && type.IsAssignableFrom(candidate)))
        {
            pending.Enqueue(implementation);
        }
    }

    private static void AssertInternalSidecar(Type declaringType, string propertyName, Type propertyType)
    {
        var property = declaringType.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(property, $"Expected internal sidecar {declaringType.Name}.{propertyName}.");
        Assert.AreEqual(propertyType, property.PropertyType);
        Assert.IsFalse(property.GetMethod!.IsPublic);
    }

    private static void AssertNoClrSidecar(Type declaringType, string propertyName)
    {
        Assert.IsNull(
            declaringType.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            $"Portable descriptor reference '{declaringType.Name}' must not retain a CLR sidecar property '{propertyName}'.");
    }

    private sealed record TestUnregisteredExpression() : IrExpression(typeof(int));
}
