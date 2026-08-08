using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CSharpClrReferenceArchitectureGuardrailTests
{
    [TestMethod]
    public void ReferenceCollector_ShouldRemainDemandDrivenAndFileBacked()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var collector = File.ReadAllText(Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "CSharpClrReferenceAssemblyCollector.cs"));

        AssertForbidden(collector, "TRUSTED_PLATFORM_ASSEMBLIES");
        AssertForbidden(collector, "Assembly.GetReferencedAssemblies");
        AssertForbidden(collector, "AppDomain.CurrentDomain.GetAssemblies");
        AssertForbidden(collector, "Directory.GetFiles");
        AssertForbidden(collector, "Directory.EnumerateFiles");
        StringAssert.Contains(collector, "compatibilityReport.Requirements");
        StringAssert.Contains(collector, "additionalReferenceTypes");
        StringAssert.Contains(collector, "outputType");
        StringAssert.Contains(collector, "_assembliesByPath");
        StringAssert.Contains(collector, "_assembliesByIdentity");
    }

    [TestMethod]
    public void ClrBindingResolver_ShouldNotEnumerateEveryLoadedAssembly()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var resolver = File.ReadAllText(Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Portability",
            "ExecutionClrBindingResolver.cs"));

        AssertForbidden(resolver, "AppDomain.CurrentDomain.GetAssemblies");
        AssertForbidden(resolver, "Assembly.GetReferencedAssemblies");
        StringAssert.Contains(resolver, "semanticAssemblies");
        StringAssert.Contains(resolver, "Type.GetType");
    }

    [TestMethod]
    public void RuntimeReferenceTemplate_ShouldStayFixedAndExcludeProcess()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var provider = File.ReadAllText(Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Runtime",
            "RuntimeReferenceProvider.cs"));

        AssertForbidden(provider, "System.Diagnostics.Process.dll");
        AssertForbidden(provider, "TRUSTED_PLATFORM_ASSEMBLIES");
        AssertForbidden(provider, "Assembly.GetReferencedAssemblies");
        StringAssert.Contains(provider, "DefaultEssentialAssemblyNames");
    }

    [TestMethod]
    public void Backend_ShouldDelegateClrReferenceDiscoveryToTheCollector()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var backend = File.ReadAllText(Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "CSharpClrExecutionBackend.cs"));

        StringAssert.Contains(backend, "CSharpClrReferenceAssemblyCollector.Collect(");
        StringAssert.Contains(backend, "compilationContext.InitializeCoreReferences(referenceAssemblies);");
        AssertForbidden(backend, "TRUSTED_PLATFORM_ASSEMBLIES");
        AssertForbidden(backend, "Assembly.GetReferencedAssemblies");
        AssertForbidden(backend, "AppDomain.CurrentDomain.GetAssemblies");
        AssertForbidden(backend, "Directory.GetFiles");
        AssertForbidden(backend, "Directory.EnumerateFiles");
    }

    private static void AssertForbidden(string text, string fragment)
    {
        Assert.IsFalse(
            text.Contains(fragment, StringComparison.Ordinal),
            $"Forbidden broad CLR discovery fragment '{fragment}' was found.");
    }
}
