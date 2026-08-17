using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        var assembly = typeof(PhysicalToExecutionPlanBuilder).Assembly;

        var legacyTypes = assembly
            .GetTypes()
            .Where(static type =>
                type.Name == "PhysicalToExecutionLoweringSession" ||
                (type.IsGenericTypeDefinition && type.Name.StartsWith("BuildResult", StringComparison.Ordinal)))
            .Select(static type => type.FullName)
            .ToArray();

        Assert.IsEmpty(legacyTypes);

        var loweringSourceOffenders = RepositorySourceScan
            .FilesUnder(RepositorySourceScan.RepositoryRoot(), "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering", "*.cs")
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("bool Supported", StringComparison.Ordinal) ||
                       text.Contains(".Supported", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(RepositorySourceScan.RepositoryRoot(), file))
            .ToArray();

        Assert.IsEmpty(loweringSourceOffenders);

        var attemptType = typeof(LoweringAttempt<>);
        Assert.IsNull(attemptType.GetProperty("Supported", BindingFlags.Instance | BindingFlags.Public));
        Assert.AreEqual(
            attemptType.GetGenericArguments()[0],
            attemptType.GetProperty(nameof(LoweringAttempt<object>.Value))!.PropertyType);
    }

    [TestMethod]
    public void PhysicalLoweringDispatchFacade_ShouldRegisterEachDescriptorExactlyOnce()
    {
        var facade = new PhysicalLoweringDispatchFacade(CreateNoOpHandlers());
        var registryField = typeof(PhysicalLoweringDispatchFacade).GetField(
            "_registry",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var registry = (PhysicalLoweringRegistry)registryField!.GetValue(facade)!;
        var names = registry.PlanDescriptors
            .Select(static descriptor => descriptor.Name)
            .Concat(registry.TableDescriptors.Select(static descriptor => descriptor.Name))
            .ToArray();

        Assert.HasCount(
            PhysicalLoweringDispatchFacade.PlanLoweringDescriptorNames.Count + PhysicalLoweringDispatchFacade.TableLoweringDescriptorNames.Count,
            names);
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
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal) ||
                       text.Contains("RoslynSharedFactory", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "src/dotnet/Musoq.Evaluator/Runtime/MetadataReferenceCache.cs",
                "src/dotnet/Musoq.Evaluator/Runtime/RoslynSharedFactory.cs",
                "src/dotnet/Musoq.Evaluator/Runtime/RuntimeLibraries.cs"
            },
            actual);
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
                return text.Contains("RuntimeLibraries", StringComparison.Ordinal) ||
                       text.Contains("RoslynSharedFactory", StringComparison.Ordinal) ||
                       text.Contains("MetadataReferenceCache", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public void LegacyInterpreterAssemblyLoading_ShouldExposeCollectibleOwnershipAndDisposal()
    {
        var loadMethod = typeof(IAssemblyLoader).GetMethod(nameof(IAssemblyLoader.Load));
        Assert.IsNotNull(loadMethod);
        Assert.AreEqual(typeof(LoadedAssemblyHandle), loadMethod!.ReturnType);
        Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(InterpreterCompilationUnit)));

        var loadContextField = typeof(LoadedAssemblyHandle).GetField(
            "_loadContext",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(loadContextField);
        Assert.AreEqual(typeof(System.Runtime.Loader.AssemblyLoadContext), loadContextField!.FieldType);
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
}
