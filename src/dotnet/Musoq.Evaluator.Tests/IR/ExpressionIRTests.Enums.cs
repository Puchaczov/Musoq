using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Converter_WhenMethodReturnsNativeEnum_ShouldAttachPortableDescriptor()
    {
        var method = typeof(EnumMethodFixture).GetMethod(nameof(EnumMethodFixture.GetStatus))!;
        var node = new AccessMethodNode(
            new FunctionToken(nameof(EnumMethodFixture.GetStatus), TextSpan.Empty),
            ArgsListNode.Empty,
            null,
            false,
            method);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<MethodCall>(result);
        Assert.IsNotNull(result.EnumType);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, result.EnumType.Origin);
        Assert.AreEqual(EnumUnderlyingKind.Int16, result.EnumType.UnderlyingKind);
        Assert.AreEqual(typeof(TableContractNativeStatus), result.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenEnumValueIntrinsic_ShouldEraseLogicalIdentityWithoutRuntimeCall()
    {
        var descriptor = CreateStatusDescriptor();
        var converter = new ExpressionConverter(columnEnumTypeResolver: (_, _) => descriptor);
        var operand = new AccessColumnNode("Status", "j", typeof(int?), TextSpan.Empty);
        var node = CreateIntrinsicNode(EnumIntrinsicKind.EnumValue, operand);

        var result = converter.Convert(node);

        Assert.IsInstanceOfType<ColumnRef>(result);
        Assert.AreEqual(typeof(int?), result.ReturnType);
        Assert.IsNull(result.EnumType);
    }

    [TestMethod]
    public void Converter_WhenFlagsIntrinsic_ShouldFreezeDescriptorAndPrimitiveMask()
    {
        var descriptor = CreateAccessDescriptor();
        var converter = new ExpressionConverter(columnEnumTypeResolver: (_, _) => descriptor);
        var operand = new AccessColumnNode("Access", "j", typeof(uint?), TextSpan.Empty);
        var node = CreateIntrinsicNode(
            EnumIntrinsicKind.HasAllFlags,
            operand,
            new IntegerNode(3u, TextSpan.Empty));

        var result = Assert.IsInstanceOfType<MethodCall>(converter.Convert(node));

        Assert.AreEqual(EnumIntrinsicKind.HasAllFlags, result.EnumIntrinsic);
        Assert.AreSame(descriptor, result.OperandEnumType);
        Assert.AreEqual(3u, result.EnumMask!.Value.AsUInt32());
        Assert.IsNull(result.EnumType);
    }

    private static AccessMethodNode CreateIntrinsicNode(
        EnumIntrinsicKind kind,
        AccessColumnNode operand,
        IntegerNode? mask = null)
    {
        var arguments = mask == null
            ? new Node[] { operand }
            : [operand, mask];
        return new AccessMethodNode(
            new FunctionToken(kind.ToString(), TextSpan.Empty),
            new ArgsListNode(arguments),
            null,
            false,
            EnumIntrinsicMethodFacts.Bind(kind, operand.ReturnType));
    }

    private static EnumTypeDescriptor CreateStatusDescriptor()
    {
        return new EnumTypeDescriptor(
            "JobStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [new EnumMemberDescriptor("Running", EnumScalarValue.FromInt32(20))]);
    }

    private static EnumTypeDescriptor CreateAccessDescriptor()
    {
        return new EnumTypeDescriptor(
            "FileAccess",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.UInt32,
            true,
            [
                new EnumMemberDescriptor("Read", EnumScalarValue.FromUInt32(1)),
                new EnumMemberDescriptor("Write", EnumScalarValue.FromUInt32(2))
            ]);
    }

    public static class EnumMethodFixture
    {
        public static TableContractNativeStatus GetStatus()
        {
            return TableContractNativeStatus.Running;
        }
    }
}
