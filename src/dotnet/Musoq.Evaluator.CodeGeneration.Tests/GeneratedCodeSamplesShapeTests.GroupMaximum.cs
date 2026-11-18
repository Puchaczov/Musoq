using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static void AssertGroupedMaximumSample(string sample)
    {
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", sample);
        Assert.Contains("CreateGroupMaximum", sample);
        var maximumCode = ExtractGeneratedCodeSection(sample);
        var nullKeyGuard = maximumCode.IndexOf("key1 == null", StringComparison.Ordinal);
        var buildValueRead = maximumCode.IndexOf("= b.Population;", StringComparison.Ordinal);
        Assert.IsTrue(
            nullKeyGuard >= 0 && buildValueRead > nullKeyGuard,
            "A null correlation key must skip the inner value read."
        );
        Assert.Contains("UpdateGroupMaximum", sample);
        Assert.DoesNotContain("HashJoinBucket<", ExtractGeneratedCodeSection(sample));
        Assert.IsFalse(
            ExtractGeneratedCodeSection(sample)
                .Contains(
                    "public readonly Musoq.Evaluator.Tests.Schema.Basic.BasicEntity __context0;",
                    StringComparison.Ordinal
                ),
            "Q142 payload should not store unused source row contexts."
        );
        Assert.IsFalse(
            ExtractGeneratedCodeSection(sample)
                .Contains("public readonly int _sq_1_greater_all_key;", StringComparison.Ordinal),
            "Q142 payload should not store key-only constant fields."
        );
        Assert.IsFalse(
            ExtractGeneratedCodeSection(sample)
                .Contains("public readonly string _sq_1_corr_0;", StringComparison.Ordinal),
            "Q142 payload should not store key-only correlation fields."
        );
        Assert.IsFalse(
            sample.Contains("private sealed class Cte0Row0 : Row", StringComparison.Ordinal),
            "Q142 should avoid generated Row payloads in the fused hash build."
        );
        Assert.IsFalse(
            ExtractGeneratedCodeSection(sample).Contains("_tableResults[0]", StringComparison.Ordinal),
            "Q142 should fuse the generated subquery directly into the residual hash build."
        );
    }
}
