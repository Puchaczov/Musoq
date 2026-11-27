using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryComputedOperatorTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("int le", "FEFFFFFF", "long le", "2100000000000000", ">>", -1, typeof(int?))]
    [DataRow("uint le", "00000080", "long le", "2100000000000000", ">>", 1073741824u, typeof(uint?))]
    [DataRow("byte", "81", "ulong le", "2100000000000000", "<<", 258, typeof(int?))]
    [DataRow("int le", "03000000", "ulong le", "0100000001000000", "<<", 6, typeof(int?))]
    [DataRow("long le", "0000000000000080", "uint le", "41000000", ">>", -4611686018427387904L, typeof(long?))]
    [DataRow("long le", "F0FFFFFFFFFFFFFF", "byte", "41", ">>", -8L, typeof(long?))]
    [DataRow("short le", "F0FF", "int le", "21000000", ">>", -8, typeof(int?))]
    public void Shift_ShouldUseTheLeftWidthAndOnlyTheLowCountBits(
        string left,
        string leftHex,
        string right,
        string rightHex,
        string operation,
        object expected,
        Type type
    )
    {
        var sql = $$"""
            binary Rec { K: byte, L: {{left}}, R: {{right}} when K = 1, V: = L {{operation}} R };
            select r.V as V from values { (H: '01{{leftHex}}{{rightHex}}'), (H: '00{{leftHex}}'), (H: '00{{leftHex}}') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", type);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [null], [null]);
    }

    [TestMethod]
    [DataRow("+", 4ul)]
    [DataRow("/", 0ul)]
    [DataRow("%", 5ul)]
    [DataRow("|", ulong.MaxValue)]
    [DataRow("^", ulong.MaxValue - 5)]
    public void SchemaOperator_ShouldConvertUInt32AndInt64IntoTheCommonUnsignedWidth(string op, ulong expected)
    {
        var sql = $$"""
            binary Rec { K: byte, A: uint le, B: long le when K = 1, V: = A {{op}} B };
            select r.V as V from values { (H: '0105000000FFFFFFFFFFFFFFFF'), (H: '0005000000'), (H: '0005000000') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        AssertResult(sql, typeof(ulong?), expected);
    }

    [TestMethod]
    [DataRow("/", 0ul)]
    [DataRow("%", 5ul)]
    public void QueryDivisionAndRemainder_ShouldConvertBothDecodedOperandsBeforeEvaluation(string op, ulong expected)
    {
        var sql = $$"""
            binary Rec { K: byte, A: uint le, B: long le when K = 1 };
            select r.A {{op}} r.B as V from values { (H: '0105000000FFFFFFFFFFFFFFFF'), (H: '0005000000'), (H: '0005000000') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        AssertResult(sql, typeof(ulong?), expected);
    }

    [TestMethod]
    [DataRow("&", "1")]
    [DataRow("|", "7")]
    [DataRow("^", "6")]
    public void NestedBitwiseExpression_ShouldPassNullFromADecodedFieldToItsCast(string op, string expected)
    {
        var sql = $$"""
            binary Rec { K: byte, A: int le, B: int le when K = 1 };
            select (r.A {{op}} r.B)::String as V
            from values { (H: '010500000003000000'), (H: '0005000000'), (H: '0005000000') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        AssertResult(sql, typeof(string), expected);
    }

    [TestMethod]
    [DataRow("/", "-1l", 0ul)]
    [DataRow("%", "-1l", 5ul)]
    [DataRow("|", "-1l", ulong.MaxValue)]
    [DataRow("*", "-1l", ulong.MaxValue - 4)]
    [DataRow("/", "2l", 2ul)]
    public void SignedLiteral_ShouldUseTheSameUnsignedOperandConversionAsADecodedField(
        string op,
        string operand,
        ulong expected
    )
    {
        var sql = $$"""
            binary Rec { K: byte, W: uint le when K = 1, V: = W {{op}} {{operand}} };
            select r.V as V from values { (H: '0105000000'), (H: '00'), (H: '00') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        AssertResult(sql, typeof(ulong?), expected);
    }

    private void AssertResult(string sql, Type type, object expected)
    {
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", type);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [null], [null]);
    }
}
