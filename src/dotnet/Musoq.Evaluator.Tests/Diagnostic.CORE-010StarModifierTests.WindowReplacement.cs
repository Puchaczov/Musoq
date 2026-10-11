using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class DiagnosticCore010StarModifierTests
{
    private const string InlineWindowReplacementQuery =
        "select v.* exclude (Discarded) replace (RowNumber() over (order by v.SortKey) as Ordinal) from values { (Ordinal: 0l, SortKey: 2i, Discarded: 'x'), (Ordinal: 0l, SortKey: 1i, Discarded: 'y') } v";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0115")]
    public void Current_P2_0115_StarReplaceAcceptsWindowExpression()
    {
        AssertInlineWindowReplacement("Ordinal", "SortKey");
    }

    [TestMethod]
    public void InlineWindowReplacement_ShouldPreserveOrdinalValuesAndEstablishedNames()
    {
        AssertInlineWindowReplacement("Ordinal", "SortKey");
    }

    private void AssertInlineWindowReplacement(string ordinalName, string sortKeyName)
    {
        using var compiled = CreateAndRunVirtualMachine(InlineWindowReplacementQuery,
            new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            (ordinalName, typeof(long)), (sortKeyName, typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 1L, 1 }, new object?[] { 2L, 2 });
    }
}
