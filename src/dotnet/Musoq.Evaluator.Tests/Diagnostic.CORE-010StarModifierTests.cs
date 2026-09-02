using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DiagnosticCore010StarModifierTests : BasicEntityTestBase
{
    [TestMethod]
    public void FullModifierChain_ShouldApplyInOrderAndPreserveReplacementPosition()
    {
        const string query =
            "select * like '%o%' exclude (country) replace (Population * 3 as population) " +
            "rename (population as Population3x) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity
                {
                    Country = "UK",
                    Population = 100m,
                    Money = 25m,
                    Month = "january"
                }]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population3x", typeof(decimal)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [300m, 25m, "january"]);
    }

    [TestMethod]
    public void QualifiedStar_ShouldMatchReplaceAndRenameTargetsCaseInsensitively()
    {
        const string query =
            "select a.* replace (Population * 2 as population) rename (population as POP) " +
            "from #A.Entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Population = 100m }] }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "a.Name", "a.City", "a.Country", "POP", "a.Money", "a.Month", "a.Time", "a.Id", "a.NullableValue" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        Assert.AreEqual(200m, table[0].Values[3]);
    }

    [TestMethod]
    public void NotLike_ShouldUseCaseInsensitiveSqlPatternMatching()
    {
        const string query = "select a.* not like '%ID' from #A.Entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Id = 42 }] }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        Assert.AreEqual(8, table.Columns.Count());
        Assert.IsFalse(table.Columns.Any(static column => column.ColumnName.EndsWith(".Id")));
    }

    [TestMethod]
    public void RenameCollision_ShouldExposeStructuredBindDiagnosticWithGuidance()
    {
        const string query = "select * rename (Name as City) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertSingleError(
            exception,
            DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
            DiagnosticPhase.Bind,
            "City");
        AssertHasGuidance(exception);
        Assert.AreEqual(DiagnosticSourceKind.Query, exception.PrimaryEnvelope.SourceKind);
        Assert.IsTrue(exception.PrimaryEnvelope.Offset.HasValue);
        Assert.IsTrue(exception.PrimaryEnvelope.Length.HasValue);
        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.PrimaryEnvelope.Snippet));
    }
}
