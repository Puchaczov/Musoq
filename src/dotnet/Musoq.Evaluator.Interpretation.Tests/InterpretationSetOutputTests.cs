using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationSetOutputTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void NullOnlyCteColumn_ShouldAcquireTheDecodedRecordType(bool binary, bool nullFirst)
    {
        var declarations = binary
            ? "binary Child { V: byte }; binary Rec { D: Child };"
            : "text Child { V: rest }; text Rec { D: Child };";
        var parsed = binary
            ? "select r.D as D from values { (S:'2a') } s cross apply Interpret<Rec>(FromHexToBytes(s.S)) r"
            : "select r.D as D from values { (S:'x') } s cross apply Parse<Rec>(s.S) r";
        const string absent = "select t.D as D from T t";
        var branches = nullFirst ? $"{absent} union all {parsed}" : $"{parsed} union all {absent}";
        var sql =
            declarations
            + $" with T as (select null as D from values {{ (K:0), (K:0) }} z), "
            + $"P as ({branches}) select p.D.V as V from P p";

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("V", binary ? typeof(byte?) : typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null], [null], [binary ? (object)(byte)42 : "x"]);
    }

    [TestMethod]
    [DataRow("text Cell { V: rest }; text Rec { D: Cell };", "Parse<Rec>(a.S)", "c.D.V", false)]
    [DataRow(
        "text A { V: rest }; text B { V: rest }; text Rec { D: switch { pattern 'a' => A, _ => B } };",
        "Parse<Rec>(a.S)",
        "c.D.V",
        false
    )]
    [DataRow(
        "binary Cell { V: byte }; binary Rec { D: Cell[1] };",
        "Interpret<Rec>(FromHexToBytes(a.S))",
        "c.D[0].V",
        true
    )]
    public void ReusingADecodedCteInUnionAll_ShouldPreserveItsPayload(
        string declarations,
        string call,
        string path,
        bool binary
    )
    {
        var input = binary ? "2a" : "a";
        var sql = $$"""
            {{declarations}}
            with P as (select r.D as D from values { (S:'{{input}}') } a cross apply {{call}} r),
            C as (select p.D as D from P p union all select p.D as D from P p)
            select {{path}} as V from C c
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        object value = binary ? (byte)42 : "a";
        TableMaterializationTestHelper.AssertRowsUnordered(table, [value], [value]);
    }

    [TestMethod]
    [DataRow("same", false)]
    [DataRow("null-first", false)]
    [DataRow("null-last", false)]
    [DataRow("null-first", true)]
    [DataRow("single", false)]
    public void SetOfTextAlternatives_ShouldUseTheDeclaredArmMembers(string mode, bool onlyOneArm)
    {
        var second = onlyOneArm ? "W" : "V";
        const string actual = "select r.D as D from values { (S:'a'), (S:'b') } t cross apply Parse<Rec>(t.S) r";
        const string absent = "select null as D from values { (N:1) } n";
        var branches = mode switch
        {
            "same" => $"{actual} union all {actual}",
            "null-first" => $"{absent} union all {actual}",
            "null-last" => $"{actual} union all {absent}",
            _ => actual,
        };
        var sql = $$"""
            text A { V: rest }; text B { {{second}}: rest };
            text Rec { D: switch { pattern 'a' => A, _ => B } };
            with U (Decoded) as ({{branches}})
            select u.Decoded.V as V from U u
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("V", onlyOneArm ? typeof(object) : typeof(string)));
        string? other = onlyOneArm ? null : "b";
        object?[][] expected = mode switch
        {
            "same" =>
            [
                ["a"],
                [other],
                ["a"],
                [other],
            ],
            "single" =>
            [
                ["a"],
                [other],
            ],
            _ =>
            [
                ["a"],
                [other],
                [null],
            ],
        };
        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }

    [TestMethod]
    [DataRow("D: Cell", "r.D.S.Length", false)]
    [DataRow("D: optional Cell", "r.D.S.Length", true)]
    [DataRow("D: Cell", "r.D.S.Length", false, true)]
    public void RequiredNestedTextLength_ShouldRetainItsDeclaredIntegerType(
        string field,
        string path,
        bool optional,
        bool cte = false
    )
    {
        const string source = " from values { (S:'ab'), (S:'') } t cross apply Parse<Rec>(t.S) r";
        var sql =
            $"text Cell {{ S: rest }}; text Rec {{ {field} }}; "
            + (
                cte
                    ? $"with U as (select r.D as D{source}) select u.D.S.Length as L from U u"
                    : $"select {path} as L{source}"
            );

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("L", optional ? typeof(int?) : typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2], [0]);
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, true)]
    public void SetWithNullAndDecodedRecord_ShouldPreserveMemberValues(bool binary, bool array, bool nullFirst)
    {
        var definition = binary
            ? $"binary Cell {{ V: byte }}; binary Rec {{ D: Cell{(array ? "[1]" : "")} }};"
            : $"text Cell {{ V: chars[1] }}; text Rec {{ D: {(array ? "repeat Cell until end" : "Cell")} }};";
        var actual = binary
            ? "select r.D as D from values { (S:'41') } t cross apply Interpret<Rec>(FromHexToBytes(t.S)) r"
            : "select r.D as D from values { (S:'A') } t cross apply Parse<Rec>(t.S) r";
        const string absent = "select null as D from values { (N:0), (N:0) } z";
        var branches = nullFirst ? $"{absent} union all {actual}" : $"{actual} union all {absent}";
        var sql = $"{definition} with X as ({branches}) select x.D{(array ? "[0]" : "")}.V as V from X x";

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("V", binary ? typeof(byte?) : typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [binary ? (object)(byte)65 : "A"], [null], [null]);
    }

    [TestMethod]
    [DataRow("binary Cell { V: byte }; binary Rec { D: Cell };", "Interpret<Rec>(FromHexToBytes('41'))", "x.D.V", true)]
    [DataRow("text Cell { V: chars[1] }; text Rec { D: repeat Cell until end };", "Parse<Rec>('A')", "x.D[0].V", false)]
    [DataRow("text Cell { V: chars[1] }; text Rec { D: Cell };", "Parse<Rec>('A')", "x.D.V", false)]
    public void UnionOfDecodedRecords_ShouldReadBothGeneratedArms(
        string definitions,
        string call,
        string path,
        bool binary
    )
    {
        var item = $"select r.D as D from values {{ (K:0) }} t cross apply {call} r";
        var sql = $"{definitions} with X as ({item} union all {item}) select {path} as V from X x";

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        object value = binary ? (byte)65 : "A";
        TableMaterializationTestHelper.AssertRowsUnordered(table, [value], [value]);
    }

    private const string Present =
        "select r.Tail as V from values { (S:'ok'), (S:'') } l cross apply Parse<Rec>(l.S) r";
    private const string Optional =
        "select r.V as V from values { (S:'[open'), (S:'[open') } q cross apply Parse<Rec>(q.S) r";
    private const string Nulls = "select null as V from values { (K:0), (K:0) } z";

    [TestMethod]
    [DataRow(Present, Optional)]
    [DataRow(Optional, Present)]
    [DataRow(Present, Nulls)]
    [DataRow(Nulls, Present)]
    public void SetWithMissingDecodedStrings_ShouldLiftLengthInEitherArmOrder(string first, string second)
    {
        var sql = $$"""
            text Rec { V: optional between '[' ']', Tail: rest };
            with X (Z) as ({{first}} union all {{second}})
            select x.Z.Length as L from X x
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("L", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2], [0], [null], [null]);
    }
}
