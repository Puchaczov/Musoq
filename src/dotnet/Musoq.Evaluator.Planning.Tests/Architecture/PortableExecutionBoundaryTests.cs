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
        var typeRefMethods = typeof(ExecutionTypeRef)
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.DeclaringType == typeof(ExecutionTypeRef))
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        var callableRefMethods = typeof(ExecutionCallableRef)
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.DeclaringType == typeof(ExecutionCallableRef))
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromClr");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromOptionalClr");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "FromClrTypes");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "ResolveClrType");
        CollectionAssert.DoesNotContain(typeRefMethods.ToArray(), "ClrDisplayName");
        CollectionAssert.DoesNotContain(callableRefMethods.ToArray(), "FromClr");
        CollectionAssert.DoesNotContain(callableRefMethods.ToArray(), "ResolveClrMethod");

        Assert.IsFalse(typeof(ExecutionTypeRef).GetProperties().Any(property => property.PropertyType == typeof(Type)));
        Assert.IsFalse(typeof(ExecutionCallableRef).GetProperties().Any(property => property.PropertyType == typeof(MethodInfo)));
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
