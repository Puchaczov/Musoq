using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

using static Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed partial class LibraryExecutionCoverageQueryTests : GenericEntityTestBase
{

    [TestMethod]
    [DataRow(17)]
    [DataRow(7919)]
    public void JoinedNullableFramesAndQualify_ShouldMatchIndependentSeededReference(int seed)
    {
        var input = CreateRows(seed);
        using var compiled = CreateAndRunVirtualMachine(JoinedFrameQuery, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var actual = table.ToArray();
        var expected = CalculateJoinedFrames(input);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Partition", typeof(int?)),
            ("Total", typeof(int?)), ("Average", typeof(decimal?)), ("Present", typeof(long)),
            ("Previous", typeof(int?)), ("Number", typeof(long)));
        Assert.HasCount(expected.Length, actual);
        for (var index = 0; index < expected.Length; index++)
        {
            var row = actual[index];
            var reference = expected[index];
            Assert.AreEqual(reference.Position, row.GetValue<int>(0));
            Assert.AreEqual(reference.Partition, row.GetValue<int?>(1));
            Assert.AreEqual(reference.Total, row.GetValue<int?>(2));
            Assert.AreEqual(reference.Average, row.GetValue<decimal?>(3));
            Assert.AreEqual(reference.Present, row.GetValue<long>(4));
            Assert.AreEqual(reference.Previous, row.GetValue<int?>(5));
            Assert.AreEqual(reference.Number, row.GetValue<long>(6));
        }
    }
}
