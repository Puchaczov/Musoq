using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderMethod_ShouldEmitDirectExpressionsWithoutRuntimeDiagnosticWrappers()
    {
        var code = new ExecutionCSharpRenderer()
            .RenderMethod(CreateMethodCallPlan(), "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.IsFalse(code.Contains("RuntimeExpressionBoundary", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("RuntimeExpressionOrigin", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsScalarMethodCall_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();

        Assert.IsTrue(renderer.CanRender(CreateMethodCallPlan()), renderer.GetUnsupportedReason(CreateMethodCallPlan()));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsScalarMethodCall_ShouldRenderLibraryInvocation()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateMethodCallPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((string)libraryBase0.ToUpper(p.Name)));",
            code);
        Assert.Contains("var libraryBase0 = new Musoq.Plugins.LibraryBase();", code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenBoundOverloadReceivesNull_ShouldPreserveParameterType()
    {
        var resultType = typeof(CorrelatedScalarSubqueryResult<decimal?>?);
        var method = typeof(LibraryBase).GetMethod(
            "__CorrelatedScalarSubqueryResult",
            [resultType])!;
        var target = new ExecutionVariable("libraryBase0", typeof(LibraryBase));
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(null, resultType)],
            null,
            typeof(decimal?),
            null,
            target);
        var renderer = new ExecutionCSharpRenderer();

        var code = renderer
            .RenderMethod(CreateProjectionPlan("Q_TypedNullMethodArgument", "Value", typeof(decimal?), call), "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("CorrelatedScalarSubqueryResult<decimal?>? )null", code);
    }

    [TestMethod]
    public void CanRender_WhenReusableMethodCallIsUnbound_ShouldReturnFalseWithClearReason()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateProjectionPlan("Q_UnboundMethodCall", "UpperName", typeof(string), CreateUnboundToUpperNameCall());

        Assert.IsFalse(renderer.CanRender(plan));
        Assert.Contains("requires a reusable target assigned by MethodTargetReusePass", renderer.GetUnsupportedReason(plan) ?? string.Empty);
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsMethodCallInsideStringConcatenation_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateMethodCallInsideBinaryPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsMethodCallInsideStringConcatenation_ShouldRenderLibraryInvocation()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateMethodCallInsideBinaryPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0(((string)libraryBase0.ToUpper(p.Name) + \"!\")));",
            code);
        Assert.Contains("var libraryBase0 = new Musoq.Plugins.LibraryBase();", code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsMethodCallInsideArithmeticBinary_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateMethodCallInsideArithmeticBinaryPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsMethodCallInsideArithmeticBinary_ShouldCastInferredResult()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateMethodCallInsideArithmeticBinaryPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((uint)((int)Math.Abs(1) + 1u)));",
            code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsMethodCallInsideUnary_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateMethodCallInsideUnaryPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsMethodCallInsideUnary_ShouldCastInferredResult()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateMethodCallInsideUnaryPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((int)(-(int)Math.Abs(1))));",
            code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsNullableMethodCallInsideUnary_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateNullableMethodCallInsideUnaryPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsNullableMethodCallInsideUnary_ShouldCastInferredResult()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateNullableMethodCallInsideUnaryPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((float? )(-(float? )libraryBase0.ToFloat(1))));",
            code);
        Assert.Contains("var libraryBase0 = new Musoq.Plugins.LibraryBase();", code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsNullableMethodCallInsideBinary_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateNullableMethodCallInsideBinaryPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsNullableMethodCallInsideBinary_ShouldCastInferredResult()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateNullableMethodCallInsideBinaryPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((float)((float? )libraryBase0.ToFloat(1) + 1ul)));",
            code);
        Assert.Contains("var libraryBase0 = new Musoq.Plugins.LibraryBase();", code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsNullableTemporalSubtraction_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateNullableTemporalSubtractionPlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_WhenPlanContainsWindowRenderNodes_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateWindowRenderNodePlan();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsNullableTemporalSubtraction_ShouldPreserveNullResult()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateNullableTemporalSubtractionPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0((p.Start - p.End)));",
            code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".GetValueOrDefault()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsProjectTable_ShouldConstructGeneratedRows()
    {
        var plan = CreateProjectTablePlan();
        var renderer = new ExecutionCSharpRenderer();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var classCode = string.Join(
            Environment.NewLine,
            renderer.RenderClassMembers(plan).Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains(
            "result.Add(new ResultRow0((string)resultSourceRow[0]));",
            methodCode);
        Assert.IsFalse(methodCode.Contains("resultSourceRow.Contexts", StringComparison.Ordinal));
        Assert.IsFalse(classCode.Contains("object[] __contexts", StringComparison.Ordinal));
        Assert.IsFalse(classCode.Contains("public override object[] Contexts", StringComparison.Ordinal));
        Assert.IsFalse(methodCode.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsCharStringEquality_ShouldRenderCharLiteralComparison()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateCharStringEqualityPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "result.Add(new ResultRow0(('A' == 'A')));",
            code);
        Assert.IsFalse(code.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenStringMatchNeedleHasNoUnicodeFold_ShouldUseOnlyDirectBcl()
    {
        var match = new ExecutionStringMatch(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            "Google%",
            "Google",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var plan = CreateProjectionPlan("Q_StringMatch", "Matches", typeof(bool), match);

        var code = new ExecutionCSharpRenderer()
            .RenderMethod(plan, "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("is string __musoqStringMatch1", code);
        Assert.Contains("__musoqStringMatch1.StartsWith(\"Google\", StringComparison.OrdinalIgnoreCase)", code);
        Assert.DoesNotContain("Operators.IsOrdinalIgnoreCaseLikeCompatible()", code);
        Assert.DoesNotContain("Operators.IsAsciiLikeSpan(__musoqStringMatch1", code);
        Assert.DoesNotContain("Operators.LikeLegacyRegex", code);
        Assert.DoesNotContain("Operators.Like(", code);
        Assert.DoesNotContain("new Operators()", code);
    }

    [TestMethod]
    [DataRow("K%", "K")]
    [DataRow("S%", "S")]
    public void RenderMethod_WhenStringMatchNeedleHasInvariantUnicodeFold_ShouldRetainFallback(
        string pattern,
        string needle)
    {
        var match = new ExecutionStringMatch(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            pattern,
            needle,
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var plan = CreateProjectionPlan("Q_StringMatchUnicodeFold", "Matches", typeof(bool), match);

        var code = new ExecutionCSharpRenderer()
            .RenderMethod(plan, "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("[0] <= 127", code);
        Assert.Contains("Operators.LikeLegacyRegexSingleAsciiPrefix", code);
        Assert.IsTrue(
            code.IndexOf("[0] ==", StringComparison.Ordinal) <
            code.IndexOf("[0] <= 127", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenStringMatchNeedleContainsI_ShouldGuardOrdinalShortcutOnce()
    {
        var match = new ExecutionStringMatch(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            "I%",
            "I",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var plan = CreateProjectionPlan("Q_StringMatchCulture", "Matches", typeof(bool), match);

        var code = new ExecutionCSharpRenderer()
            .RenderMethod(plan, "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.AreEqual(
            1,
            code.Split("Operators.IsOrdinalIgnoreCaseLikeCompatible()", StringSplitOptions.None).Length - 1);
        Assert.Contains("?", code);
        Assert.Contains("__musoqStringMatch1[0] <= 127", code);
        Assert.Contains("__musoqStringMatch1[0] == 'I'", code);
        Assert.Contains("(__musoqStringMatch1[0]) | 32", code);
        Assert.DoesNotContain("Operators.IsAsciiLikeSpan(__musoqStringMatch1", code);
        Assert.Contains("Operators.LikeLegacyRegexSingleAsciiPrefix(__musoqStringMatch1, \"I%\", \"I\")", code);
        Assert.IsTrue(
            code.IndexOf("__musoqStringMatch1[0] == 'I'", StringComparison.Ordinal) <
            code.IndexOf("__musoqStringMatch1[0] <= 127", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsTwoStringMatches_ShouldAllocateCollisionSafeNames()
    {
        var first = new ExecutionStringMatch(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            "A%",
            "A",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var second = new ExecutionStringMatch(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            "%Z",
            "Z",
            ExecutionStringMatchKind.Suffix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var both = new ExecutionBinary(BinaryOpKind.And, first, second, typeof(bool));

        var code = new ExecutionCSharpRenderer()
            .RenderMethod(CreateProjectionPlan("Q_TwoStringMatches", "Matches", typeof(bool), both), "ExecutePlan")
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("__musoqStringMatch1", code);
        Assert.Contains("__musoqStringMatch2", code);
    }

    [TestMethod]
    public void RenderExpression_WhenPlanUsesPreparedAndDynamicLike_ShouldUseStaticMatcherAbi()
    {
        var matcherType = ExecutionClrBindingFactory.FromClr(typeof(PreparedLikeMatcher));
        var cacheSlotType = ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));
        var boolType = ExecutionClrBindingFactory.FromClr(typeof(bool));
        var matcherVariable = new ExecutionVariable("matcher", matcherType);
        var cacheVariable = new ExecutionVariable("likeCache", cacheSlotType);
        var renderer = new ExecutionCSharpRenderer();
        var prepare = new ExecutionPrepareLikeMatcher(
            new ExecutionLiteral("a%", typeof(string)),
            ExecutionStringMatchComparison.LikeIgnoreCase,
            matcherType);
        var prepared = new ExecutionPreparedLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            new ExecutionVariableRead(matcherVariable),
            boolType);
        var dynamic = new ExecutionDynamicLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            new ExecutionLiteral("a%", typeof(string)),
            new ExecutionVariableRead(cacheVariable),
            ExecutionStringMatchComparison.LikeIgnoreCase,
            boolType);
        var cacheSlot = new ExecutionLikeMatcherCacheSlot(cacheSlotType);

        Assert.AreEqual("Operators.PrepareLike(\"a%\")", Render(prepare));
        Assert.AreEqual("Operators.LikePrepared(\"alpha\", matcher)", Render(prepared));
        Assert.AreEqual("Operators.LikeDynamic(\"alpha\", \"a%\", likeCache)", Render(dynamic));
        Assert.AreEqual("new Musoq.Evaluator.LikeMatcherCacheSlot(false)", Render(cacheSlot));
        Assert.AreEqual(
            "new Musoq.Evaluator.LikeMatcherCacheSlot(true)",
            Render(new ExecutionLikeMatcherCacheSlot(true, cacheSlotType)));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderExpression(prepare));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderExpression(prepared));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderExpression(dynamic));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderExpression(cacheSlot));

        string Render(ExecutionExpression expression) => renderer.RenderExpression(expression)
            .NormalizeWhitespace()
            .ToFullString();
    }

    [TestMethod]
    public void RenderExpression_WhenDynamicLikeUsesEffectfulOperands_ShouldEmitEachOnceInLeftToRightArgumentOrder()
    {
        var cacheSlotType = ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));
        var dynamic = new ExecutionDynamicLikeMatch(
            new ExecutionFieldRead("p", "Input", typeof(string)),
            new ExecutionFieldRead("p", "Pattern", typeof(string)),
            new ExecutionVariableRead(new ExecutionVariable("likeCache", cacheSlotType)),
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));

        var code = new ExecutionCSharpRenderer()
            .RenderExpression(dynamic)
            .NormalizeWhitespace()
            .ToFullString();
        var inputRead = "p.Input";
        var patternRead = "p.Pattern";

        Assert.HasCount(2, code.Split(inputRead, StringSplitOptions.None));
        Assert.HasCount(2, code.Split(patternRead, StringSplitOptions.None));
        Assert.IsLessThan(code.IndexOf(patternRead, StringComparison.Ordinal), code.IndexOf(inputRead, StringComparison.Ordinal));
        Assert.AreEqual("Operators.LikeDynamic(p.Input, p.Pattern, likeCache)", code);
    }
}
