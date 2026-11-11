using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PortableExecutionBoundaryTests
{
    [TestMethod]
    public void ExecutionReferences_ShouldExposeDescriptors_NotClrBindingHelpers()
    {
        var typeRefMethods = CompilerContractSymbols.For(typeof(ExecutionTypeRef))
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => SymbolEqualityComparer.Default.Equals(method.DeclaringType, CompilerContractSymbols.For(typeof(ExecutionTypeRef))))
            .Select(method => method.MetadataName)
            .ToHashSet(StringComparer.Ordinal);
        var callableRefMethods = CompilerContractSymbols.For(typeof(ExecutionCallableRef))
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => SymbolEqualityComparer.Default.Equals(method.DeclaringType, CompilerContractSymbols.For(typeof(ExecutionCallableRef))))
            .Select(method => method.MetadataName)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromClr");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromOptionalClr");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromClrTypes");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "ResolveClrType");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "ClrDisplayName");
        CollectionAssert.DoesNotContain(callableRefMethods.ToArray(), "FromClr");
        CollectionAssert.DoesNotContain(callableRefMethods.ToArray(), "ResolveClrMethod");

        Assert.IsFalse(CompilerContractSymbols.For(typeof(ExecutionTypeRef)).GetProperties().Any(property => SymbolEqualityComparer.Default.Equals(property.PropertyType, CompilerContractSymbols.For(typeof(Type)))));
        Assert.IsFalse(CompilerContractSymbols.For(typeof(ExecutionCallableRef)).GetProperties().Any(property => SymbolEqualityComparer.Default.Equals(property.PropertyType, CompilerContractSymbols.For(typeof(MethodInfo)))));
    }

    [TestMethod]
    public void PortableExecutionAndOptimizationSources_ShouldNotReferenceRoslyn()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(root, "src/dotnet/Musoq.Evaluator/IR/Execution", "*.cs")
            .Concat(RepositorySourceScan.FilesUnder(root, "src/dotnet/Musoq.Evaluator/IR/Optimization/Execution", "*.cs"));

        var leakingFiles = files
            .Where(file => File.ReadAllText(file).Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(root, file))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(leakingFiles, "Portable execution and optimization code must not reference Roslyn: " + string.Join(", ", leakingFiles));
    }
}
