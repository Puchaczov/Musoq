using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetRenderPurposeTests
{
    [TestMethod]
    public void CompilationPurposes_MapToStableTargetPurposes()
    {
        Assert.AreEqual(
            TargetRenderPurpose.Execution,
            TargetRenderPurposeFactory.CreatePurpose(CompilationPurpose.Execution));
        Assert.AreEqual(
            TargetRenderPurpose.Inspection,
            TargetRenderPurposeFactory.CreatePurpose(CompilationPurpose.Inspection));
        Assert.AreEqual(
            TargetRenderPurpose.PortablePackaging,
            TargetRenderPurposeFactory.CreatePurpose(CompilationPurpose.PortableArtifactPackaging));
        Assert.AreEqual(
            TargetRenderPurpose.StrictValidation,
            TargetRenderPurposeFactory.CreatePurpose(CompilationPurpose.ArtifactValidation));
    }

    [TestMethod]
    public void ExecutionWithoutPdb_UsesFastProfile_AndArtifactPurposesUseStableProfile()
    {
        Assert.AreEqual(
            TargetRenderProfile.ExecutionFast,
            TargetRenderPurposeFactory.CreateProfile(CompilationPurpose.Execution, emitPdb: false));
        Assert.AreEqual(
            TargetRenderProfile.StableArtifact,
            TargetRenderPurposeFactory.CreateProfile(CompilationPurpose.Execution, emitPdb: true));
        Assert.AreEqual(
            TargetRenderProfile.StableArtifact,
            TargetRenderPurposeFactory.CreateProfile(CompilationPurpose.Inspection, emitPdb: false));
    }

    [TestMethod]
    public void CacheIdentity_SeparatesRenderProfiles()
    {
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();
        var fast = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            "select 1",
            provider,
            options,
            ExecutionTargetIds.CSharpClr,
            TargetRenderProfile.ExecutionFast);
        var stable = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            "select 1",
            provider,
            options,
            ExecutionTargetIds.CSharpClr,
            TargetRenderProfile.StableArtifact);

        Assert.AreNotEqual(fast, stable);
    }
}
