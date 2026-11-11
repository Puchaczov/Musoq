using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using static Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class LibraryExecutionAsOfQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void CompositeAsOfKeys_WhenNullableAndOrdinal_ShouldPreserveExpectedMatches()
    {
        using var compiled = CreateAndRunVirtualMachine<AsOfInput>(CompositeQuery, CreateRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftPosition", typeof(int)),
            ("RightPosition", typeof(int?))
        );
        int?[] expected = [1, 2, null, 4, 5, 6, null];
        Assert.HasCount(expected.Length, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<int?>(1));
        }
    }
}
