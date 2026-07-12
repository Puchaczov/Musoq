using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
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

        var expected = """
            private readonly struct DHashPayload0
            {
                public readonly string b_City;
                public readonly string b_Country;
                public DHashPayload0(string b_City, string b_Country)
                {
                    this.b_City = b_City;
                    this.b_Country = b_Country;
                }
            }
            """;

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
