using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class TextBoundaryResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void SupplementaryCharacter_ShouldOccupyTwoUtf16Units()
    {
        const string sql = "select Length(Txt) as Units, Substring(Txt, 1, 2) as Middle from values {(Txt: 'A😀B')} s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Units", typeof(int?)), ("Middle", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4, "😀"]);
    }

    [TestMethod]
    public void StringLength_ShouldReturnNullableIntForPresentEmptyAndNullText()
    {
        const string sql =
            "select Length(Txt) as Units from values {(Id:1,Txt:'A😀B'),(Id:2,Txt:''),(Id:3,Txt:null)} s order by Id";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Units", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4], [0], [null]);
    }

    [TestMethod]
    public void CollectionLength_ShouldCountBytesAndPreserveEmptyAndNull()
    {
        const string sql =
            "select Length(GetBytes(Txt)) as Elements from values {(Id:1,Txt:'Aé'),(Id:2,Txt:''),(Id:3,Txt:null)} s order by Id";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Elements", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3], [0], [null]);
    }
}
