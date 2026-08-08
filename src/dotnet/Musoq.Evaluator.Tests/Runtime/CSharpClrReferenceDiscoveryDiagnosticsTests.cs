using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Evaluator.Tests.External.Contracts;
using Musoq.Targets.Abstractions;
using Musoq.Targets.CSharpClr;
using Musoq.Targets.Execution;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class CSharpClrReferenceDiscoveryDiagnosticsTests
{
    [TestMethod]
    public void UnresolvablePortableDescriptor_ShouldProduceMt1005WithRequirementContext()
    {
        var descriptor = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.ClrOnly,
            "clr:Missing.Type@Missing.Assembly",
            "Missing.Type");

        var exception = Assert.Throws<CSharpClrReferenceDiscoveryException>(
            () => Collect(
                new ExecutionTargetRequirement(
                    ExecutionTargetRequirementKind.ClrTypeUsage,
                    "unresolvable source row",
                    descriptor)));

        AssertMissingReferenceDiagnostic(
            exception,
            "clr:Missing.Type@Missing.Assembly",
            "unresolvable source row",
            "the CLR descriptor could not be resolved");
    }

    [TestMethod]
    public void DynamicAssemblyWithoutLocation_ShouldProduceMt1005InsteadOfLoadingBroadly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Musoq.Wave5.Dynamic"),
            AssemblyBuilderAccess.Run);
        var type = assembly
            .DefineDynamicModule("main")
            .DefineType("DynamicRow", TypeAttributes.Public)
            .CreateType()!;

        var exception = Assert.Throws<CSharpClrReferenceDiscoveryException>(
            () => Collect(
                new[]
                {
                    assembly
                },
                new ExecutionTargetRequirement(
                    ExecutionTargetRequirementKind.ClrTypeUsage,
                    "dynamic source row",
                    ExecutionPortableSymbolFactory.FromType(type))));

        AssertMissingReferenceDiagnostic(
            exception,
            assembly.FullName!,
            "dynamic source row",
            "the assembly has no file-backed location");
    }

    [TestMethod]
    [DataRow(typeof(FileNotFoundException), "the assembly file could not be found")]
    [DataRow(typeof(BadImageFormatException), "the assembly file is not a valid CLR metadata image")]
    [DataRow(typeof(FileLoadException), "the assembly file could not be loaded")]
    public void MetadataReferenceFailures_ShouldUseStableMt1005Reasons(
        Type exceptionType,
        string expectedReason)
    {
        var innerException = (Exception)Activator.CreateInstance(exceptionType)!;
        var exception = CSharpClrReferenceDiscoveryException.ForMetadataReference(
            typeof(ExternalPayload).Assembly,
            "external payload output",
            innerException);

        AssertMissingReferenceDiagnostic(
            exception,
            typeof(ExternalPayload).Assembly.FullName!,
            "external payload output",
            expectedReason);
    }

    [TestMethod]
    public void BadMetadataImageFromCompilationContext_ShouldBecomeMt1005WithAssemblyIdentity()
    {
        var directoryPath = CreateDirectory();
        var badPath = Path.Combine(directoryPath, "bad.dll");
        File.WriteAllText(badPath, "not managed metadata");

        try
        {
            var context = new CompilationContextManager(CSharpCompilation.Create("wave5"));
            var exception = Assert.Throws<CSharpClrReferenceDiscoveryException>(
                () => context.InitializeCoreReferences(
                    new Assembly[]
                    {
                        new AssemblyReferenceStub(badPath, "Wave5.BadMetadata")
                    }));

            AssertMissingReferenceDiagnostic(
                exception,
                "Wave5.BadMetadata",
                "execution-plan CLR reference",
                "the assembly file is not a valid CLR metadata image");
        }
        finally
        {
            DeleteDirectory(directoryPath);
        }
    }

    [TestMethod]
    public void MissingMetadataFileFromCompilationContext_ShouldBecomeMt1005WithAssemblyIdentity()
    {
        var directoryPath = CreateDirectory();
        var missingPath = Path.Combine(directoryPath, "missing.dll");

        try
        {
            var context = new CompilationContextManager(CSharpCompilation.Create("wave5"));
            var exception = Assert.Throws<CSharpClrReferenceDiscoveryException>(
                () => context.InitializeCoreReferences(
                    new Assembly[]
                    {
                        new AssemblyReferenceStub(missingPath, "Wave5.Missing")
                    }));

            AssertMissingReferenceDiagnostic(
                exception,
                "Wave5.Missing",
                "execution-plan CLR reference",
                "the assembly file could not be found");
        }
        finally
        {
            DeleteDirectory(directoryPath);
        }
    }

    [TestMethod]
    public void DuplicateAssemblyIdentitiesFromDifferentPaths_ShouldKeepFirstReferenceOnly()
    {
        var probe = CollectDuplicateAssemblies();
        try
        {
            Assert.HasCount(1, probe.ReferencePaths);
            Assert.AreEqual(
                Path.GetFullPath(probe.FirstPath),
                Path.GetFullPath(probe.ReferencePaths[0]));
        }
        finally
        {
            DeleteDirectory(probe.DirectoryPath);
        }
    }

    [TestMethod]
    public void CollectibleAssembly_ShouldNotBeRetainedByReferenceDiscoveryState()
    {
        var probe = CollectFromCollectibleAssembly();
        try
        {
            for (var attempt = 0; attempt < 10 && probe.Context.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.IsFalse(probe.Context.IsAlive);
        }
        finally
        {
            DeleteDirectory(probe.DirectoryPath);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Context, string DirectoryPath) CollectFromCollectibleAssembly()
    {
        var directoryPath = CreateDirectory();
        var assemblyPath = CopyAssembly(directoryPath, "collectible.dll");
        var loadContext = new AssemblyLoadContext("wave5-collectible", isCollectible: true);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType(typeof(ExternalPayload).FullName!, throwOnError: true)!;

        _ = Collect(
            [assembly],
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "collectible source row",
                ExecutionPortableSymbolFactory.FromType(type)));

        var reference = new WeakReference(loadContext);
        loadContext.Unload();
        return (reference, directoryPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (string DirectoryPath, string FirstPath, string[] ReferencePaths) CollectDuplicateAssemblies()
    {
        var directoryPath = CreateDirectory();
        var firstPath = CopyAssembly(directoryPath, "first.dll");
        var secondPath = CopyAssembly(directoryPath, "second.dll");
        var firstContext = new AssemblyLoadContext("wave5-first", isCollectible: true);
        var secondContext = new AssemblyLoadContext("wave5-second", isCollectible: true);
        var referencePaths = Array.Empty<string>();

        try
        {
            var firstAssembly = firstContext.LoadFromAssemblyPath(firstPath);
            var secondAssembly = secondContext.LoadFromAssemblyPath(secondPath);
            referencePaths = Collect([firstAssembly, secondAssembly])
                .Select(static reference => reference.Location)
                .ToArray();
        }
        finally
        {
            firstContext.Unload();
            secondContext.Unload();
        }

        return (directoryPath, firstPath, referencePaths);
    }

    private static void AssertMissingReferenceDiagnostic(
        CSharpClrReferenceDiscoveryException exception,
        string assemblyIdentity,
        string requirementDetail,
        string reason)
    {
        var result = CSharpClrExecutionBackend.CreateMissingReferenceResult(exception);
        var diagnostic = result.Diagnostics.Single();

        Assert.AreEqual(TargetDiagnosticCodes.MissingClrReference, diagnostic.Code);
        Assert.AreEqual(
            $"Required CLR assembly '{assemblyIdentity}' for execution requirement " +
            $"'{requirementDetail}' could not be referenced: {reason}.",
            diagnostic.Message);
        Assert.IsFalse(diagnostic.Message.Contains("CS0234", StringComparison.Ordinal));
        Assert.IsFalse(diagnostic.Message.Contains("CS0246", StringComparison.Ordinal));
        Assert.IsFalse(diagnostic.Message.Contains("CS0012", StringComparison.Ordinal));
    }

    private static IReadOnlyList<Assembly> Collect(
        params ExecutionTargetRequirement[] requirements) =>
        Collect([], requirements);

    private static IReadOnlyList<Assembly> Collect(
        IReadOnlyList<Assembly> semanticAssemblies,
        params ExecutionTargetRequirement[] requirements)
    {
        return CSharpClrReferenceAssemblyCollector.Collect(
            new ExecutionTargetCompatibilityReport(requirements),
            new CSharpClrExecutionBindingContext(),
            semanticAssemblies,
            [],
            null,
            PreloadedPaths());
    }

    private static IReadOnlySet<string> PreloadedPaths() =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(typeof(object).Assembly.Location),
            Path.GetFullPath(typeof(System.ComponentModel.Component).Assembly.Location)
        };

    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Musoq.Evaluator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CopyAssembly(string directoryPath, string fileName)
    {
        var destination = Path.Combine(directoryPath, fileName);
        File.Copy(typeof(ExternalPayload).Assembly.Location, destination);
        return destination;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        for (var attempt = 0; attempt < 10 && Directory.Exists(directoryPath); attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, recursive: true);
            }
            catch (UnauthorizedAccessException)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }

    private sealed class AssemblyReferenceStub(string location, string identity) : Assembly
    {
        public override string Location => location;

        public override string FullName => identity;
    }
}
