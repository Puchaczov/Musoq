using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class TextRecordArrayApplyTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void RuntimeIndexIntoRepeatedSwitch_ShouldReadTheSelectedMember(bool cte, bool onlyOneArm)
    {
        var schemas = $$"""
            text A { X: chars[1] }; text B { {{(onlyOneArm ? "Y" : "X")}}: chars[1] };
            text T { Items: repeat switch { pattern 'a' => A, _ => B } until end };
            """;
        const string from =
            " from values { (S:'ab',I:0), (S:'ab',I:1), (S:'',I:0), (S:'',I:0) } s cross apply Parse<T>(s.S) r";
        var sql =
            schemas
            + (
                cte
                    ? $" with P (A,I) as (select r.Items as Items, s.I as I{from}) select p.A[p.I].X as V from P p"
                    : $" select r.Items[s.I].X as V{from}"
            );

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", onlyOneArm ? typeof(object) : typeof(string));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], [onlyOneArm ? null : "b"], [null], [null]);
    }

    [TestMethod]
    public void TwoRepeatsOfTheSameChild_ShouldUseTheirOwnRecordReaders()
    {
        const string sql = """
            text C { V: chars[1] };
            text Rec { A: repeat C until ',', B: repeat C until end };
            select r.A[0].V, r.B[0].V from values { (S: 'x,y') } s cross apply Parse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, ["x", "y"]);
    }

    [TestMethod]
    public void ExpandedRecord_ShouldExposeInheritedFields()
    {
        const string sql = """
            text Prefix { V: chars[1] };
            text Child extends Prefix {};
            text Rec { Items: repeat Child until end };
            select e.V from values { (S: 'X') } s cross apply Parse<Rec>(s.S) r cross apply r.Items e
            """;

        AssertQueryRows(sql, ["X"]);
    }

    [TestMethod]
    public void ExpandedRecord_ShouldExposeCaptureMembers()
    {
        const string sql = """
            text Child { Part: pattern '(?<Key>.)' capture (Key) };
            text Rec { Items: repeat Child until end };
            select e.Part.Key from values { (S: 'X') } s cross apply Parse<Rec>(s.S) r cross apply r.Items e
            """;

        AssertQueryRows(sql, ["X"]);
    }
}
