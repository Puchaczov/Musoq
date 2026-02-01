using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Tests.Common;
using SchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for QueryHints when using coupled schemas.
///     These tests verify that SKIP/TAKE/DISTINCT hints are correctly passed to data sources
///     when using the 'couple' statement to define table structures.
/// </summary>
[TestClass]
public class CoupledSchemaQueryHintsTests
{
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static CoupledSchemaQueryHintsTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    public TestContext TestContext { get; set; }

    private ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    [TestMethod]
    public void CoupledSchema_WithoutOrderByOrGroupBy_ShouldPassSkipHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, p.Priority from People() p skip 10";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.AreEqual(10L, capturedContext.QueryHints.SkipValue, "SkipValue should be 10");
        Assert.IsNull(capturedContext.QueryHints.TakeValue, "TakeValue should be null");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "Should have optimization hints");
    }

    [TestMethod]
    public void CoupledSchema_WithoutOrderByOrGroupBy_ShouldPassTakeHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, p.Priority from People() p take 5";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsNull(capturedContext.QueryHints.SkipValue, "SkipValue should be null");
        Assert.AreEqual(5L, capturedContext.QueryHints.TakeValue, "TakeValue should be 5");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "Should have optimization hints");
    }

    [TestMethod]
    public void CoupledSchema_WithoutOrderByOrGroupBy_ShouldPassSkipAndTakeHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, p.Priority from People() p skip 10 take 5";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.AreEqual(10L, capturedContext.QueryHints.SkipValue, "SkipValue should be 10");
        Assert.AreEqual(5L, capturedContext.QueryHints.TakeValue, "TakeValue should be 5");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "Should have optimization hints");
    }

    [TestMethod]
    public void CoupledSchema_WithDistinct_ShouldNotPassHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i % 10, Name = $"Item{i % 10}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select distinct p.Name from People() p skip 2 take 3";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - DISTINCT creates implicit GROUP BY
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints,
            "DISTINCT creates implicit GROUP BY and should prevent hints from being passed");
    }

    [TestMethod]
    public void CoupledSchema_WithOrderBy_ShouldNotPassHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, p.Priority from People() p order by p.Name skip 10 take 5";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - ORDER BY means no hints
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints,
            "ORDER BY should prevent hints from being passed");
    }

    [TestMethod]
    public void CoupledSchema_WithGroupBy_ShouldNotPassHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Category{i % 5}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, Count(p.Priority) from People() p group by p.Name take 3";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - GROUP BY means no hints
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints,
            "GROUP BY should prevent hints from being passed");
    }

    [TestMethod]
    public void CoupledSchema_MultipleTablesWithJoin_ShouldNotPassHints()
    {
        // Arrange
        RuntimeContext capturedItemsContext = null;
        RuntimeContext capturedCategoriesContext = null;

        var multiApi = new ApiDataSourceQueryHintsIntegrationTests.FakeMultiApiDataSource(
            itemsProvider: ctx =>
            {
                capturedItemsContext = ctx;
                return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity
                {
                    Id = i,
                    Name = $"Item{i}",
                    Status = $"cat{i % 3}"
                });
            },
            categoriesProvider: ctx =>
            {
                capturedCategoriesContext = ctx;
                return Enumerable.Range(0, 3).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeCategoryEntity
                {
                    CategoryId = $"cat{i}",
                    CategoryName = $"Category {i}"
                });
            });

        var query = @"
            table Item { Id 'System.Int32', Name 'System.String', Status 'System.String' };
            table Category { CategoryId 'System.String', CategoryName 'System.String' };
            couple #multiapi.items with table Item as Items;
            couple #multiapi.categories with table Category as Categories;
            select i.Name, c.CategoryName 
            from Items() i 
            inner join Categories() c on i.Status = c.CategoryId
            take 5";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeMultiApiSchemaProvider(multiApi), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - Multi-table means no hints for any source
        Assert.IsNotNull(capturedItemsContext, "Items RuntimeContext should be captured");
        Assert.IsNotNull(capturedCategoriesContext, "Categories RuntimeContext should be captured");

        Assert.IsFalse(capturedItemsContext.QueryHints.HasOptimizationHints,
            "Multi-table query should not pass hints to Items");
        Assert.IsFalse(capturedCategoriesContext.QueryHints.HasOptimizationHints,
            "Multi-table query should not pass hints to Categories");
    }

    [TestMethod]
    public void CoupledSchema_NoOptimizationClauses_ShouldNotPassAnyHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new ApiDataSourceQueryHintsIntegrationTests.FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new ApiDataSourceQueryHintsIntegrationTests.FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        var query = @"
            table Person { Name 'System.String', Status 'System.String', Priority 'System.Int32' };
            couple #api.items with table Person as People;
            select p.Name, p.Priority from People() p";

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new ApiDataSourceQueryHintsIntegrationTests.FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - No optimization clauses means QueryHints.Empty
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints,
            "No optimization clauses should result in no hints");
    }
}

