using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Execution.Analysis;
using Musoq.Targets.TestPortable;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PortableSubsetTargetAssemblyTests
{
    [TestMethod]
    public void PortableSubsetTarget_WhenRendering_ShouldProduceDeterministicImmutableProgram()
    {
        var plan = CreateLiteralPlan();
        var request = CreateRequest(plan);
        var backend = new PortableSubsetExecutionBackend();

        var first = backend.Render(request);
        var second = backend.Render(request);

        Assert.IsTrue(first.Success);
        var firstArtifact = Assert.IsInstanceOfType<PortableSubsetRenderedArtifact>(first.Artifact);
        var secondArtifact = Assert.IsInstanceOfType<PortableSubsetRenderedArtifact>(second.Artifact);
        Assert.AreEqual(firstArtifact.Program.CreateManifest(), secondArtifact.Program.CreateManifest());
        StringAssert.Contains(firstArtifact.Program.CreateManifest(), "let value = i32:1");
        StringAssert.Contains(firstArtifact.Program.CreateManifest(), $"semantics-fingerprint={ExecutionSemanticsContract.Version1.Fingerprint}");
        Assert.Throws<NotSupportedException>(() =>
            ((IList<PortableInstruction>)firstArtifact.Program.Body.Instructions).Clear());
    }

    [TestMethod]
    public void PortableSubsetTarget_WhenFinalizedAndInspected_ShouldExportManifestWithoutClrArtifacts()
    {
        var rendered = Assert.IsInstanceOfType<PortableSubsetRenderedArtifact>(
            new PortableSubsetExecutionBackend().Render(CreateRequest(
                CreateLiteralPlan())).Artifact);

        var finalization = new PortableSubsetRenderedQueryFinalizer().Finalize(
            rendered,
            TargetFinalizationOptions.Empty);
        var inspection = new PortableSubsetRenderedQueryInspector().Inspect(rendered);

        Assert.IsTrue(finalization.Success);
        var export = Assert.IsInstanceOfType<TargetExportArtifact>(finalization.Artifact);
        Assert.HasCount(1, export.SourceFiles);
        Assert.AreEqual("program.musoq-portable", export.SourceFiles[0].Path);
        StringAssert.Contains(export.SourceFiles[0].Content, "portable-subset-program:v1");
        Assert.IsEmpty(export.BinaryBlobs);
        Assert.HasCount(1, export.Entrypoints);
        Assert.IsNull(inspection.GeneratedCSharpCode);
        StringAssert.Contains(inspection.SourceMetadata["inspectionText"], "let value = i32:1");
        Assert.IsFalse(typeof(PortableSubsetRenderedQueryFinalizer).Assembly.GetTypes().Any(static type =>
            typeof(IClrExecutableQueryActivator).IsAssignableFrom(type)));
    }

    [TestMethod]
    public void PortableSubsetTargetAssembly_ShouldHaveOnlyPortableTargetDependencies()
    {
        var assembly = typeof(PortableSubsetExecutionBackend).Assembly;
        var referencedAssemblyNames = assembly.GetReferencedAssemblies()
            .Select(static reference => reference.Name)
            .ToArray();

        CollectionAssert.Contains(referencedAssemblyNames, "Musoq.Evaluator");
        CollectionAssert.Contains(referencedAssemblyNames, "Musoq.Targets.Abstractions");
        CollectionAssert.Contains(referencedAssemblyNames, "Musoq.Targets.Execution");
        CollectionAssert.DoesNotContain(referencedAssemblyNames, "Musoq.Converter");
        CollectionAssert.DoesNotContain(referencedAssemblyNames, "Musoq.Targets.CSharpClr");
        CollectionAssert.DoesNotContain(referencedAssemblyNames, "Musoq.Targets.Execution.Analysis");
        Assert.IsFalse(referencedAssemblyNames.Any(static name =>
            name != null && name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)));

        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.TestPortable",
            "Musoq.Targets.TestPortable.csproj");
        var project = XDocument.Load(projectPath);
        var projectReferences = project
            .Descendants("ProjectReference")
            .Select(static element => Path.GetFileNameWithoutExtension(element.Attribute("Include")!.Value.Replace('\\', '/')))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "Musoq.Evaluator",
                "Musoq.Targets.Abstractions",
                "Musoq.Targets.Execution"
            },
            projectReferences);
        Assert.IsFalse(project.Descendants("PackageReference").Any());

        var forbiddenSourceTokens = new[]
        {
            "System.Reflection",
            "Microsoft.CodeAnalysis",
            "Musoq.Converter",
            "Musoq.Targets.CSharpClr",
            "Musoq.Targets.Execution.Analysis",
            "Assembly.Load",
            "Activator.CreateInstance",
            "ITableRunnable",
            "ITypedRunnable",
            ".ClrMethod",
            "RequireClrType",
            "RequireClrMethod"
        };
        var sourceDirectory = Path.GetDirectoryName(projectPath)!;
        Assert.IsFalse(File.Exists(Path.Combine(sourceDirectory, "PortableSubsetHostAbiInventory.cs")));
        StringAssert.Contains(
            File.ReadAllText(Path.Combine(sourceDirectory, "PortableSubsetExecutionBackend.cs")),
            "TargetHostAbiInventoryBuilder.Build");
        var sourceOffenders = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => forbiddenSourceTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(repositoryRoot, file)}:{token}"))
            .ToArray();
        Assert.IsEmpty(
            sourceOffenders,
            "Portable test target source must not access CLR sidecars, CLR activation, Roslyn, converter, or target implementation packages: " +
            string.Join(", ", sourceOffenders));
        var clrTypeSidecarOffenders = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => Regex.IsMatch(File.ReadAllText(file), @"\.ClrType\b(?!Usage)"))
            .Select(file => Path.GetRelativePath(repositoryRoot, file))
            .ToArray();
        Assert.IsEmpty(
            clrTypeSidecarOffenders,
            "Portable test target source must not access ExecutionTypeRef.ClrType sidecars: " +
            string.Join(", ", clrTypeSidecarOffenders));
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldNotBeProductionRegistered()
    {
        var repositoryRoot = FindRepositoryRoot();
        var converterProject = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Musoq.Converter.csproj"));
        var converterSourceFiles = Directory.EnumerateFiles(
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter"),
            "*.cs",
            SearchOption.AllDirectories);
        var productionReferences = converterSourceFiles
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal) ||
                       text.Contains("PortableSubsetTarget", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsFalse(converterProject.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal));
        Assert.IsEmpty(productionReferences);
        Assert.ThrowsExactly<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveBackend(PortableSubsetTarget.TargetId));
    }

    [TestMethod]
    public void PortableSubsetTargetDescriptor_ShouldExposeNoClrActivationPhase()
    {
        var descriptor = ExecutionTargetDescriptor.Create(
            PortableSubsetTarget.TargetId,
            renderPhase: new PortableSubsetExecutionBackend(),
            finalizationPhase: new PortableSubsetRenderedQueryFinalizer(),
            inspectionPhase: new PortableSubsetRenderedQueryInspector(),
            createRenderInputs: static _ => new EmptyTargetBackendRenderInputs(PortableSubsetTarget.TargetId),
            createArtifactPackage: static context => TargetArtifactPackage.CreatePortableExportPackage(
                context.TargetId,
                "PortableSubsetProgram",
                Assert.IsInstanceOfType<TargetExportArtifact>(context.ExecutableArtifact),
                context.SemanticsContract,
                executionIrVersion: context.ExecutionIrVersion));

        Assert.IsNull(descriptor.ActivationPhase);
        Assert.AreEqual(PortableSubsetTarget.TargetId, descriptor.TargetId);
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldDeclareItsCapabilityProfileExplicitly()
    {
        var capabilities = PortableSubsetTarget.Capabilities;

        Assert.IsTrue(capabilities.SupportedOperations.Contains(new ExecutionOperationId("expr.binary")));
        Assert.IsTrue(capabilities.SupportedFeatureKinds.Contains(ExecutionTargetFeatureKind.BinaryOperation));
        Assert.IsTrue(capabilities.SupportedFeatureKinds.Contains(ExecutionTargetFeatureKind.Callable));
        Assert.IsFalse(capabilities.SupportedRequirementKinds.Contains(ExecutionTargetRequirementKind.ClrOnlyConstant));
        Assert.IsTrue(capabilities.SupportedTypeSymbolPortabilities.Contains(ExecutionPortableSymbolPortability.ClrOnly));
        Assert.IsTrue(capabilities.SupportedCallableSymbolPortabilities.Contains(ExecutionPortableSymbolPortability.ClrOnly));
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldNotRepresentObjectConstructionAsNoOp()
    {
        var plan = new ExecutionPlan(
            "Q_PortableObjectConstruction",
            [],
            new ExecutionBlock(
            [
                new ExecutionCreateObject(new ExecutionVariable("library", typeof(object)))
            ]));

        var exception = Assert.ThrowsExactly<PortableSubsetLoweringException>(
            () => PortableSubsetLowerer.Lower(plan));

        StringAssert.Contains(exception.Message, "CLR object construction");
        Assert.IsNull(typeof(PortableSubsetProgram).Assembly.GetType(
            "Musoq.Targets.TestPortable.PortableNoOpInstruction"));
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldNotTreatArbitraryCoalesceNamedMethodAsIntrinsic()
    {
        var method = typeof(PortableSubsetTargetAssemblyTests).GetMethod(
            nameof(CustomCoalesce),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var call = new ExecutionMethodCall(
            ExecutionClrBindingFactory.FromClr(method),
            [
                new ExecutionLiteral("left", typeof(string)),
                new ExecutionLiteral("right", typeof(string))
            ],
            "Coalesce",
            ExecutionClrBindingFactory.FromClr(typeof(string)),
            null);
        var plan = new ExecutionPlan(
            "Q_CustomCoalesce",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(new ExecutionVariable("value", typeof(string)), call)
            ]));

        var exception = Assert.ThrowsExactly<PortableSubsetLoweringException>(
            () => PortableSubsetLowerer.Lower(plan));

        StringAssert.Contains(exception.Message, call.Method.StableId);
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldEncodeCanonicalNonObjectLiteralValues()
    {
        var timestamp = new DateTime(638400000000000000, DateTimeKind.Utc);
        var identifier = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        var plan = new ExecutionPlan(
            "Q_PortableConstants",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(new ExecutionVariable("character", typeof(char)), new ExecutionLiteral('A', typeof(char))),
                new ExecutionLet(new ExecutionVariable("float", typeof(float)), new ExecutionLiteral(float.NaN, typeof(float))),
                new ExecutionLet(new ExecutionVariable("timestamp", typeof(DateTime)), new ExecutionLiteral(timestamp, typeof(DateTime))),
                new ExecutionLet(new ExecutionVariable("identifier", typeof(Guid)), new ExecutionLiteral(identifier, typeof(Guid))),
                new ExecutionLet(new ExecutionVariable("enum", typeof(TestConstantEnum)), new ExecutionLiteral(TestConstantEnum.Second, typeof(TestConstantEnum)))
            ]));

        var manifest = PortableSubsetLowerer.Lower(plan).CreateManifest();

        StringAssert.Contains(manifest, "char:0041");
        StringAssert.Contains(manifest, "f32:00000000FFC00000");
        StringAssert.Contains(manifest, "datetime:638400000000000000:Utc");
        StringAssert.Contains(manifest, "guid:00112233445566778899AABBCCDDEEFF");
        StringAssert.Contains(manifest, "enum:");
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldReceivePortableTypeDescriptorsWithoutClrSidecars()
    {
        var descriptor = typeof(ExecutionTypeRef).GetProperty(nameof(ExecutionTypeRef.Descriptor));
        var clrType = typeof(ExecutionTypeRef).GetProperty(
            "ClrType",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(descriptor);
        Assert.IsTrue(descriptor.GetMethod!.IsPublic);
        Assert.AreEqual("Musoq.Targets.Abstractions.ExecutionPortableTypeDescriptor", descriptor.PropertyType.FullName);
        Assert.IsNull(clrType);
    }

    [TestMethod]
    public void PortableSubsetTarget_ShouldReceivePortableCallableDescriptorsWithoutReflection()
    {
        var descriptor = typeof(ExecutionCallableRef).GetProperty(nameof(ExecutionCallableRef.Descriptor));
        var clrMethod = typeof(ExecutionCallableRef).GetProperty(
            "ClrMethod",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(descriptor);
        Assert.IsTrue(descriptor.GetMethod!.IsPublic);
        Assert.AreEqual("Musoq.Targets.Abstractions.ExecutionPortableCallableDescriptor", descriptor.PropertyType.FullName);
        Assert.IsNull(clrMethod);
    }

    [TestMethod]
    public void TargetCatalog_WhenRenderContractVersionIsUnsupported_ShouldRejectBeforeLowering()
    {
        var descriptor = ExecutionTargetDescriptor.Create(
            PortableSubsetTarget.TargetId,
            renderPhase: new PortableSubsetExecutionBackend(),
            createRenderInputs: static _ => new EmptyTargetBackendRenderInputs(PortableSubsetTarget.TargetId));
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);
        var plan = CreateLiteralPlan() with
        {
            ExecutionIrVersion = TargetContractVersions.ExecutionIr + 1
        };
        var request = CreateRequest(plan);

        var result = ExecutionTargetCatalog.Render(request);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(static diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedLowering &&
            diagnostic.Message.Contains("IR version", StringComparison.Ordinal)));
    }

    private static ExecutionPlan CreateLiteralPlan()
    {
        return new ExecutionPlan(
            "Q_PortableSubset",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("value", typeof(int)),
                    new ExecutionLiteral(1, typeof(int)))
            ]));
    }

    private static string CustomCoalesce(string left, string right) => left ?? right;

    private enum TestConstantEnum : short
    {
        First = 1,
        Second = 2
    }

    private static TargetRenderRequest CreateRequest(ExecutionPlan plan)
    {
        return new TargetRenderRequest
        {
            TargetId = PortableSubsetTarget.TargetId,
            Identity = new TargetRenderIdentity("PortableSubset"),
            Options = TargetRenderOptions.Empty,
            ScriptBinding = TargetScriptBindingContract.Empty,
            References = TargetReferenceInventory.Empty,
            ExecutionPlan = plan,
            ExecutionIrVersion = plan.ExecutionIrVersion,
            SemanticsContract = plan.SemanticsContract,
            OperationReport = ExecutionTargetOperationAnalyzer.Analyze(plan),
            FeatureReport = ExecutionTargetFeatureAnalyzer.Analyze(plan),
            CompatibilityReport = new ExecutionTargetCompatibilityReport([]),
            RuntimeContract = new TargetRuntimeContract(
                plan.Identifier,
                [],
                [],
                [],
                new TargetNullBehaviorContract(false, false, false, "none"),
                new TargetCancellationContract(false, false),
                new TargetDiagnosticsContract(false, false, false),
                new TargetProfilingContract(false, false, 0, 0)),
            HostAbiVersion = TargetContractVersions.HostAbi,
            BackendInputs = new EmptyTargetBackendRenderInputs(PortableSubsetTarget.TargetId)
        };
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "dotnet", "Musoq.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
