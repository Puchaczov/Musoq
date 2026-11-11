using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
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
            .Where(file =>
                File.ReadAllText(file).Contains("class PhysicalToExecutionPlanBuilder", StringComparison.Ordinal)
            )
            .ToArray();

        Assert.HasCount(1, declarations);
        Assert.IsLessThanOrEqualTo(250, File.ReadAllLines(declarations[0]).Length);

        var methods = CompilerContractSymbols
            .For(typeof(PhysicalToExecutionPlanBuilder))
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method =>
                SymbolEqualityComparer.Default.Equals(
                    method.DeclaringType,
                    CompilerContractSymbols.For(typeof(PhysicalToExecutionPlanBuilder))
                ) && !method.IsSpecialName
            )
            .Select(static method => method.MetadataName)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "Build" }, methods);
        Assert.IsEmpty(
            CompilerContractSymbols
                .For(typeof(PhysicalToExecutionPlanBuilder))
                .GetNestedTypes(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                )
        );
    }

    [TestMethod]
    public void LoweringDispatch_ShouldUseOnlyTypedAttemptResults()
    {
        var delegateReturnTypes = new[]
        {
            CompilerContractSymbols.For(typeof(PhysicalLoweringHandlers)),
            CompilerContractSymbols.For(typeof(PhysicalPlanLoweringDescriptor)),
            CompilerContractSymbols.For(typeof(PhysicalTableLoweringDescriptor)),
        }
            .SelectMany(static type =>
                type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            )
            .SelectMany(static constructor => constructor.GetParameters())
            .Select(static parameter => parameter.ParameterType)
            .Where(static type =>
                type.IsGenericType
                && type.GetGenericTypeDefinition().MetadataName.StartsWith("Func`", StringComparison.Ordinal)
            )
            .Select(static type => type.GetGenericArguments()[^1])
            .ToArray();

        Assert.IsNotEmpty(delegateReturnTypes);
        Assert.IsTrue(
            delegateReturnTypes.All(static type =>
                type.IsGenericType
                && SymbolEqualityComparer.Default.Equals(
                    type.GetGenericTypeDefinition(),
                    CompilerContractSymbols.For(typeof(LoweringAttempt<>))
                )
            )
        );

        var loweringSources = RepositorySourceScan.FilesUnder(
            RepositorySourceScan.RepositoryRoot(),
            "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering",
            "*.cs"
        );
        var unsupportedSignaling = loweringSources
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("null!", StringComparison.Ordinal)
                    || text.Contains("default!", StringComparison.Ordinal)
                    || text.Contains("bool Supported", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsEmpty(unsupportedSignaling);
    }

    [TestMethod]
    public void SemanticHandoffs_ShouldNotExposeMutableScopeTraversal()
    {
        var semanticPhaseArtifacts = CompilerContractSymbols
            .For(typeof(QueryAnalyzer))
            .Assembly.GetType("Musoq.Evaluator.Visitors.SemanticPhaseArtifacts")!;
        var rewriteInput = CompilerContractSymbols
            .For(typeof(QueryAnalyzer))
            .Assembly.GetType("Musoq.Evaluator.Visitors.RewriteQueryPhaseInput")!;
        var semanticBuildArtifacts = CompilerContractSymbols
            .For(typeof(Converter.InstanceCreator))
            .Assembly.GetType("Musoq.Converter.Build.SemanticBuildArtifacts")!;

        foreach (var type in new[] { semanticPhaseArtifacts, rewriteInput, semanticBuildArtifacts })
        {
            var exposedScopeTypes = type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
                .SelectMany(static property => ExpandType(property.PropertyType))
                .Concat(
                    type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .SelectMany(static method => ExpandType(method.ReturnType))
                )
                .Where(static memberType =>
                    SymbolEqualityComparer.Default.Equals(memberType, CompilerContractSymbols.For(typeof(Scope)))
                    || SymbolEqualityComparer.Default.Equals(
                        memberType,
                        CompilerContractSymbols.For(typeof(ScopeWalker))
                    )
                )
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
        var executionGate = CompilerContractSymbols
            .For(typeof(CompiledQuery))
            .GetField("_executionGate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(executionGate);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(SemaphoreSlim)),
                executionGate!.FieldType
            )
        );

        var asyncRun = CompilerContractSymbols
            .For(typeof(IAsyncTableRunnable))
            .GetMethod(nameof(IAsyncTableRunnable.RunAsync));
        Assert.IsNotNull(asyncRun);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols
                    .For(typeof(System.Threading.Tasks.ValueTask<>))
                    .Construct(CompilerContractSymbols.For(typeof(Tables.Table))),
                asyncRun!.ReturnType
            )
        );
        Assert.IsTrue(
            CompilerContractSymbols
                .For(typeof(IDisposable))
                .IsAssignableFrom(CompilerContractSymbols.For(typeof(EvaluatorRuntimeEnvironment)))
        );

        var targetFiles = RepositorySourceScan.ProductionSourceFiles(
            RepositorySourceScan.RepositoryRoot(),
            "Musoq.Targets.CSharpClr"
        );
        var staticGlobalConsumers = targetFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal)
                    || text.Contains("RoslynSharedFactory", StringComparison.Ordinal)
                    || text.Contains("MetadataReferenceCache", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsEmpty(staticGlobalConsumers);
    }

    [TestMethod]
    public void PortableDescriptorsAndCaches_ShouldKeepTheirBoundaries()
    {
        var descriptorTypes = new[]
        {
            CompilerContractSymbols.For(typeof(ExecutionTypeRef)),
            CompilerContractSymbols.For(typeof(ExecutionCallableRef)),
        };
        var forbiddenBindingMethods = descriptorTypes
            .SelectMany(static type =>
                type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                )
            )
            .Where(static method =>
                method.MetadataName.Contains("ResolveClr", StringComparison.Ordinal)
                || method.MetadataName.Contains("FromClr", StringComparison.Ordinal)
            )
            .ToArray();
        Assert.IsEmpty(forbiddenBindingMethods);

        var cacheTypes = new[]
        {
            CompilerContractSymbols.For(typeof(Helpers.EvaluationHelper)),
            CompilerContractSymbols.For(typeof(BuildMetadataAndInferTypesVisitorUtilities)),
        };
        var cacheFields = cacheTypes
            .SelectMany(static type => type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            .Where(static field =>
                field.MetadataName.Contains("Cache", StringComparison.Ordinal)
                || field.MetadataName.Contains("Adapters", StringComparison.Ordinal)
            )
            .ToArray();

        Assert.IsNotEmpty(cacheFields);
        Assert.IsTrue(
            cacheFields.All(static field =>
                field.FieldType.IsGenericType
                && (
                    SymbolEqualityComparer.Default.Equals(
                        field.FieldType.GetGenericTypeDefinition(),
                        CompilerContractSymbols.For(typeof(BoundedRuntimeCache<,>))
                    )
                    || SymbolEqualityComparer.Default.Equals(
                        field.FieldType.GetGenericTypeDefinition(),
                        CompilerContractSymbols.For(typeof(WeakTypeRuntimeCache<>))
                    )
                )
            )
        );

        Assert.IsTrue(
            CompilerContractSymbols
                .For(typeof(IDisposable))
                .IsAssignableFrom(CompilerContractSymbols.For(typeof(InterpreterCompilationUnit)))
        );
        var handleContext = CompilerContractSymbols
            .For(typeof(LoadedAssemblyHandle))
            .GetField("_loadContext", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(handleContext);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(System.Runtime.Loader.AssemblyLoadContext)),
                handleContext!.FieldType
            )
        );
    }

    private static IEnumerable<ITypeSymbol> ExpandType(ITypeSymbol type)
    {
        yield return type;

        if (type.IsArray || type.IsPointer)
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
