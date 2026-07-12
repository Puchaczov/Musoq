using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CSharpClrRenderedQueryFinalizerTests
{
    [TestMethod]
    public void Finalize_WhenCSharpCompilationSucceeds_ShouldReturnClrAssemblyArtifact()
    {
        var compilation = CreateValidCompilation();
        var rendering = new RenderingBuildArtifacts(
            new CSharpRenderedQueryArtifact(compilation, "FinalizerSmoke.CompiledQuery"));
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);

        var result = (CSharpClrFinalizationResult)finalizer.Finalize(
            rendering.Artifact,
            new CSharpClrFinalizationOptions(false));

        Assert.IsTrue(result.EmitResult.Success);
        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Diagnostics);
        var artifact = (ClrAssemblyExecutableArtifact)result.Artifact!;
        Assert.IsTrue(artifact.DllFile.Length > 0);
        Assert.IsNull(artifact.PdbFile);
        Assert.AreEqual("FinalizerSmoke.CompiledQuery", artifact.RunnableTypeName);
    }

    [TestMethod]
    public void Finalize_WhenPdbIsEnabled_ShouldReturnPortablePdbBytes()
    {
        var compilation = CreateValidCompilation();
        var rendering = new RenderingBuildArtifacts(
            new CSharpRenderedQueryArtifact(compilation, "FinalizerSmoke.CompiledQuery"));
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);

        var result = (CSharpClrFinalizationResult)finalizer.Finalize(
            rendering.Artifact,
            new CSharpClrFinalizationOptions(true));

        Assert.IsTrue(result.EmitResult.Success);
        var artifact = (ClrAssemblyExecutableArtifact)result.Artifact!;
        Assert.IsTrue(artifact.DllFile.Length > 0);
        Assert.IsNotNull(artifact.PdbFile);
        Assert.IsTrue(artifact.PdbFile.Length > 0);
    }

    [TestMethod]
    public void Finalize_WhenCSharpCompilationFails_ShouldExposeTargetNeutralDiagnostics()
    {
        var compilation = CreateInvalidCompilation();
        var rendering = new RenderingBuildArtifacts(
            new CSharpRenderedQueryArtifact(compilation, "FinalizerSmoke.CompiledQuery"));
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);

        var result = (CSharpClrFinalizationResult)finalizer.Finalize(
            rendering.Artifact,
            new CSharpClrFinalizationOptions(false));

        Assert.IsFalse(result.EmitResult.Success);
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Artifact);
        Assert.IsTrue(result.Diagnostics.Any(static diagnostic =>
            diagnostic.Severity == TargetDiagnosticSeverity.Error &&
            diagnostic.Code.StartsWith("CS", System.StringComparison.Ordinal) &&
            diagnostic.Message.Contains("CS", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Finalize_WhenArtifactTypeIsWrong_ShouldReportExpectedAndActualTargetDetails()
    {
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);
        var artifact = new FakeRenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

        var exception = Assert.Throws<System.InvalidOperationException>(
            () => finalizer.Finalize(artifact, new CSharpClrFinalizationOptions(false)));

        StringAssert.Contains(exception.Message, nameof(CSharpRenderedQueryArtifact));
        StringAssert.Contains(exception.Message, ExecutionTargetIds.CSharpClr.ToString());
        StringAssert.Contains(exception.Message, nameof(FakeRenderedQueryArtifact));
        StringAssert.Contains(exception.Message, TestExecutionTargetIds.TestOnlyNonClr.ToString());
    }

    [TestMethod]
    public void Finalize_WhenOptionsTypeIsWrong_ShouldReportExpectedAndActualOptionTypes()
    {
        var compilation = CreateValidCompilation();
        var rendering = new RenderingBuildArtifacts(
            new CSharpRenderedQueryArtifact(compilation, "FinalizerSmoke.CompiledQuery"));
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);

        var exception = Assert.Throws<System.NotSupportedException>(
            () => finalizer.Finalize(rendering.Artifact, TargetFinalizationOptions.Empty));

        StringAssert.Contains(exception.Message, nameof(CSharpClrFinalizationOptions));
        StringAssert.Contains(exception.Message, TargetFinalizationOptions.Empty.GetType().Name);
    }

    [TestMethod]
    public void CompilationArtifacts_WhenSet_ShouldSynchronizeLegacyDllAndPdbFields()
    {
        var dll = new byte[] { 1, 2, 3 };
        var pdb = new byte[] { 4, 5 };
        var executable = new ClrAssemblyExecutableArtifact(dll, pdb, "Runnable");
        var emitResult = CreateValidCompilation().Emit(new System.IO.MemoryStream());
        var items = new BuildItems
        {
            CompilationArtifacts = new CompilationBuildArtifacts(emitResult, executable)
        };

        dll[0] = 9;
        pdb[0] = 9;
        var exposedDll = items.DllFile!;
        var exposedPdb = items.PdbFile!;
        exposedDll[1] = 9;
        exposedPdb[1] = 9;

        Assert.AreSame(executable, items.ExecutableArtifact);
        Assert.AreNotSame(dll, items.DllFile);
        Assert.AreNotSame(pdb, items.PdbFile);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, items.DllFile);
        CollectionAssert.AreEqual(new byte[] { 4, 5 }, items.PdbFile);
    }

    private static CSharpCompilation CreateValidCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace FinalizerSmoke { public sealed class CompiledQuery { } }");
        return CreateCompilation(syntaxTree);
    }

    private static CSharpCompilation CreateInvalidCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace FinalizerSmoke { public sealed class CompiledQuery { public void Broken( } }");
        return CreateCompilation(syntaxTree);
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "FinalizerSmoke",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed record FakeRenderedQueryArtifact(ExecutionTargetId Target)
        : RenderedQueryArtifact(Target);
}
