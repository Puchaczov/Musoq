using Microsoft.CodeAnalysis;
using Musoq.Evaluator.Tests.Architecture;
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
        var assembly = CompilerContractSymbols.For(typeof(ExecutionPlan)).Assembly;
        var concreteTypes = assembly
            .GetTypes()
            .Where(static type =>
                !type.IsAbstract &&
                (CompilerContractSymbols.For(typeof(ExecutionNode)).IsAssignableFrom(type) ||
                 CompilerContractSymbols.For(typeof(ExecutionExpression)).IsAssignableFrom(type) ||
                 CompilerContractSymbols.For(typeof(RowShape)).IsAssignableFrom(type)))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var nodeCount = concreteTypes.Count(static type => CompilerContractSymbols.For(typeof(ExecutionNode)).IsAssignableFrom(type));
        var expressionCount = concreteTypes.Count(static type => CompilerContractSymbols.For(typeof(ExecutionExpression)).IsAssignableFrom(type));
        var rowShapeCount = concreteTypes.Count(static type => CompilerContractSymbols.For(typeof(RowShape)).IsAssignableFrom(type));
        var clrShapedMembers = concreteTypes
            .SelectMany(static type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(static property => IsClrShaped(property.PropertyType))
                .Select(property => $"{type.FullName}.{property.MetadataName}:{FormatType(property.PropertyType)}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var inventory = string.Join(
            "\n",
            concreteTypes.Select(static type => type.FullName)
                .Concat(clrShapedMembers.Select(static member => $"clr:{member}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inventory)));
        var originalInventory = string.Join("\n", concreteTypes
            .Where(type => type.Name != nameof(ExecutionByteBufferOperation) && type.Name != nameof(ExecutionByteBufferArgument))
            .Select(static type => type.FullName)
            .Concat(clrShapedMembers.Select(static member => $"clr:{member}")));
        var originalHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(originalInventory)));
        Assert.AreEqual(89, nodeCount);
        Assert.AreEqual(54, concreteTypes.Count(type => type.Name != nameof(ExecutionByteBufferOperation) && type.Name != nameof(ExecutionByteBufferArgument) &&
            CompilerContractSymbols.For(typeof(ExecutionExpression)).IsAssignableFrom(type)));
        Assert.AreEqual(56, expressionCount);
        Assert.AreEqual(7, rowShapeCount);
        Assert.AreEqual(0, clrShapedMembers.Length);
        Assert.AreEqual(
            "29967EDF7730AA20AFECE0AC6AB1C86CC153E2835ECDEA2A73D1DDB9738FB9FD",
            originalHash);
        Assert.AreEqual(
            "C3E8ADC8114158ACB8298ED1DB4604B0B354361B21A9268B4289B28806D25E37",
            hash,
            $"Current inventory hash: {hash}");
    }

    [TestMethod]
    public void ExecutionExpressionConverter_RegisteredTypes_ShouldCoverEveryConcreteIrExpression()
    {
        var concreteExpressions = CompilerContractSymbols.For(typeof(IrExpression)).Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && CompilerContractSymbols.For(typeof(IrExpression)).IsAssignableFrom(type))
            .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        CollectionAssert.AreEquivalent(
            concreteExpressions.ToArray(),
            ExecutionExpressionConverter.RegisteredExpressionTypes.Select(CompilerContractSymbols.For).ToArray());
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
        var executionAssembly = CompilerContractSymbols.For(typeof(ExecutionPlan)).Assembly;
        var pending = new Queue<ITypeSymbol>();
        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var violations = new List<string>();
        EnqueueContractTypes(CompilerContractSymbols.For(typeof(ExecutionPlan)), executionAssembly, pending);

        while (pending.TryDequeue(out var contractType))
        {
            if (!visited.Add(contractType))
                continue;

            foreach (var property in contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsClrShaped(property.PropertyType))
                {
                    violations.Add(
                        $"{contractType.FullName}.{property.MetadataName}:{FormatType(property.PropertyType)}");
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

        AssertNoClrSidecar(CompilerContractSymbols.For(typeof(ExecutionTypeRef)), "ClrType");
        AssertNoClrSidecar(CompilerContractSymbols.For(typeof(ExecutionCallableRef)), "ClrMethod");
        AssertInternalSidecar(CompilerContractSymbols.For(typeof(ExecutionConstantValue)), "ClrOnlyValue", CompilerContractSymbols.For(typeof(object)));
    }

    private static bool IsClrShaped(ITypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(type, CompilerContractSymbols.For(typeof(Type))) ||
            SymbolEqualityComparer.Default.Equals(type, CompilerContractSymbols.For(typeof(object))) ||
            CompilerContractSymbols.For(typeof(MemberInfo)).IsAssignableFrom(type) ||
            CompilerContractSymbols.For(typeof(Assembly)).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.IsArray)
            return IsClrShaped(type.GetElementType()!);

        return type.IsGenericType && type.GetGenericArguments().Any(IsClrShaped);
    }

    private static string FormatType(ITypeSymbol type)
    {
        if (type.IsArray)
            return $"{FormatType(type.GetElementType()!)}[]";

        if (!type.IsGenericType)
            return type.FullName ?? type.MetadataName;

        var definitionName = type.GetGenericTypeDefinition().FullName ?? type.MetadataName;
        var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
            definitionName = definitionName[..tickIndex];

        return $"{definitionName}<{string.Join(",", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static void EnqueueContractTypes(ITypeSymbol type, IAssemblySymbol executionAssembly, Queue<ITypeSymbol> pending)
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

        if (!SymbolEqualityComparer.Default.Equals(type.Assembly, executionAssembly) ||
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

    private static void AssertInternalSidecar(ITypeSymbol declaringType, string propertyName, ITypeSymbol propertyType)
    {
        var property = declaringType.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(property, $"Expected internal sidecar {declaringType.Name}.{propertyName}.");
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(propertyType, property.PropertyType));
        Assert.IsFalse(property.GetMethod!.IsPublic);
    }

    private static void AssertNoClrSidecar(ITypeSymbol declaringType, string propertyName)
    {
        Assert.IsNull(
            declaringType.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            $"Portable descriptor reference '{declaringType.Name}' must not retain a CLR sidecar property '{propertyName}'.");
    }

    private sealed record TestUnregisteredExpression() : IrExpression(typeof(int));
}
