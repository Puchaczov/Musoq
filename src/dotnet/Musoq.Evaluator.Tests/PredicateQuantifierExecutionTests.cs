using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PredicateQuantifierExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void AnyLike_WhenFirstOrLaterFieldMatches_ShouldReturnMatchingRows()
    {
        const string query = "select a.Id from #A.Entities() a where any(a.Name, a.City) like '%target%' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 1, 2, 4);
    }

    [TestMethod]
    public void AllLike_WhenAllFieldsMatch_ShouldReturnOnlyAllMatchRows()
    {
        const string query = "select a.Id from #A.Entities() a where all(a.Name, a.City) like '%target%' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 4);
    }

    [TestMethod]
    public void AnyNotLike_WhenAnyFieldFails_ShouldApplyNegationPerField()
    {
        const string query = "select a.Id from #A.Entities() a where any(a.Name, a.City) not like '%target%' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 1, 2, 3, 5);
    }

    [TestMethod]
    public void AllNotLike_WhenNoFieldsMatch_ShouldReturnNoMatchAndNullOnlyRows()
    {
        const string query = "select a.Id from #A.Entities() a where all(a.Name, a.City) not like '%target%' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 3, 5);
    }

    [TestMethod]
    public void AnyRLike_WhenAnyFieldMatchesRegex_ShouldReturnMatchingRows()
    {
        const string query = "select a.Id from #A.Entities() a where any(a.Name, a.City) rlike '^target' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 1, 2, 4);
    }

    [TestMethod]
    public void AllNotRLike_WhenNoFieldsMatchRegex_ShouldReturnNoMatchAndNullOnlyRows()
    {
        const string query = "select a.Id from #A.Entities() a where all(a.Name, a.City) not rlike '^target' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 3, 5);
    }

    [TestMethod]
    public void AllLike_WithLiteralArgument_ShouldUseLiteralAsAFieldPredicate()
    {
        const string query = "select a.Id from #A.Entities() a where all('literal target', a.Name) like '%target%' order by a.Id";

        AssertResultIds(query, CreateTruthTableSource(), 1, 4);
    }

    [TestMethod]
    public void AnyLike_WithMethodExpressions_ShouldExecuteExpandedPredicates()
    {
        const string query = "select a.Id from #A.Entities() a where any(ToUpper(a.Name), ToUpper(a.City)) like '%TARGET%' order by a.Id";

        AssertResultIds(query, CreateNonNullTruthTableSource(), 1, 2, 4);
    }

    [TestMethod]
    public void PredicateQuantifier_WhenNestedInsideLargerWhereExpression_ShouldComposeWithBooleanOperators()
    {
        const string query = "select a.Id from #A.Entities() a where (a.Country = 'search' and any(a.Name, a.City) like '%target%') or a.Id = 99 order by a.Id";

        AssertResultIds(query, CreateComposedWhereSource(), 1, 2, 4, 99);
    }

    private void AssertResultIds(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        params int[] expectedIds)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        var actualIds = Enumerable.Range(0, table.Count)
            .Select(rowIndex => Convert.ToInt32(table[rowIndex][0]))
            .ToArray();

        CollectionAssert.AreEqual(expectedIds, actualIds);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateTruthTableSource()
    {
        return CreateSingleSource(
            new BasicEntity { Id = 1, Name = "target first", City = "plain", Country = "search" },
            new BasicEntity { Id = 2, Name = "plain", City = "target later", Country = "search" },
            new BasicEntity { Id = 3, Name = "plain", City = "other", Country = "search" },
            new BasicEntity { Id = 4, Name = "target all", City = "target city", Country = "search" },
            new BasicEntity { Id = 5, Name = null, City = null, Country = "search" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateNonNullTruthTableSource()
    {
        return CreateSingleSource(
            new BasicEntity { Id = 1, Name = "target first", City = "plain", Country = "search" },
            new BasicEntity { Id = 2, Name = "plain", City = "target later", Country = "search" },
            new BasicEntity { Id = 3, Name = "plain", City = "other", Country = "search" },
            new BasicEntity { Id = 4, Name = "target all", City = "target city", Country = "search" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateComposedWhereSource()
    {
        return CreateSingleSource(
            new BasicEntity { Id = 1, Name = "target first", City = "plain", Country = "search" },
            new BasicEntity { Id = 2, Name = "plain", City = "target later", Country = "search" },
            new BasicEntity { Id = 3, Name = "plain", City = "other", Country = "search" },
            new BasicEntity { Id = 4, Name = "target all", City = "target city", Country = "search" },
            new BasicEntity { Id = 5, Name = null, City = null, Country = "search" },
            new BasicEntity { Id = 99, Name = "fallback", City = "other", Country = "other" });
    }
}
