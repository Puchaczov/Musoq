using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationCaseProjectionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void WindowCaseSelectingDecodedPayload_ShouldKeepTheChosenMember(bool binary, bool useFallback)
    {
        var declarations = binary
            ? "binary A { V: byte }; binary R { D: A[1] };"
            : "text A { V: rest }; text R { D: A };";
        var rows = binary ? "(K:2,S:'07'),(K:1,S:'2a')" : "(K:2,S:'b'),(K:1,S:'a')";
        var call = binary ? "Interpret<R>(FromHexToBytes(s.S))" : "Parse<R>(s.S)";
        var path = binary ? "p.X[0].V" : "p.X.V";
        const string choice = "case when RowNumber() over (order by s.K)=1 then r.D end";
        var value = useFallback ? $"({choice}) ?? r.D" : choice;
        var sql = $$"""
            {{declarations}}
            with P as (select {{value}} as X from values { {{rows}} } s cross apply {{call}} r)
            select {{path}} as V from P p
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        var type = binary ? (useFallback ? typeof(byte) : typeof(byte?)) : typeof(string);
        TableMaterializationTestHelper.AssertColumns(table, ("V", type));
        object first = binary ? (byte)42 : "a";
        object? second =
            !useFallback ? null
            : binary ? (byte)7
            : "b";
        TableMaterializationTestHelper.AssertRowsUnordered(table, [first], [second]);
    }

    [TestMethod]
    [DataRow("text A { V: rest }; text R { D: A };", false, true)]
    [DataRow("text R { D: pattern '(?<V>.)' capture (V) };", false, true)]
    [DataRow("text A { V: rest }; text B { V: rest }; text R { D: switch { pattern 'a' => A, _ => B } };", false, true)]
    [DataRow(
        "text A { V: rest }; text B { V: rest }; text R { D: switch { pattern 'a' => A, _ => B } };",
        false,
        false
    )]
    [DataRow("binary A { V: byte }; binary R { D: A[1] };", true, true)]
    [DataRow("binary A { V: byte }; binary R { D: A[1] };", true, false)]
    public void ProjectingCaseOverDecodedValues_ShouldPreserveMemberTypeAndNullability(
        string declarations,
        bool binary,
        bool nullElse
    )
    {
        var source = binary ? "(K:1,S:'2a'),(K:0,S:'00')" : "(K:1,S:'a'),(K:0,S:'b')";
        var call = binary ? "Interpret<R>(FromHexToBytes(s.S))" : "Parse<R>(s.S)";
        var path = binary ? "p.X[0].V" : "p.X.V";
        var sql = $$"""
            {{declarations}}
            with P as (select case when s.K=1 then r.D else {{(nullElse ? "null" : "r.D")}} end as X
                from values { {{source}} } s cross apply {{call}} r)
            select {{path}} as V from P p
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        var type = binary ? (nullElse ? typeof(byte?) : typeof(byte)) : typeof(string);
        TableMaterializationTestHelper.AssertColumns(table, ("V", type));
        object first = binary ? (byte)42 : "a";
        object? second =
            nullElse ? null
            : binary ? (byte)0
            : "b";
        TableMaterializationTestHelper.AssertRowsUnordered(table, [first], [second]);
    }
}
