using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.External.Contracts;
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

    private static void DeleteDirectory(string directoryPath)
    {
        for (var attempt = 0; attempt < 10 && Directory.Exists(directoryPath); attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, recursive: true);
            }
            catch (UnauthorizedAccessException) when (attempt < 9)
            {
                Thread.Sleep(20);
            }
        }
    }

    private sealed class AssemblyReferenceStub(string location, string identity) : Assembly
    {
        public override string Location => location;

        public override string FullName => identity;
    }

}
