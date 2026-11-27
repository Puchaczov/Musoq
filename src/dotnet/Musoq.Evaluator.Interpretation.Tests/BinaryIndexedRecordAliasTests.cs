using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryIndexedRecordAliasTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("A")]
    [DataRow("switch { pattern 'a' => A, _ => B }")]
    public void ComputedFieldReadingEmbeddedTextArray_ShouldKeepTheElementMemberType(string element)
    {
        var sql = $$"""
            text A { X: chars[1] }; text B { X: chars[1] };
            text T { Items: repeat {{element}} until end };
            binary Rec { M: string[1] ascii as T, V: = M.Items[0].X };
            select r.V as V from values { (H:'61'), (H:'62') } s cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", typeof(string));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], ["b"]);
    }

    [TestMethod]
    [DataRow("case when K = 1 then A else B end", false)]
    [DataRow("case when K = 1 then A else B end", true)]
    [DataRow("case when K = 0 then B else A end", true)]
    public void SelectedRecordArray_ShouldUseTheCountGuaranteedByBothBranches(string expression, bool cte)
    {
        var source =
            " from values { (H:'012a'),(H:'002a'),(H:'002a') } t cross apply Interpret<Rec>(FromHexToBytes(t.H)) r";
        var sql =
            $"binary Cell {{ V: byte }}; binary Rec {{ K: byte, A: Cell[1], B: Cell[0], C: = {expression} }}; "
            + (
                cte
                    ? $"with P (Z) as (select r.C as C{source}) select p.Z[0].V as V from P p"
                    : $"select r.C[0].V as V{source}"
            );

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", typeof(byte?));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)42], [null], [null]);
    }

    [TestMethod]
    [DataRow("A", false)]
    [DataRow("case when K = 1 then A else A end", false)]
    [DataRow("case when K = 1 then A else null end", true)]
    public void ComputedMemberAfterARecordArrayChoice_ShouldRetainItsDeclaredByteType(string expression, bool optional)
    {
        var sql = $$"""
            binary Cell { V: byte };
            binary Rec { K: byte, A: Cell[1], C: = {{expression}}, E: = C[0].V };
            select r.E as E from values { (H:'012a'), (H:'002a') } s
            cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "E", optional ? typeof(byte?) : typeof(byte));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)42], [optional ? (byte?)null : (byte)42]);
    }

    [TestMethod]
    [DataRow("Rows[1]", "r.A.N", (byte)66)]
    [DataRow("Rows[1]", "r.V", (byte)66)]
    [DataRow("CASE WHEN I = 1 THEN Rows[1] ELSE Rows[0] END", "r.A.N", (byte)65)]
    [DataRow("CASE WHEN I = 1 THEN Rows[1] ELSE NULL END", "r.A.N", null)]
    public void ComputedArrayElement_ShouldRetainRecordMembers(string expression, string projection, byte? expected)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Rec { I: byte, Rows: Item[2], A: = {{expression}}, V: = A.N };
            let b: byte[] = array { 0ub, 65ub, 66ub };
            select {{projection}} from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }

    [TestMethod]
    [DataRow("binary Holder<T> { Rows: T[2], A: = Rows[1] }; binary Rec { X: Holder<Item> }", "r.X.A.N")]
    [DataRow("binary Holder<T> { Rows: T[2] }; binary Rec { X: Holder<Item>, V: = X.Rows[1].N }", "r.V")]
    [DataRow("binary Holder<T> { Rows: T[2] }; binary Rec { X: Holder<Item> }", "r.X.Rows[1].N")]
    public void ClosedGenericRecordArray_ShouldExposeItsElementMembers(string schemas, string projection)
    {
        var sql = $$"""
            binary Item { N: byte };
            {{schemas}};
            let b: byte[] = array { 65ub, 66ub };
            select {{projection}} from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }

    [TestMethod]
    [DataRow("Child<Item>")]
    [DataRow("Child<T>")]
    public void GenericChild_ShouldReceiveItsClosedEnclosingRecordArray(string child)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Child<U> { UField: U, V: = parent.Rows[0].N };
            binary Wrap<T> { Rows: T[1], C: {{child}} };
            binary Rec { X: Wrap<Item> };
            let b: byte[] = array { 66ub, 36ub };
            select r.X.C.V, r.X.C.UField.N from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66, (byte)36]);
    }

    [TestMethod]
    public void GenericGrandparent_ShouldForwardTheConcreteArray()
    {
        const string sql = """
            binary Item { N: byte };
            binary Child { V: = parent.parent.Rows[0].N };
            binary Mid { C: Child };
            binary Wrap<T> { Rows: T[1], M: Mid };
            binary Rec { X: Wrap<Item> };
            let b: byte[] = array { 66ub };
            select r.X.M.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }

    [TestMethod]
    public void ChildIndexingAnEnclosingGenericArray_ShouldUseTheConcreteElement()
    {
        const string sql = """
            binary Item { N: byte };
            binary Inner { I: byte, V: = parent.Rows[I].N };
            binary Holder<T> { Rows: T[2], C: Inner };
            binary Rec { H: Holder<Item> };
            let b: byte[] = array { 65ub, 66ub, 1ub };
            select r.H.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }
}
