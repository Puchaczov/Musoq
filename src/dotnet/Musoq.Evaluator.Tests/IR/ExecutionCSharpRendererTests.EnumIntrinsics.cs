using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderExpression_WhenEnumName_ShouldEmitCanonicalPrimitiveSwitch()
    {
        var descriptor = CreateStatusDescriptor();
        var expression = CreateEnumIntrinsicCall(
            EnumIntrinsicKind.EnumName,
            new ExecutionFieldRead("p", "Status", typeof(int?)),
            descriptor);

        var code = new ExecutionCSharpRenderer()
            .RenderExpression(expression)
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("p.Status switch", code);
        Assert.Contains("10 => \"Queued\"", code);
        Assert.Contains("20 => \"Running\"", code);
        Assert.DoesNotContain("20 => \"Active\"", code);
        Assert.Contains("_ => null", code);
        AssertEnumIntrinsicHotPathIsPrimitive(code);
        Assert.AreEqual(1, CountOccurrences(code, "p.Status"));
    }

    [TestMethod]
    public void RenderExpression_WhenIsDefined_ShouldEmitDeduplicatedPrimitivePatterns()
    {
        var descriptor = CreateStatusDescriptor();
        var expression = CreateEnumIntrinsicCall(
            EnumIntrinsicKind.IsDefined,
            new ExecutionFieldRead("p", "Status", typeof(int?)),
            descriptor);

        var code = new ExecutionCSharpRenderer()
            .RenderExpression(expression)
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("10 or 20", code);
        Assert.Contains("=> true", code);
        Assert.Contains("_ => false", code);
        Assert.AreEqual(1, CountOccurrences(code, "20"));
        AssertEnumIntrinsicHotPathIsPrimitive(code);
        Assert.AreEqual(1, CountOccurrences(code, "p.Status"));
    }

    [TestMethod]
    public void RenderExpression_WhenHasAllFlags_ShouldCaptureOperandOnceAndUseDirectMask()
    {
        var descriptor = CreateAccessDescriptor();
        var expression = CreateEnumIntrinsicCall(
            EnumIntrinsicKind.HasAllFlags,
            new ExecutionFieldRead("p", "Access", typeof(uint?)),
            descriptor,
            EnumScalarValue.FromUInt32(3));

        var code = new ExecutionCSharpRenderer()
            .RenderExpression(expression)
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("p.Access switch", code);
        Assert.Contains("uint __enumValue0", code);
        Assert.Contains("__enumValue0 & 3u", code);
        Assert.Contains("== 3u", code);
        Assert.Contains("_ => false", code);
        AssertEnumIntrinsicHotPathIsPrimitive(code);
        Assert.AreEqual(1, CountOccurrences(code, "p.Access"));
    }

    [TestMethod]
    public void RenderExpression_WhenHasAnyFlagsUsesZeroMask_ShouldEmitFalseComparison()
    {
        var descriptor = CreateAccessDescriptor();
        var expression = CreateEnumIntrinsicCall(
            EnumIntrinsicKind.HasAnyFlags,
            new ExecutionFieldRead("p", "Access", typeof(uint?)),
            descriptor,
            EnumScalarValue.FromUInt32(0));

        var code = new ExecutionCSharpRenderer()
            .RenderExpression(expression)
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Contains("(__enumValue0 & 0u) != 0u", code);
        AssertEnumIntrinsicHotPathIsPrimitive(code);
    }

    private static ExecutionMethodCall CreateEnumIntrinsicCall(
        EnumIntrinsicKind kind,
        ExecutionExpression operand,
        EnumTypeDescriptor descriptor,
        EnumScalarValue? mask = null)
    {
        var arguments = mask == null
            ? new ExecutionExpression[] { operand }
            : [operand, new ExecutionLiteral(mask.Value.AsUInt32(), typeof(uint))];
        var returnType = kind == EnumIntrinsicKind.EnumName ? typeof(string) : typeof(bool);

        return new ExecutionMethodCall(
            EnumIntrinsicMethodFacts.Bind(kind, operand.ReturnType.ResolveClrType()),
            arguments,
            null,
            returnType)
        {
            EnumIntrinsic = kind,
            OperandEnumType = descriptor,
            EnumMask = mask
        };
    }

    private static EnumTypeDescriptor CreateStatusDescriptor()
    {
        return new EnumTypeDescriptor(
            "JobStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [
                new EnumMemberDescriptor("Queued", EnumScalarValue.FromInt32(10)),
                new EnumMemberDescriptor("Running", EnumScalarValue.FromInt32(20)),
                new EnumMemberDescriptor("Active", EnumScalarValue.FromInt32(20))
            ]);
    }

    private static EnumTypeDescriptor CreateAccessDescriptor()
    {
        return new EnumTypeDescriptor(
            "FileAccess",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.UInt32,
            true,
            [
                new EnumMemberDescriptor("None", EnumScalarValue.FromUInt32(0)),
                new EnumMemberDescriptor("Read", EnumScalarValue.FromUInt32(1)),
                new EnumMemberDescriptor("Write", EnumScalarValue.FromUInt32(2)),
                new EnumMemberDescriptor("ReadWrite", EnumScalarValue.FromUInt32(3))
            ]);
    }

    private static void AssertEnumIntrinsicHotPathIsPrimitive(string code)
    {
        Assert.DoesNotContain("EnumIntrinsicMarkers", code);
        Assert.DoesNotContain("Enum.Parse", code);
        Assert.DoesNotContain("Enum.ToObject", code);
        Assert.DoesNotContain("Convert.ChangeType", code);
        Assert.DoesNotContain("object", code);
    }
}
