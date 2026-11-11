using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsHashPayloadShape_ShouldEmitReadonlyStruct()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateHashPayloadShapePlan();
        var code = RenderClassMembersCode(renderer, plan);

        var expected = (
            "private readonly struct DHashPayload0\n"
            + "{\n"
            + "    public readonly string b_City;\n"
            + "    public readonly string b_Country;\n"
            + "    public DHashPayload0(string b_City, string b_Country)\n"
            + "    {\n"
            + "        this.b_City = b_City;\n"
            + "        this.b_Country = b_Country;\n"
            + "    }\n"
            + "}"
        );

        Assert.Contains(Normalize(expected), Normalize(code));
        Assert.IsFalse(code.Contains(": Row", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("public override object this[int columnNumber]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("public override object[] Contexts", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenHashPayloadFeedsHashJoin_ShouldEmitPayloadBucketAndFieldReads()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateHashPayloadJoinPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var dHash = new Dictionary<string, HashJoinBucket<DHashPayload0>>();", code);
        Assert.Contains("DHashPayload0 d = new DHashPayload0(b.City, b.Country);", code);
        Assert.Contains("string key = d.b_Country;", code);
        Assert.Contains("new HashJoinBucket<DHashPayload0>(d)", code);
    }
}
