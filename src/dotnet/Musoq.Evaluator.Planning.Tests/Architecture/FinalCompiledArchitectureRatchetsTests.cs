using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Compiled-contract ratchets for the final runtime-v2 boundaries.
/// These checks intentionally inspect loaded types and delegates rather than
/// treating source file names as evidence of ownership.
/// </summary>
[TestClass]
public sealed class FinalCompiledArchitectureRatchetsTests
{
    [TestMethod]
    public void PhysicalBuilder_ShouldRemainACompatibilityFacade()
    {
        var builder = CompilerContractSymbols.For(typeof(PhysicalToExecutionPlanBuilder));
        var buildMethods = builder
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method =>
                SymbolEqualityComparer.Default.Equals(
                    method.DeclaringType,
                    CompilerContractSymbols.For(typeof(PhysicalToExecutionPlanBuilder))
                ) && !method.IsSpecialName
            )
            .ToArray();

        Assert.HasCount(1, buildMethods);
        Assert.AreEqual(nameof(PhysicalToExecutionPlanBuilder.Build), buildMethods[0].Name);
        Assert.IsEmpty(
            builder.GetNestedTypes(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            )
        );
        Assert.IsNull(builder.Assembly.GetType("Musoq.Evaluator.IR.Execution.PhysicalLoweringKernel"));
    }

    [TestMethod]
    public void LoweringRegistry_ShouldExposeOnlyTypedAttemptResults()
    {
        var descriptorTypes = new[]
        {
            CompilerContractSymbols.For(typeof(PhysicalPlanLoweringDescriptor)),
            CompilerContractSymbols.For(typeof(PhysicalTableLoweringDescriptor)),
        };

        var resultTypes = descriptorTypes
            .SelectMany(static type =>
                type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            )
            .SelectMany(static constructor => constructor.GetParameters())
            .Select(static parameter => parameter.ParameterType)
            .Where(static type =>
                type.IsGenericType
                && SymbolEqualityComparer.Default.Equals(
                    type.GetGenericTypeDefinition(),
                    CompilerContractSymbols.For(typeof(Func<,>))
                )
            )
            .Select(static type => type.GetGenericArguments()[1])
            .ToArray();

        Assert.IsNotEmpty(resultTypes);
        Assert.IsTrue(
            resultTypes.All(static type =>
                type.IsGenericType
                && SymbolEqualityComparer.Default.Equals(
                    type.GetGenericTypeDefinition(),
                    CompilerContractSymbols.For(typeof(LoweringAttempt<>))
                )
            )
        );

        var attemptProperties = CompilerContractSymbols
            .For(typeof(LoweringAttempt<>))
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);
        CollectionAssert.AreEquivalent(
            new[]
            {
                nameof(LoweringAttempt<object>.Kind),
                nameof(LoweringAttempt<object>.Value),
                nameof(LoweringAttempt<object>.UnsupportedReason),
                nameof(LoweringAttempt<object>.IsTerminal),
                nameof(LoweringAttempt<object>.IsBuilt),
                nameof(LoweringAttempt<object>.IsUnsupported),
            },
            attemptProperties.Select(static property => property.MetadataName).ToArray()
        );
    }

    [TestMethod]
    public void ExecutionNodes_ShouldBeExhaustivelyRegistered()
    {
        var assembly = CompilerContractSymbols.For(typeof(ExecutionNode)).Assembly;
        var concreteNodes = assembly
            .GetTypes()
            .Where(static type =>
                type is { IsClass: true, IsAbstract: false }
                && CompilerContractSymbols.For(typeof(ExecutionNode)).IsAssignableFrom(type)
            )
            .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var registeredNodes = ExecutionNodeDefinitionCatalog
            .Definitions.Select(static definition => CompilerContractSymbols.For(definition.NodeType))
            .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        var missing = concreteNodes
            .Except<ITypeSymbol>(registeredNodes, SymbolEqualityComparer.Default)
            .Select(static type => type.FullName)
            .OrderBy(static name => name)
            .ToArray();
        var stale = registeredNodes
            .Except<ITypeSymbol>(concreteNodes, SymbolEqualityComparer.Default)
            .Select(static type => type.FullName)
            .OrderBy(static name => name)
            .ToArray();

        Assert.IsEmpty(missing, $"Concrete execution nodes without definitions: {string.Join(", ", missing)}");
        Assert.IsEmpty(stale, $"Node definitions without concrete types: {string.Join(", ", stale)}");
        Assert.HasCount(registeredNodes.Count, ExecutionNodeRegistry.Descriptors);
    }

    [TestMethod]
    public void ExecutionNodeDefinitions_ShouldHaveUniqueIdsAndExecutableBehavior()
    {
        var definitions = ExecutionNodeDefinitionCatalog.Definitions;
        var ids = definitions.Select(static definition => definition.OperationId.Value).ToArray();

        Assert.HasCount(ids.Length, ids.Distinct(StringComparer.Ordinal).ToArray());
        Assert.IsTrue(ids.All(static id => !string.IsNullOrWhiteSpace(id)));
        Assert.IsTrue(
            definitions.All(static definition =>
                definition.Behavior.Printer is not null
                && definition.Behavior.Rewriter is not null
                && Enum.IsDefined(definition.Behavior.TargetCapability)
            )
        );
    }

    [TestMethod]
    public void OperatorCatalog_ShouldCarryTheRegisteredOperationForEachPlanNode()
    {
        var table = new ExecutionCreateTable(
            new ExecutionVariable("results", typeof(Table)),
            new GeneratedRowShape("ResultRow", [])
        );
        var returned = new ExecutionReturnTable(new ExecutionVariable("results", typeof(Table)));
        var plan = new ExecutionPlan("catalog", [], new ExecutionBlock([table, returned]));

        var catalog = ExecutionPlanOperatorCatalog.Create(plan);

        Assert.IsTrue(catalog.TryGetDescriptor(table, out var tableDescriptor));
        Assert.IsTrue(catalog.TryGetDescriptor(returned, out var returnDescriptor));
        Assert.AreEqual(ExecutionOperationCatalog.Resolve(table).Value, tableDescriptor.OperationId);
        Assert.AreEqual(ExecutionOperationCatalog.Resolve(returned).Value, returnDescriptor.OperationId);
        Assert.IsNull(ExecutionPlanOperatorCatalog.Create("ReturnTable").Operators.Single().OperationId);
    }

    [TestMethod]
    public void PortableDescriptors_ShouldNotCarryClrBindingMembers()
    {
        foreach (
            var descriptor in new[]
            {
                CompilerContractSymbols.For(typeof(ExecutionTypeRef)),
                CompilerContractSymbols.For(typeof(ExecutionCallableRef)),
            }
        )
        {
            var members = descriptor
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(static member =>
                    member.Name.Contains("ResolveClr", StringComparison.Ordinal)
                    || member.Name.Contains("FromClr", StringComparison.Ordinal)
                    || member.Name is "ClrType" or "ClrMethod"
                )
                .ToArray();

            Assert.IsEmpty(
                members.Select(static member => member.Name).ToArray(),
                $"Portable descriptor {descriptor.FullName} exposes CLR binding members."
            );
        }
    }

    [TestMethod]
    public void RuntimeContracts_ShouldExposeContextualRunsAndOwnedLifetime()
    {
        var contextRun = CompilerContractSymbols
            .For(typeof(IContextTableRunnable))
            .GetMethod(nameof(IContextTableRunnable.Run));
        var asyncContextRun = CompilerContractSymbols
            .For(typeof(IContextAsyncTableRunnable))
            .GetMethod(nameof(IContextAsyncTableRunnable.RunAsync));

        Assert.IsNotNull(contextRun);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(QueryRunContext)),
                contextRun!.GetParameters().Single().ParameterType
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(Table)), contextRun.ReturnType)
        );
        Assert.IsNotNull(asyncContextRun);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(QueryRunContext)),
                asyncContextRun!.GetParameters().Single().ParameterType
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols
                    .For(typeof(System.Threading.Tasks.ValueTask<>))
                    .Construct(CompilerContractSymbols.For(typeof(Table))),
                asyncContextRun.ReturnType
            )
        );
        Assert.IsTrue(
            CompilerContractSymbols
                .For(typeof(IDisposable))
                .IsAssignableFrom(CompilerContractSymbols.For(typeof(CompiledQuery)))
        );
        Assert.IsTrue(
            CompilerContractSymbols
                .For(typeof(IDisposable))
                .IsAssignableFrom(CompilerContractSymbols.For(typeof(EvaluatorRuntimeEnvironment)))
        );
    }

    [TestMethod]
    public void RuntimeCaches_ShouldNotUseStrongTypeKeysForNamedCaches()
    {
        var cacheFields = CompilerContractSymbols
            .For(typeof(EvaluationHelper))
            .Assembly.GetTypes()
            .SelectMany(static type =>
                type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => (type, field))
            )
            .Where(static item =>
                item.field.Name.Contains("Cache", StringComparison.Ordinal)
                || item.field.Name.Contains("Adapters", StringComparison.Ordinal)
            )
            .ToArray();

        var typeKeyedStrongCaches = cacheFields
            .Where(static item => ContainsStrongTypeKey(item.field.FieldType))
            .Select(static item => $"{item.type.FullName}.{item.field.Name}")
            .ToArray();

        Assert.IsEmpty(
            typeKeyedStrongCaches,
            $"Strong type-keyed runtime caches: {string.Join(", ", typeKeyedStrongCaches)}"
        );
    }

    private static bool ContainsStrongTypeKey(ITypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(type, CompilerContractSymbols.For(typeof(Type))))
            return true;

        if (type.IsArray || type.IsPointer)
            return ContainsStrongTypeKey(type.GetElementType()!);

        return type.IsGenericType && type.GetGenericArguments().Any(ContainsStrongTypeKey);
    }
}
