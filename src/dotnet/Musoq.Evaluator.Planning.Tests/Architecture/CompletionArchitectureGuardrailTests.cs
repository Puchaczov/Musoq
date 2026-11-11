using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CompletionArchitectureGuardrailTests
{
    [TestMethod]
    public void LoweringAssembly_ShouldNotContainLegacyGenericBuildResultsOrSessions()
    {
        var assembly = CompilerContractSymbols.For(typeof(PhysicalToExecutionPlanBuilder)).Assembly;

        var legacyTypes = assembly
            .GetTypes()
            .Where(static type =>
                type.MetadataName == "PhysicalToExecutionLoweringSession"
                || (
                    type.IsGenericTypeDefinition
                    && type.MetadataName.StartsWith("BuildResult", StringComparison.Ordinal)
                )
            )
            .Select(static type => type.FullName)
            .ToArray();

        Assert.IsEmpty(legacyTypes);

        var loweringSourceOffenders = RepositorySourceScan
            .FilesUnder(
                RepositorySourceScan.RepositoryRoot(),
                "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering",
                "*.cs"
            )
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("bool Supported", StringComparison.Ordinal)
                    || text.Contains(".Supported", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(RepositorySourceScan.RepositoryRoot(), file))
            .ToArray();

        Assert.IsEmpty(loweringSourceOffenders);

        var attemptType = CompilerContractSymbols.For(typeof(LoweringAttempt<>));
        Assert.IsNull(attemptType.GetProperty("Supported", BindingFlags.Instance | BindingFlags.Public));
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                attemptType.GetGenericArguments()[0],
                attemptType.GetProperty(nameof(LoweringAttempt<object>.Value))!.PropertyType
            )
        );
    }

    [TestMethod]
    public void PhysicalLoweringDispatchFacade_ShouldRegisterEachDescriptorExactlyOnce()
    {
        var trace = new LoweringDispatchTrace();
        trace.Exercise();
        CollectionAssert.AreEqual(LoweringDispatchTrace.ExpectedPlanOrder, trace.PlanCalls.ToArray());
        CollectionAssert.AreEqual(LoweringDispatchTrace.ExpectedTableOrder, trace.TableCalls.ToArray());
        var names = trace.PlanCalls.Concat(trace.TableCalls).ToArray();
        Assert.HasCount(12, names);
        Assert.HasCount(names.Length, names.Distinct(StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void RoslynCompatibilityGlobals_ShouldHaveNoProductionConsumersOutsideTheirShims()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var evaluatorFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");
        var actual = evaluatorFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal)
                    || text.Contains("RoslynSharedFactory", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "src/dotnet/Musoq.Evaluator/Runtime/MetadataReferenceCache.cs",
                "src/dotnet/Musoq.Evaluator/Runtime/RoslynSharedFactory.cs",
                "src/dotnet/Musoq.Evaluator/Runtime/RuntimeLibraries.cs",
            },
            actual
        );
    }

    [TestMethod]
    public void CSharpTarget_ShouldUseExplicitRuntimeEnvironmentsOutsideCompatibilityShims()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Targets.CSharpClr");
        var actual = targetFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal)
                    || text.Contains("RoslynSharedFactory", StringComparison.Ordinal)
                    || text.Contains("MetadataReferenceCache", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public void LegacyInterpreterAssemblyLoading_ShouldExposeCollectibleOwnershipAndDisposal()
    {
        var loadMethod = CompilerContractSymbols.For(typeof(IAssemblyLoader)).GetMethod(nameof(IAssemblyLoader.Load));
        Assert.IsNotNull(loadMethod);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(LoadedAssemblyHandle)),
                loadMethod!.ReturnType
            )
        );
        Assert.IsTrue(
            CompilerContractSymbols
                .For(typeof(IDisposable))
                .IsAssignableFrom(CompilerContractSymbols.For(typeof(InterpreterCompilationUnit)))
        );

        var loadContextField = CompilerContractSymbols
            .For(typeof(LoadedAssemblyHandle))
            .GetField("_loadContext", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(loadContextField);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(System.Runtime.Loader.AssemblyLoadContext)),
                loadContextField!.FieldType
            )
        );
    }
}
