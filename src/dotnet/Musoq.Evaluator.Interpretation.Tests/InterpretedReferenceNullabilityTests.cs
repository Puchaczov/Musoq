using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretedReferenceNullabilityTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void MissingRecordInANestedArray_ShouldLiftItsByteMember(bool fixedLength, bool throughCte)
    {
        var items = fixedLength ? "A: Cell[1]" : "K: byte, A: Cell[K]";
        var index = fixedLength ? 1 : 0;
        var values = fixedLength ? "(H:'2A'), (H:'00')" : "(H:'012A'), (H:'00'), (H:'00')";
        var source = $" from values {{ {values} }} s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r";
        var sql =
            $"binary Cell {{ V: byte }}; binary Bag {{ {items} }}; binary Rec {{ B: Bag }}; "
            + (
                throughCte
                    ? $"with P (X) as (select r.B as B{source}) select p.X.A[{index}].V as V from P p"
                    : $"select r.B.A[{index}].V as V{source}"
            );

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", typeof(byte?));
        object?[][] expected = fixedLength
            ?
            [
                [null],
                [null],
            ]
            :
            [
                [(byte)42],
                [null],
                [null],
            ];
        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }

    [TestMethod]
    [DataRow("r.Cells", "Cells: Item[1]", "0")]
    [DataRow("r.Cells", "Cells: Item[1]", "-1")]
    [DataRow("r.Group.Cells", "Group: ItemGroup", "0")]
    public void RenamedFixedRecordArray_ShouldKeepAnInBoundsMemberNonNullable(
        string field,
        string definition,
        string index
    )
    {
        var sql = $$"""
            binary Item { V: byte }; binary ItemGroup { Cells: Item[1] };
            binary Rec { {{definition}} };
            with P (Items) as (
                select {{field}} as A from values { (H: '2A') } t
                cross apply Interpret<Rec>(FromHexToBytes(t.H)) r
            ), Q (Renamed) as (select p.Items as Items from P p)
            select q.Renamed[{{index}}].V as V from Q q
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", typeof(byte));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)42]);
    }

    [TestMethod]
    public void FirstFixedRecordAfterFailedOuterInterpretation_ShouldPreserveNullAndDuplicateRows()
    {
        const string sql = """
            binary Item { V: byte }; binary Rec { Cells: Item[1] };
            select r.Cells[0].V as V from values { (H:''), (H:''), (H:'00'), (H:'2A') } t
            outer apply TryInterpret<Rec>(FromHexToBytes(t.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", typeof(byte?));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null], [null], [(byte)0], [(byte)42]);
    }

    [TestMethod]
    [DataRow("S: optional between '[' ']'", "r.S", "cross apply Parse", false)]
    [DataRow("S: optional between '[' ']'", "r.ParsedFields.S", "cross apply PartialParse", true)]
    [DataRow("S: between '[' ']'", "r.S", "outer apply TryParse", false)]
    [DataRow("S: between '[' ']'", "r.S", "outer apply TryParse", true)]
    [DataRow("ChildValue: Child", "r.ChildValue.S", "cross apply Parse", true)]
    public void MissingTextCapture_ShouldKeepNullableLengthAndBothInputRows(
        string fields,
        string path,
        string apply,
        bool cte
    )
    {
        var schema = $"text Child {{ S: optional between '[' ']' }}; text Rec {{ {fields} }}; ";
        var source = $" from values {{ (S: '[open'), (S: '[open'), (S: '[ok]') }} s {apply}<Rec>(s.S) r";
        var sql =
            schema
            + (
                cte
                    ? $"with Parsed (Value) as (select {path} as V{source}) select p.Value.Length as N from Parsed p"
                    : $"select {path}.Length as N{source}"
            );

        AssertLengths(sql, [null], [null], [2]);
    }

    [TestMethod]
    [DataRow("S: string[1] ascii when K = 1", "r.S")]
    [DataRow("S: = case when K = 1 then 'a' else null end", "r.S")]
    [DataRow("H: Header when K = 1", "r.H.S")]
    public void RenamingNullableBinaryStringInACte_ShouldKeepTheLengthsLogicalType(string declaration, string path)
    {
        var sql = $$"""
            binary Header { S: string[1] ascii };
            binary Rec { K: byte, {{declaration}} };
            with P (Val) as (
                select {{path}} as S from values { (H: '00'), (H: '0141') } s
                cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            ) select p.Val.Length as N from P p
            """;

        AssertLengths(sql, [null], [1]);
    }

    [TestMethod]
    [DataRow("Parse", "r.Items")]
    [DataRow("PartialParse", "r.ParsedFields.Items")]
    public void RecoveredRepeat_ShouldExposeNullLengthAndLeaveTheTailAtItsCheckpoint(string api, string path)
    {
        var sql = $$"""
            text Rec { Items: optional repeat literal 'a' until '|', Tail: rest };
            select {{path}}.Length as N, {{(api == "Parse" ? "r.Tail" : "r.ParsedFields.Tail")}} as T
            from values { (S: 'a?'), (S: 'aa|Z'), (S: '|Z') } s cross apply {{api}}<Rec>(s.S) r
            """;

        AssertLengths(sql, [null, "a?"], [2, "Z"], [0, "Z"]);
    }

    [TestMethod]
    [DataRow("chars[1]", "rest", "a", "bbb", false, false)]
    [DataRow("repeat chars[1] until '|'", "repeat chars[1] until end", "a|", "bbb", false, false)]
    [DataRow("optional literal '!'", "rest", "a", "bbb", true, false)]
    [DataRow("optional literal '!'", "rest", "a", "bbb", true, true)]
    public void CommonSwitchMember_ShouldRetainItsTypedLength(
        string a,
        string b,
        string first,
        string second,
        bool optional,
        bool cte
    )
    {
        var declarations = $$"""
            text A { S: {{a}} };
            text B { S: {{b}} };
            text Rec { C: switch { pattern 'a' => A, _ => B } };
            """;
        var source = $" from values {{ (S: '{first}'), (S: '{second}') }} s cross apply Parse<Rec>(s.S) r";
        var sql =
            declarations
            + (
                cte
                    ? $" with P as (select r.C.S as V{source}) select p.V.Length as N from P p"
                    : $" select r.C.S.Length as N{source}"
            );

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "N", optional ? typeof(int?) : typeof(int));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [optional ? (int?)null : 1], [3]);
    }

    [TestMethod]
    [DataRow(false, false, "0")]
    [DataRow(false, true, "0")]
    [DataRow(false, false, "1 - 1")]
    [DataRow(false, true, "-1")]
    [DataRow(true, false, "0")]
    [DataRow(true, true, "1 - 1")]
    [DataRow(true, false, "-1")]
    public void MissingRepeatedElement_ShouldKeepItsMemberLengthNull(bool records, bool cte, string index)
    {
        var item = records ? "Item" : "chars[1]";
        var schemas =
            $"text Item {{ Parts: repeat chars[1] until ',' }}; text Rec {{ Chunks: repeat {item} until ';' }}; ";
        var values = records ? "(S: 'ab,;'), (S: ';'), (S: ';'), (S: ',;')" : "(S: 'a;'), (S: ';'), (S: ';')";
        var source = $" from values {{ {values} }} s cross apply Parse<Rec>(s.S) r";
        var member = records ? ".Parts.Length" : ".Length";
        var sql =
            schemas
            + (
                cte
                    ? $"with P (V) as (select r.Chunks as C{source}) select p.V[{index}]{member} as N from P p"
                    : $"select r.Chunks[{index}]{member} as N{source}"
            );

        AssertLengths(
            sql,
            records
                ?
                [
                    [2],
                    [null],
                    [null],
                    [0],
                ]
                :
                [
                    [1],
                    [null],
                    [null],
                ]
        );
    }

    [TestMethod]
    [DataRow("named", false, "0")]
    [DataRow("named", true, "1 - 1")]
    [DataRow("switch", false, "0")]
    [DataRow("switch", true, "1 - 1")]
    [DataRow("partial", false, "0")]
    [DataRow("partial", true, "1 - 1")]
    [DataRow("capture", false, "0")]
    public void NestedRepeatedTextField_ShouldRetainItsMissingElementType(string shape, bool cte, string index)
    {
        const string repeated = "Items: repeat chars[1] until ';'";
        var (schema, path, api, member) = shape switch
        {
            "partial" => (
                $"text Rec {{ {repeated}, Bad: literal '!' }}",
                "r.ParsedFields.Items",
                "PartialParse",
                "Length"
            ),
            "switch" => (
                $"text A {{ {repeated} }}; text B {{ {repeated} }}; text Rec {{ C: switch {{ pattern 'a' => A, _ => B }} }}",
                "r.C.Items",
                "Parse",
                "Length"
            ),
            "capture" => (
                "text Child { Items: repeat pattern '(?<L>[a-z])' capture (L) until ';' }; text Rec { C: Child }",
                "r.C.Items",
                "Parse",
                "L.Length"
            ),
            _ => ($"text Child {{ {repeated} }}; text Rec {{ C: Child }}", "r.C.Items", "Parse", "Length"),
        };
        var source = $" from values {{ (S: 'a;'), (S: ';'), (S: ';') }} s cross apply {api}<Rec>(s.S) r";
        var sql =
            schema
            + (
                cte
                    ? $"; with P (V) as (select {path} as V{source}) select p.V[{index}].{member} as N from P p"
                    : $"; select {path}[{index}].{member} as N{source}"
            );

        AssertLengths(sql, [1], [null], [null]);
    }

    private void AssertLengths(string sql, params object?[][] rows)
    {
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "N", typeof(int?));
        TableMaterializationTestHelper.AssertRowsUnordered(table, rows);
    }
}
