using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RuntimeDynamicConstantSample_ShouldKeepConcreteRootAndAvoidDynamicReads()
    {
        var source = ReadSample("Q231_PublicDynamicRootConstant.cs").Content;
        var generated = GeneratedSection(source);

        Assert.Contains(
            "SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>",
            generated);
        Assert.DoesNotContain("SourceInvocation<object>", generated);
        Assert.DoesNotContain("SourceInvocation<dynamic>", generated);
        Assert.DoesNotContain("((dynamic)", generated);
        Assert.DoesNotContain("ExpandoAdapter", generated);
        Assert.DoesNotContain("GeneratedDictionaryAccess", generated);
    }

    [TestMethod]
    public void RuntimeDynamicRootFilterProjectionSample_ShouldUseCanonicalReadsAndTypedSource()
    {
        var source = ReadSample("Q232_PublicDynamicRootFilterProjection.cs").Content;
        var generated = GeneratedSection(source);

        Assert.Contains(
            "SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>",
            generated);
        Assert.DoesNotContain("((dynamic)", generated);
        Assert.Contains(".RuntimeKey", generated);
        Assert.Contains(".Enabled", generated);
        Assert.Contains(".Metric", generated);
        Assert.Contains(".Payload", generated);
        Assert.Contains("int runtimeKey = ko3iko.RuntimeKey;", generated);
        Assert.DoesNotContain("2 == (int)(object)((dynamic)ko3iko).RuntimeKey", generated);
        Assert.DoesNotContain("SourceInvocation<object>", generated);
        Assert.DoesNotContain("ExpandoAdapter", generated);
        Assert.DoesNotContain("GeneratedDictionaryAccess", generated);
        Assert.DoesNotContain("System.Reflection", generated);
    }

    [TestMethod]
    public void RuntimeDynamicNestedSample_ShouldCastLeavesAndKeepNullGuardedPath()
    {
        var source = ReadSample("Q233_PublicDynamicNestedNullable.cs").Content;
        var generated = GeneratedSection(source);

        Assert.Contains(".Branch", generated);
        Assert.Contains(".Measurement", generated);
        Assert.Contains(".Raw", generated);
        Assert.DoesNotContain("(double)(object)", generated);
        Assert.Contains("branch.Measurement", generated);
        Assert.DoesNotContain("(ulong)(object)", generated);
        Assert.Contains("branch.Raw", generated);
        Assert.DoesNotContain("GetNestedValue", generated);
        Assert.DoesNotContain("GeneratedDictionaryAccess", generated);
        Assert.DoesNotContain("new Expando", generated);
    }

    [TestMethod]
    public void RuntimeDynamicJoinMethodSample_ShouldKeepJoinAndLibraryCallsStatic()
    {
        var source = ReadSample("Q234_PublicDynamicJoinMethod.cs").Content;
        var generated = GeneratedSection(source);

        Assert.Contains(
            "SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>",
            generated);
        Assert.Contains(
            "SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>",
            generated);
        Assert.Contains("Scale(", generated);
        Assert.DoesNotContain("dynamic).Scale", generated);
        Assert.DoesNotContain("GetMember", generated);
        Assert.DoesNotContain("dynamic)l", generated);
        Assert.DoesNotContain("SourceInvocation<object>", generated);
    }

    private static string GeneratedSection(string source)
    {
        var marker = source.IndexOf("// === Generated C# ===", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, marker);
        return source[marker..];
    }
}
