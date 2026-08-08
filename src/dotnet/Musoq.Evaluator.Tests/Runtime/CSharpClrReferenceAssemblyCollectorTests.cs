using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Targets.Abstractions;
using Musoq.Targets.CSharpClr;
using Musoq.Targets.Execution;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class CSharpClrReferenceAssemblyCollectorTests
{
    [TestMethod]
    public void Collect_WhenProcessTypeIsRequired_ShouldReturnOnlyProcessAssembly()
    {
        var references = Collect(
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "process-source",
                ExecutionPortableSymbolFactory.FromType(typeof(Process))));

        Assert.AreEqual(1, references.Count);
        Assert.AreEqual(Normalize(typeof(Process).Assembly.Location), Normalize(references[0].Location));
    }

    [TestMethod]
    public void Collect_WhenRequirementIsPrimitiveOrPreloaded_ShouldReturnNoAdditionalAssemblies()
    {
        var requirements = new[]
        {
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "string",
                ExecutionPortableSymbolFactory.FromType(typeof(string))),
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "integer",
                ExecutionPortableSymbolFactory.FromType(typeof(int)))
        };

        var references = Collect(requirements);

        Assert.IsEmpty(references);
    }

    [TestMethod]
    public void Collect_WhenSameTypeAppearsInRequirementsAndGeneratedFields_ShouldDeduplicateByPath()
    {
        var generatedRow = ExecutionPortableSymbolFactory.GeneratedRow(
            "ResultRow",
            [new ExecutionPortableRowFieldDescriptor(
                "Process",
                ExecutionPortableSymbolFactory.FromType(typeof(Process)),
                "Unknown")]);

        var references = Collect(
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "process-source",
                ExecutionPortableSymbolFactory.FromType(typeof(Process))),
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.GeneratedClrRow,
                "ResultRow",
                generatedRow));

        Assert.AreEqual(1, references.Count);
        Assert.AreEqual(Normalize(typeof(Process).Assembly.Location), Normalize(references[0].Location));
    }

    [TestMethod]
    public void Collect_WhenGenericTypeContainsExternalArgument_ShouldVisitOnlyArgumentAssembly()
    {
        var references = Collect(
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "uri-map",
                ExecutionPortableSymbolFactory.FromType(typeof(Dictionary<string, Uri>))));

        CollectionAssert.AreEquivalent(
            new[] { Normalize(typeof(Uri).Assembly.Location) },
            references.Select(reference => Normalize(reference.Location)).ToArray());
    }

    [TestMethod]
    public void Collect_WhenCallableUsesExternalDeclaringType_ShouldVisitCallableAssembly()
    {
        var method = typeof(Uri).GetMethod(nameof(Uri.ToString), Type.EmptyTypes) ??
                     throw new InvalidOperationException("Uri.ToString() was not found.");

        var references = Collect(
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.MethodInfoCall,
                "uri-to-string",
                CallableSymbol: ExecutionPortableSymbolFactory.FromMethod(method)));

        CollectionAssert.AreEquivalent(
            new[] { Normalize(typeof(Uri).Assembly.Location) },
            references.Select(reference => Normalize(reference.Location)).ToArray());
    }

    [TestMethod]
    public void Collect_WhenOutputAndAdditionalTypesAreExternal_ShouldVisitBothInStableOrder()
    {
        var references = CSharpClrReferenceAssemblyCollector.Collect(
            new ExecutionTargetCompatibilityReport([]),
            new CSharpClrExecutionBindingContext(),
            [],
            [typeof(Uri)],
            typeof(Process),
            PreloadedPaths());

        CollectionAssert.AreEqual(
            new[]
            {
                Normalize(typeof(Uri).Assembly.Location),
                Normalize(typeof(Process).Assembly.Location)
            },
            references.Select(reference => Normalize(reference.Location)).ToArray());
    }

    [TestMethod]
    public void Collect_WhenRepeatedConcurrently_ShouldPreserveDeterministicAssemblyOrder()
    {
        var requirements = new[]
        {
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "uri",
                ExecutionPortableSymbolFactory.FromType(typeof(Uri))),
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "process",
                ExecutionPortableSymbolFactory.FromType(typeof(Process)))
        };
        var results = new string[8][];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = Collect(requirements)
                .Select(reference => Normalize(reference.Location))
                .ToArray();
        });

        for (var index = 1; index < results.Length; index++)
            CollectionAssert.AreEqual(results[0], results[index]);
    }

    private static IReadOnlyList<Assembly> Collect(params ExecutionTargetRequirement[] requirements)
    {
        return CSharpClrReferenceAssemblyCollector.Collect(
            new ExecutionTargetCompatibilityReport(requirements),
            new CSharpClrExecutionBindingContext(),
            [],
            [],
            null,
            PreloadedPaths());
    }

    private static IReadOnlySet<string> PreloadedPaths()
    {
        return new HashSet<string>(
            [
                Normalize(typeof(object).Assembly.Location),
                Normalize(typeof(Component).Assembly.Location)
            ],
            StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) => Path.GetFullPath(path);
}
