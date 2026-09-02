using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureCompletionGuardrailTests
{
    [TestMethod]
    public void Builder_ShouldBeOneSmallNonPartialFacade()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var declarations = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution", "*.cs")
            .Where(file => File.ReadAllText(file).Contains("class PhysicalToExecutionPlanBuilder", StringComparison.Ordinal))
            .ToArray();

        Assert.HasCount(1, declarations);
        Assert.IsLessThanOrEqualTo(250, File.ReadAllLines(declarations[0]).Length);

        var methods = typeof(PhysicalToExecutionPlanBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.DeclaringType == typeof(PhysicalToExecutionPlanBuilder) && !method.IsSpecialName)
            .Select(static method => method.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "Build" }, methods);
        Assert.IsEmpty(typeof(PhysicalToExecutionPlanBuilder).GetNestedTypes(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
    }

    [TestMethod]
    public void LoweringDispatch_ShouldUseOnlyTypedAttemptResults()
    {
        var delegateReturnTypes = new[]
        {
            typeof(PhysicalLoweringHandlers),
            typeof(PhysicalPlanLoweringDescriptor),
            typeof(PhysicalTableLoweringDescriptor)
        }
        .SelectMany(static type => type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        .SelectMany(static constructor => constructor.GetParameters())
        .Select(static parameter => parameter.ParameterType)
        .Where(static type => type.IsGenericType &&
                              type.GetGenericTypeDefinition().Name.StartsWith("Func`", StringComparison.Ordinal))
        .Select(static type => type.GetGenericArguments()[^1])
        .ToArray();

        Assert.IsNotEmpty(delegateReturnTypes);
        Assert.IsTrue(delegateReturnTypes.All(static type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LoweringAttempt<>)));

        var loweringSources = RepositorySourceScan.FilesUnder(
            RepositorySourceScan.RepositoryRoot(),
            "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering",
            "*.cs");
        var unsupportedSignaling = loweringSources
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("null!", StringComparison.Ordinal) ||
                       text.Contains("default!", StringComparison.Ordinal) ||
                       text.Contains("bool Supported", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsEmpty(unsupportedSignaling);
    }

    [TestMethod]
    public void SemanticHandoffs_ShouldNotExposeMutableScopeTraversal()
    {
        var semanticPhaseArtifacts = typeof(QueryAnalyzer).Assembly.GetType(
            "Musoq.Evaluator.Visitors.SemanticPhaseArtifacts")!;
        var rewriteInput = typeof(QueryAnalyzer).Assembly.GetType(
            "Musoq.Evaluator.Visitors.RewriteQueryPhaseInput")!;
        var semanticBuildArtifacts = typeof(Converter.InstanceCreator).Assembly.GetType(
            "Musoq.Converter.Build.SemanticBuildArtifacts")!;

        foreach (var type in new[] { semanticPhaseArtifacts, rewriteInput, semanticBuildArtifacts })
        {
            var exposedScopeTypes = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(static property => ExpandType(property.PropertyType))
                .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(static method => ExpandType(method.ReturnType)))
                .Where(static memberType => memberType == typeof(Scope) || memberType == typeof(ScopeWalker))
                .ToArray();

            Assert.IsEmpty(exposedScopeTypes, $"Mutable scope leaked from {type.FullName}.");
        }
    }

    [TestMethod]
    public void ExecutionIr_ShouldCopyCollectionBoundaries()
    {
        var source = new List<ExecutionNode> { new TestNode() };
        var block = new ExecutionBlock(source);

        source.Clear();

        Assert.HasCount(1, block.Nodes);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionNode>)block.Nodes).Clear());
    }

    [TestMethod]
    public void RuntimeAndTargetContracts_ShouldHaveExplicitOwnership()
    {
        var executionGate = typeof(CompiledQuery).GetField(
            "_executionGate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(executionGate);
        Assert.AreEqual(typeof(SemaphoreSlim), executionGate!.FieldType);

        var asyncRun = typeof(IAsyncTableRunnable).GetMethod(nameof(IAsyncTableRunnable.RunAsync));
        Assert.IsNotNull(asyncRun);
        Assert.AreEqual(typeof(System.Threading.Tasks.ValueTask<Tables.Table>), asyncRun!.ReturnType);
        Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(EvaluatorRuntimeEnvironment)));

        var targetFiles = RepositorySourceScan.ProductionSourceFiles(
            RepositorySourceScan.RepositoryRoot(),
            "Musoq.Targets.CSharpClr");
        var staticGlobalConsumers = targetFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal) ||
                       text.Contains("RoslynSharedFactory", StringComparison.Ordinal) ||
                       text.Contains("MetadataReferenceCache", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsEmpty(staticGlobalConsumers);
    }

    [TestMethod]
    public void PortableDescriptorsAndCaches_ShouldKeepTheirBoundaries()
    {
        var descriptorTypes = new[] { typeof(ExecutionTypeRef), typeof(ExecutionCallableRef) };
        var forbiddenBindingMethods = descriptorTypes
            .SelectMany(static type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(static method => method.Name.Contains("ResolveClr", StringComparison.Ordinal) ||
                                    method.Name.Contains("FromClr", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(forbiddenBindingMethods);

        var cacheTypes = new[]
        {
            typeof(Helpers.EvaluationHelper),
            typeof(BuildMetadataAndInferTypesVisitorUtilities)
        };
        var cacheFields = cacheTypes
            .SelectMany(static type => type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            .Where(static field => field.Name.Contains("Cache", StringComparison.Ordinal) ||
                                   field.Name.Contains("Adapters", StringComparison.Ordinal))
            .ToArray();

        Assert.IsNotEmpty(cacheFields);
        Assert.IsTrue(cacheFields.All(static field =>
            field.FieldType.IsGenericType &&
            (field.FieldType.GetGenericTypeDefinition() == typeof(BoundedRuntimeCache<,>) ||
             field.FieldType.GetGenericTypeDefinition() == typeof(WeakTypeRuntimeCache<>))));

        Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(InterpreterCompilationUnit)));
        var handleContext = typeof(LoadedAssemblyHandle).GetField(
            "_loadContext",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(handleContext);
        Assert.AreEqual(typeof(System.Runtime.Loader.AssemblyLoadContext), handleContext!.FieldType);
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

    private sealed record TestNode : ExecutionNode;
}
