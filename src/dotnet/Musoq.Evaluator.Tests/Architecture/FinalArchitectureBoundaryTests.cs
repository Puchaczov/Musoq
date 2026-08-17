using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class FinalArchitectureBoundaryTests
{
    [TestMethod]
    public void PhysicalLoweringFacade_ShouldOwnCompositionAndBoundedDispatchInventory()
    {
        var builderField = typeof(PhysicalToExecutionPlanBuilder).GetField(
            "_physicalLoweringFacade",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(builderField);
        Assert.AreEqual(typeof(PhysicalLoweringFacade), builderField!.FieldType);

        var facadeMethods = typeof(PhysicalLoweringFacade)
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.DeclaringType == typeof(PhysicalLoweringFacade) && !method.IsSpecialName)
            .ToArray();
        Assert.IsLessThanOrEqualTo(3, facadeMethods.Length);

        var dispatchFacade = new PhysicalLoweringDispatchFacade(CreateNoOpHandlers());
        var registryField = typeof(PhysicalLoweringDispatchFacade).GetField("_registry", BindingFlags.Instance | BindingFlags.NonPublic);
        var registry = registryField?.GetValue(dispatchFacade) as PhysicalLoweringRegistry;
        Assert.IsNotNull(registry);

        CollectionAssert.AreEqual(
            PhysicalLoweringDispatchFacade.PlanLoweringDescriptorNames.ToArray(),
            registry!.PlanDescriptors.Select(static descriptor => descriptor.Name).ToArray());
        CollectionAssert.AreEqual(
            PhysicalLoweringDispatchFacade.TableLoweringDescriptorNames.ToArray(),
            registry.TableDescriptors.Select(static descriptor => descriptor.Name).ToArray());
    }

    [TestMethod]
    public void LoweringCoordinators_ShouldBeTopLevelAndNotDependOnTheBuilder()
    {
        var assembly = typeof(PhysicalToExecutionPlanBuilder).Assembly;
        var coordinators = assembly
            .GetTypes()
            .Where(static type =>
                type.Namespace?.StartsWith("Musoq.Evaluator.IR.Execution.Lowering", StringComparison.Ordinal) == true &&
                type.Name.Contains("Lowerer", StringComparison.Ordinal))
            .ToArray();

        Assert.IsNotEmpty(coordinators);
        Assert.IsTrue(coordinators.All(static type => !type.IsNested));

        var builderType = typeof(PhysicalToExecutionPlanBuilder);
        var nestedLowerers = builderType
            .GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static type => type.Name.Contains("Lower", StringComparison.Ordinal) ||
                                  type.Name.Contains("Coordinator", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(nestedLowerers.Select(static type => type.FullName).ToArray());

        var forbiddenBuilderDependencies = coordinators
            .SelectMany(type => GetDirectMemberTypes(type)
                .Where(memberType => memberType == builderType)
                .Select(_ => type.FullName!))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(forbiddenBuilderDependencies);
    }

    [TestMethod]
    public void AggregateAndWindowLowerers_ShouldNotDependOnPhysicalLoweringImplementation()
    {
        var engineType = typeof(PhysicalLoweringImplementation);
        var lowererTypes = new[]
        {
            typeof(AggregatePlanLowerer),
            typeof(WindowPlanLowerer)
        };

        var dependencies = lowererTypes
            .SelectMany(type => GetDirectMemberTypes(type)
                .Where(memberType => memberType == engineType)
                .Select(_ => type.FullName!))
            .ToArray();

        Assert.IsEmpty(dependencies);
        Assert.IsNull(engineType.GetMethod(
            "BuildAggregateOnlyPipeline",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNull(engineType.GetMethod(
            "BuildSingleKeyAggregatePipeline",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNull(engineType.GetMethod(
            "BuildValueTupleAggregatePipeline",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNull(engineType.GetMethod(
            "BuildWindowPipeline",
            BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [TestMethod]
    public void PhysicalDomainLowerers_ShouldDependOnTypedDomainContracts()
    {
        var engineType = typeof(PhysicalLoweringImplementation);
        var lowererTypes = new[]
        {
            typeof(JoinPlanLowerer),
            typeof(CtePlanLowerer),
            typeof(PipelinePlanLowerer),
            typeof(MultiStatementPlanLowerer),
            typeof(DescPlanLowerer)
        };

        var directDependencies = lowererTypes
            .SelectMany(type => GetDirectMemberTypes(type)
                .Where(memberType => memberType == engineType || memberType.IsAssignableTo(typeof(Delegate)))
                .Select(_ => type.FullName!))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(directDependencies);
        Assert.IsTrue(lowererTypes.All(static type =>
            type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single()
                .GetParameters()
                .Single()
                .ParameterType.IsInterface));
    }

    [TestMethod]
    public void LoweringState_ShouldBeImmutableAndSplitByResponsibility()
    {
        var scopeType = typeof(LoweringScope);
        var mutableFields = scopeType
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static field => !field.IsInitOnly && !field.IsLiteral)
            .ToArray();
        var writableProperties = scopeType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.SetMethod is not null)
            .ToArray();

        Assert.IsEmpty(mutableFields);
        Assert.IsEmpty(writableProperties);
        Assert.IsNotNull(typeof(PhysicalLoweringFacts));
        Assert.IsNotNull(typeof(CteLoweringContext));
        Assert.IsNotNull(typeof(DirectTableLoweringContext));
        Assert.IsNotNull(typeof(RecursiveCteLoweringContext));
    }

    [TestMethod]
    public void CSharpRenderingTypes_ShouldNotReferencePlanningOrLoweringContracts()
    {
        var assembly = typeof(ExecutionCSharpRenderer).Assembly;
        var forbidden = assembly
            .GetTypes()
            .Where(static type =>
                type.Namespace?.StartsWith("Musoq.Targets.CSharpClr", StringComparison.Ordinal) == true)
            .SelectMany(type => GetDirectMemberTypes(type)
                .Where(IsPlanningOrLoweringType)
                .Select(memberType => $"{type.FullName} -> {memberType.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            forbidden,
            "C# rendering must consume execution contracts, not planning or lowering implementation types.");
    }

    [TestMethod]
    public void NodeDefinitionInventory_ShouldBeTheOnlyNodeMetadataSource()
    {
        var definitions = ExecutionNodeDefinitionCatalog.Definitions;
        var definitionTypes = definitions.Select(static definition => definition.NodeType).ToArray();
        var definitionIds = definitions.Select(static definition => definition.OperationId).ToArray();

        Assert.HasCount(definitionTypes.Length, definitionTypes.Distinct().ToArray());
        Assert.HasCount(definitionIds.Length, definitionIds.Distinct().ToArray());
        Assert.IsTrue(definitionIds.All(ExecutionOperationCatalog.AllOperationIds.Contains));

        CollectionAssert.AreEqual(
            definitionTypes,
            ExecutionNodeRegistry.Descriptors.Select(static descriptor => descriptor.NodeType).ToArray());
        CollectionAssert.AreEqual(
            definitions.Select(static definition => definition.RendererFamily).ToArray(),
            ExecutionNodeRegistry.Descriptors.Select(static descriptor => descriptor.RendererFamily).ToArray());
        CollectionAssert.AreEquivalent(
            definitionTypes,
            ExecutionOperationCatalog.RegisteredNodeTypes.ToArray());
    }

    [TestMethod]
    public void PortableExecutionContracts_ShouldNotExposeClrReflectionOrObjectSidecars()
    {
        Assert.IsNull(typeof(ExecutionTypeRef).GetProperty("ClrType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.IsNull(typeof(ExecutionCallableRef).GetProperty("ClrMethod", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

        var forbidden = new[]
        {
            typeof(ExecutionPlan),
            typeof(ExecutionNode),
            typeof(ExecutionExpression)
        }
        .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(property => ExpandType(property.PropertyType)
                .Where(IsClrSidecarType)
                .Select(memberType => $"{type.FullName}.{property.Name}: {memberType.FullName}")))
        .ToArray();

        Assert.IsEmpty(forbidden);
    }

    [TestMethod]
    public void RuntimeCaches_ShouldUseBoundedContractsAndExplicitOwnership()
    {
        var cacheFields = new[]
        {
            typeof(EvaluationHelper).GetField("NestedValueAccessors", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(EvaluationHelper).GetField("XmlDocCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(EvaluationHelper).GetField("CastableTypeCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(EvaluationHelper).GetField("ObjectChunkAdapters", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("HasIndexerCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("IsIndexableCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("TypeHintAttributeCache", BindingFlags.Static | BindingFlags.NonPublic)
        };

        Assert.IsTrue(cacheFields.All(static field =>
            field is not null &&
            field.FieldType.IsGenericType &&
            (field.FieldType.GetGenericTypeDefinition() == typeof(BoundedRuntimeCache<,>) ||
             field.FieldType.GetGenericTypeDefinition() == typeof(WeakTypeRuntimeCache<>))));

        var metadataCacheField = MetadataReferenceCache.Default.GetType().GetField(
            "_cache",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(metadataCacheField);
        Assert.AreEqual(typeof(BoundedRuntimeCache<,>), metadataCacheField!.FieldType.GetGenericTypeDefinition());
    }

    private static PhysicalLoweringHandlers CreateNoOpHandlers() => new(
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<ExecutionPlan>.NoMatch(),
        static _ => LoweringAttempt<LoweredTable>.NoMatch(),
        static _ => LoweringAttempt<LoweredTable>.NoMatch(),
        static _ => LoweringAttempt<LoweredTable>.NoMatch(),
        static _ => LoweringAttempt<LoweredTable>.NoMatch(),
        static _ => LoweringAttempt<LoweredTable>.NoMatch());

    private static IEnumerable<Type> GetDirectMemberTypes(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var memberType in ExpandType(field.FieldType))
                yield return memberType;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var memberType in ExpandType(property.PropertyType))
                yield return memberType;
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                foreach (var memberType in ExpandType(parameter.ParameterType))
                    yield return memberType;
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var memberType in ExpandType(method.ReturnType))
                yield return memberType;

            foreach (var parameter in method.GetParameters())
            {
                foreach (var memberType in ExpandType(parameter.ParameterType))
                    yield return memberType;
            }
        }
    }

    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;

        if (type.IsArray || type.IsByRef || type.IsPointer)
        {
            foreach (var nested in ExpandType(type.GetElementType()!))
                yield return nested;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var nested in ExpandType(argument))
                    yield return nested;
            }
        }
    }

    private static bool IsPlanningOrLoweringType(Type type)
    {
        return type.Namespace?.StartsWith("Musoq.Evaluator.IR.Planning", StringComparison.Ordinal) == true ||
               type.Namespace?.StartsWith("Musoq.Evaluator.IR.Execution.Lowering", StringComparison.Ordinal) == true;
    }

    private static bool IsClrSidecarType(Type type)
    {
        return type == typeof(Type) ||
               type == typeof(object) ||
               typeof(MemberInfo).IsAssignableFrom(type) ||
               typeof(Assembly).IsAssignableFrom(type);
    }
}
