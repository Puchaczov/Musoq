using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryStrictCastCarrierTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Char", "Z", "=", "Z")]
    [DataRow("DateTime", "2024-02-29", ">", "2024-02-28")]
    [DataRow("DateTimeOffset", "2024-02-29T04:05:06+02:00", "=", "2024-02-29T02:05:06+00:00")]
    [DataRow("TimeSpan", "02:30:00", "<", "03:00:00")]
    [DataRow("Guid", "2ffcf6fa-3369-4300-946a-bb131a037985", "=", "2FFCF6FA-3369-4300-946A-BB131A037985")]
    [DataRow("Int32", "2", "=", "2")]
    public void PreviousStrictCast_ShouldRetainItsNullableScalarType(
        string target,
        string input,
        string operation,
        string other
    )
    {
        var hex = Convert.ToHexString(Encoding.ASCII.GetBytes(input));
        var sql = $$"""
            binary Rec {
                K: byte, S: string[{{input.Length}}] ascii when K = 1, D: = S::{{target}},
                Same: = (case when K = 1 then D else null end) {{operation}} '{{other}}'::{{target}},
                V: byte when D {{operation}} '{{other}}'::{{target}}
            };
            select r.Same as InSchema, r.D {{operation}} '{{other}}'::{{target}} as InQuery, r.V as Guarded
            from values { (H: '01{{hex}}2A'), (H: '00'), (H: '00') } s
            cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "InSchema", typeof(bool?));
        AssertColumn(table, 1, "InQuery", typeof(bool?));
        AssertColumn(table, 2, "Guarded", typeof(byte?));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [true, true, (byte)42],
            [null, null, null],
            [null, null, null]
        );
    }

    [TestMethod]
    [DataRow("2024-03-01", "DateTime", "-", "2024-02-28", "DateTime", "2.00:00:00", "TimeSpan")]
    [DataRow(
        "2024-02-29T04:00:00.0000001+02:00",
        "DateTimeOffset",
        "-",
        "2024-02-29T02:00:00+00:00",
        "DateTimeOffset",
        "00:00:00.0000001",
        "TimeSpan"
    )]
    [DataRow("2024-02-29", "DateTime", "+", "1.00:00:00", "TimeSpan", "2024-03-01", "DateTime")]
    [DataRow("2024-03-01", "DateTime", "-", "00:00:01", "TimeSpan", "2024-02-29T23:59:59", "DateTime")]
    [DataRow(
        "2024-02-29T23:00:00-04:00",
        "DateTimeOffset",
        "+",
        "02:00:00",
        "TimeSpan",
        "2024-03-01T05:00:00+00:00",
        "DateTimeOffset"
    )]
    [DataRow("01:30:00", "TimeSpan", "-", "02:30:00", "TimeSpan", "-01:00:00", "TimeSpan")]
    [DataRow("02:30:00", "TimeSpan", "+", "01:30:00", "TimeSpan", "04:00:00", "TimeSpan")]
    public void ComputedTemporalArithmetic_ShouldUseCoreResultTypesAndNullPropagation(
        string input,
        string leftType,
        string op,
        string right,
        string rightType,
        string result,
        string resultType
    )
    {
        var hex = Convert.ToHexString(Encoding.ASCII.GetBytes(input));
        var sql = $$"""
            binary Rec { K: byte, S: string[{{input.Length}}] ascii when K = 1, V: = S::{{leftType}} {{op}} '{{right}}'::{{rightType}} };
            select r.V = '{{result}}'::{{resultType}} as Matches
            from values { (H: '01{{hex}}'), (H: '00'), (H: '00') } a
            cross apply Interpret<Rec>(FromHexToBytes(a.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "Matches", typeof(bool?));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [true], [null], [null]);
    }
}
