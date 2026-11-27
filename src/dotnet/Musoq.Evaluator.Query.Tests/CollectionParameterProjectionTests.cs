using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class CollectionParameterProjectionTests : BasicEntityTestBase
{
    [TestMethod]
    public void NullCollectionMemberMakesBothNonmatchingPredicatesUnknown()
    {
        const string sql =
            "param(ids: int?[] = array { null }) select s.K in $ids as P,s.K not in $ids as N from values { (K:1) } s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("P", typeof(bool?)), ("N", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { null, null });
    }

    [TestMethod]
    public void NullableProbeCanSearchACollectionOfNonnullableElements()
    {
        const string sql =
            "param(ids: int[] = array { 1 }) select s.K in $ids as P,s.K not in $ids as N from values { (K:1),(K:null) } s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [true, false], [null, null]);
    }
}
