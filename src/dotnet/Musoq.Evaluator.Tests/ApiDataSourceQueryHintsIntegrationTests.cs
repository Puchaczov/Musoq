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
///     Integration tests for QueryHints distribution to data sources.
///     These tests verify that SKIP/TAKE/DISTINCT hints are correctly passed (or not passed)
///     to data sources based on query structure:
///     - Single-table without ORDER BY, GROUP BY, or DISTINCT: hints ARE passed
///     - Single-table with ORDER BY: hints are NOT passed (sorting happens after retrieval)
///     - Single-table with GROUP BY: hints are NOT passed (grouping happens after retrieval)
///     - Single-table with DISTINCT: hints are NOT passed (DISTINCT creates implicit GROUP BY)
///     - Multi-table (JOIN/APPLY): hints are NOT passed (optimization applies to joined result)
/// </summary>
[TestClass]
public class ApiDataSourceQueryHintsIntegrationTests
{
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static ApiDataSourceQueryHintsIntegrationTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    public TestContext TestContext { get; set; }

    private ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    #region Single-Table QueryHints Tests

    [DataTestMethod]
    [DataRow("select Id from #api.items()", null, null, false, DisplayName = "No optimization clauses")]
    [DataRow("select Id from #api.items() skip 10", 10L, null, false, DisplayName = "SKIP only")]
    [DataRow("select Id from #api.items() take 5", null, 5L, false, DisplayName = "TAKE only")]
    [DataRow("select Id from #api.items() skip 10 take 5", 10L, 5L, false, DisplayName = "SKIP and TAKE")]
    public void SingleTable_WithoutOrderBy_ShouldPassHints(string query, long? expectedSkip, long? expectedTake, bool expectedDistinct)
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.AreEqual(expectedSkip, capturedContext.QueryHints.SkipValue, $"SkipValue mismatch for: {query}");
        Assert.AreEqual(expectedTake, capturedContext.QueryHints.TakeValue, $"TakeValue mismatch for: {query}");
        Assert.AreEqual(expectedDistinct, capturedContext.QueryHints.IsDistinct, $"IsDistinct mismatch for: {query}");
        
        var hasHints = expectedSkip.HasValue || expectedTake.HasValue || expectedDistinct;
        Assert.AreEqual(hasHints, capturedContext.QueryHints.HasOptimizationHints, 
            $"HasOptimizationHints mismatch for: {query}");
    }

    [DataTestMethod]
    [DataRow("select Id from #api.items() order by Id", DisplayName = "ORDER BY only")]
    [DataRow("select Id from #api.items() order by Id skip 10", DisplayName = "ORDER BY with SKIP")]
    [DataRow("select Id from #api.items() order by Id take 5", DisplayName = "ORDER BY with TAKE")]
    [DataRow("select Id from #api.items() order by Id skip 10 take 5", DisplayName = "ORDER BY with SKIP and TAKE")]
    [DataRow("select distinct Id from #api.items() order by Id", DisplayName = "ORDER BY with DISTINCT")]
    [DataRow("select distinct Id from #api.items() order by Id skip 5", DisplayName = "ORDER BY with DISTINCT and SKIP")]
    [DataRow("select Id from #api.items() order by Id desc take 3", DisplayName = "ORDER BY DESC with TAKE")]
    [DataRow("select Id from #api.items() order by Name, Id skip 2 take 3", DisplayName = "Multi-column ORDER BY")]
    public void SingleTable_WithOrderBy_ShouldNotPassHints(string query)
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - ORDER BY means no hints should be passed to data source
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints, 
            $"ORDER BY should prevent hints from being passed for: {query}");
    }

    [DataTestMethod]
    [DataRow("select distinct Id from #api.items()", DisplayName = "DISTINCT only")]
    [DataRow("select distinct Id from #api.items() skip 5", DisplayName = "DISTINCT with SKIP")]
    [DataRow("select distinct Id from #api.items() take 3", DisplayName = "DISTINCT with TAKE")]
    [DataRow("select distinct Id from #api.items() skip 2 take 3", DisplayName = "DISTINCT with SKIP and TAKE")]
    public void SingleTable_WithDistinct_ShouldNotPassHints(string query)
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i % 10}" });
        });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - DISTINCT creates implicit GROUP BY, so no hints should be passed
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints, 
            $"DISTINCT creates implicit GROUP BY and should prevent hints from being passed for: {query}");
    }

    [DataTestMethod]
    [DataRow("select Name, Count(Id) from #api.items() group by Name", DisplayName = "GROUP BY only")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name skip 10", DisplayName = "GROUP BY with SKIP")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name take 5", DisplayName = "GROUP BY with TAKE")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name skip 10 take 5", DisplayName = "GROUP BY with SKIP and TAKE")]
    [DataRow("select distinct Name from #api.items() group by Name", DisplayName = "GROUP BY with DISTINCT")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name having Count(Id) > 5", DisplayName = "GROUP BY with HAVING")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name having Count(Id) > 5 take 3", DisplayName = "GROUP BY with HAVING and TAKE")]
    [DataRow("select Name, Count(Id) from #api.items() group by Name order by Count(Id) desc take 5", DisplayName = "GROUP BY with ORDER BY and TAKE")]
    public void SingleTable_WithGroupBy_ShouldNotPassHints(string query)
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 100).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i % 10}" });
        });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - GROUP BY means no hints should be passed to data source
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints, 
            $"GROUP BY should prevent hints from being passed for: {query}");
    }

    #endregion

    #region Multi-Table QueryHints Tests

    [DataTestMethod]
    [DataRow("select a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId", 
        DisplayName = "INNER JOIN")]
    [DataRow("select a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId skip 5", 
        DisplayName = "INNER JOIN with SKIP")]
    [DataRow("select a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId take 10", 
        DisplayName = "INNER JOIN with TAKE")]
    [DataRow("select a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId skip 5 take 10", 
        DisplayName = "INNER JOIN with SKIP and TAKE")]
    [DataRow("select distinct a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId", 
        DisplayName = "INNER JOIN with DISTINCT")]
    [DataRow("select a.Id from #multiapi.items() a left outer join #multiapi.categories() b on a.Status = b.CategoryId take 5", 
        DisplayName = "LEFT JOIN with TAKE")]
    [DataRow("select a.Id from #multiapi.items() a cross apply #multiapi.categories() b", 
        DisplayName = "CROSS APPLY")]
    [DataRow("select a.Id from #multiapi.items() a cross apply #multiapi.categories() b skip 3 take 5", 
        DisplayName = "CROSS APPLY with SKIP and TAKE")]
    public void MultiTable_ShouldNotPassHintsToAnySources(string query)
    {
        // Arrange
        RuntimeContext capturedItemsContext = null;
        RuntimeContext capturedCategoriesContext = null;
        
        var multiApi = new FakeMultiApiDataSource(
            itemsProvider: ctx =>
            {
                capturedItemsContext = ctx;
                return Enumerable.Range(1, 50).Select(i => new FakeApiEntity 
                { 
                    Id = i, 
                    Name = $"Item{i}", 
                    Status = $"cat{i % 3}" 
                });
            },
            categoriesProvider: ctx =>
            {
                capturedCategoriesContext = ctx;
                return Enumerable.Range(0, 3).Select(i => new FakeCategoryEntity 
                { 
                    CategoryId = $"cat{i}", 
                    CategoryName = $"Category {i}" 
                });
            });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeMultiApiSchemaProvider(multiApi), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - Multi-table queries should NEVER pass hints to individual sources
        Assert.IsNotNull(capturedItemsContext, "Items context should be captured");
        Assert.IsNotNull(capturedCategoriesContext, "Categories context should be captured");
        
        // Items source should have empty hints
        Assert.IsNull(capturedItemsContext.QueryHints.SkipValue, $"Items SkipValue should be null for: {query}");
        Assert.IsNull(capturedItemsContext.QueryHints.TakeValue, $"Items TakeValue should be null for: {query}");
        Assert.IsFalse(capturedItemsContext.QueryHints.IsDistinct, $"Items IsDistinct should be false for: {query}");
        Assert.IsFalse(capturedItemsContext.QueryHints.HasOptimizationHints, $"Items should have no hints for: {query}");
        
        // Categories source should have empty hints
        Assert.IsNull(capturedCategoriesContext.QueryHints.SkipValue, $"Categories SkipValue should be null for: {query}");
        Assert.IsNull(capturedCategoriesContext.QueryHints.TakeValue, $"Categories TakeValue should be null for: {query}");
        Assert.IsFalse(capturedCategoriesContext.QueryHints.IsDistinct, $"Categories IsDistinct should be false for: {query}");
        Assert.IsFalse(capturedCategoriesContext.QueryHints.HasOptimizationHints, $"Categories should have no hints for: {query}");
    }

    [DataTestMethod]
    [DataRow("select a.Id from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId order by a.Id skip 5", 
        DisplayName = "JOIN with ORDER BY and SKIP")]
    [DataRow("select a.Id from #multiapi.items() a cross apply #multiapi.categories() b order by a.Name take 10", 
        DisplayName = "CROSS APPLY with ORDER BY and TAKE")]
    [DataRow("select distinct a.Status from #multiapi.items() a inner join #multiapi.categories() b on a.Status = b.CategoryId order by a.Status", 
        DisplayName = "JOIN with DISTINCT and ORDER BY")]
    public void MultiTable_WithOrderBy_ShouldNotPassHintsToAnySources(string query)
    {
        // Arrange
        RuntimeContext capturedItemsContext = null;
        RuntimeContext capturedCategoriesContext = null;
        
        var multiApi = new FakeMultiApiDataSource(
            itemsProvider: ctx =>
            {
                capturedItemsContext = ctx;
                return Enumerable.Range(1, 50).Select(i => new FakeApiEntity 
                { 
                    Id = i, 
                    Name = $"Item{i}", 
                    Status = $"cat{i % 3}" 
                });
            },
            categoriesProvider: ctx =>
            {
                capturedCategoriesContext = ctx;
                return Enumerable.Range(0, 3).Select(i => new FakeCategoryEntity 
                { 
                    CategoryId = $"cat{i}", 
                    CategoryName = $"Category {i}" 
                });
            });

        // Act
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeMultiApiSchemaProvider(multiApi), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert - Multi-table with ORDER BY: double reason for no hints
        Assert.IsNotNull(capturedItemsContext);
        Assert.IsNotNull(capturedCategoriesContext);
        
        Assert.IsFalse(capturedItemsContext.QueryHints.HasOptimizationHints, $"Items should have no hints for: {query}");
        Assert.IsFalse(capturedCategoriesContext.QueryHints.HasOptimizationHints, $"Categories should have no hints for: {query}");
    }

    #endregion

    #region RuntimeContext Integration Tests

    [TestMethod]
    public void RuntimeContext_ShouldReceiveEnvironmentVariables()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[] { new FakeApiEntity { Id = 1, Name = "Test" } };
        });

        // Act
        var query = "select Id, Name from #api.items()";
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.EnvironmentVariables, "EnvironmentVariables should not be null");
    }

    [TestMethod]
    public void RuntimeContext_ShouldReceiveCancellationToken()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[] { new FakeApiEntity { Id = 1, Name = "Test" } };
        });

        // Act
        var query = "select Id from #api.items()";
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.EndWorkToken);
    }

    [TestMethod]
    public void RuntimeContext_WithWhereClause_ShouldReceiveWhereNode()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Active", Status = "active" },
                new FakeApiEntity { Id = 2, Name = "Inactive", Status = "inactive" }
            };
        });

        // Act
        var query = "select Id, Name from #api.items() where Status = 'active'";
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QuerySourceInfo);
        Assert.IsNotNull(capturedContext.QuerySourceInfo.WhereNode, "WhereNode should be captured for predicate pushdown");
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void RuntimeContext_QueryResult_ShouldBeCorrect()
    {
        // Arrange
        var api = new FakeApiDataSource(_ => Enumerable.Range(1, 20).Select(i => new FakeApiEntity 
        { 
            Id = i, 
            Name = $"Item{i:D2}" 
        }));

        // Act
        var query = "select Id, Name from #api.items() where Id > 5 and Id <= 10";
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), 
            new FakeApiSchemaProvider(api), LoggerResolver, TestCompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(5, table.Count); // Ids 6, 7, 8, 9, 10
        Assert.AreEqual(2, table.Columns.Count()); // Id and Name columns
    }

    #endregion

    #region Test Infrastructure

    /// <summary>
    ///     Represents an entity returned from a fake API.
    /// </summary>
    public class FakeApiEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Id), 0 },
            { nameof(Name), 1 },
            { nameof(Status), 2 },
            { nameof(Priority), 3 }
        };

        public static readonly IReadOnlyDictionary<int, Func<FakeApiEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<FakeApiEntity, object>>
            {
                { 0, e => e.Id },
                { 1, e => e.Name },
                { 2, e => e.Status },
                { 3, e => e.Priority }
            };

        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    ///     Fake API data source that captures RuntimeContext and returns configured entities.
    /// </summary>
    public class FakeApiDataSource
    {
        private readonly Func<RuntimeContext, IEnumerable<FakeApiEntity>> _dataProvider;

        public FakeApiDataSource(Func<RuntimeContext, IEnumerable<FakeApiEntity>> dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public IEnumerable<FakeApiEntity> GetItems(RuntimeContext context)
        {
            return _dataProvider(context);
        }
    }

    /// <summary>
    ///     Schema provider for the fake API.
    /// </summary>
    public class FakeApiSchemaProvider : ISchemaProvider
    {
        private readonly FakeApiDataSource _api;

        public FakeApiSchemaProvider(FakeApiDataSource api)
        {
            _api = api;
        }

        public ISchema GetSchema(string schema)
        {
            return new FakeApiSchema(_api);
        }
    }

    /// <summary>
    ///     Schema for the fake API data source.
    /// </summary>
    public class FakeApiSchema : SchemaBase
    {
        private readonly FakeApiDataSource _api;

        public FakeApiSchema(FakeApiDataSource api) : base("api", CreateLibrary())
        {
            _api = api;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new FakeApiTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            var entities = _api.GetItems(runtimeContext);
            return new FakeApiRowSource(entities);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var lib = new Library();
            methodsManager.RegisterLibraries(lib);
            return new MethodsAggregator(methodsManager);
        }
    }

    /// <summary>
    ///     Table schema for the fake API entities.
    /// </summary>
    public class FakeApiTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(FakeApiEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(FakeApiEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(FakeApiEntity.Status), 2, typeof(string)),
            new SchemaColumn(nameof(FakeApiEntity.Priority), 3, typeof(int))
        ];

        public SchemaTableMetadata Metadata => new(typeof(FakeApiEntity));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.FirstOrDefault(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }
    }

    /// <summary>
    ///     Row source for fake API entities.
    /// </summary>
    public class FakeApiRowSource : RowSource
    {
        private readonly IEnumerable<FakeApiEntity> _entities;

        public FakeApiRowSource(IEnumerable<FakeApiEntity> entities)
        {
            _entities = entities;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                foreach (var entity in _entities)
                    yield return new Musoq.Schema.DataSources.EntityResolver<FakeApiEntity>(
                        entity,
                        FakeApiEntity.NameToIndexMap,
                        FakeApiEntity.IndexToObjectAccessMap);
            }
        }
    }

    #region Multi-API Test Infrastructure

    /// <summary>
    ///     Category entity for multi-table JOIN tests.
    /// </summary>
    public class FakeCategoryEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(CategoryId), 0 },
            { nameof(CategoryName), 1 }
        };

        public static readonly IReadOnlyDictionary<int, Func<FakeCategoryEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<FakeCategoryEntity, object>>
            {
                { 0, e => e.CategoryId },
                { 1, e => e.CategoryName }
            };

        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    /// <summary>
    ///     Multi-API data source that provides both items and categories for JOIN tests.
    /// </summary>
    public class FakeMultiApiDataSource
    {
        private readonly Func<RuntimeContext, IEnumerable<FakeApiEntity>> _itemsProvider;
        private readonly Func<RuntimeContext, IEnumerable<FakeCategoryEntity>> _categoriesProvider;

        public FakeMultiApiDataSource(
            Func<RuntimeContext, IEnumerable<FakeApiEntity>> itemsProvider,
            Func<RuntimeContext, IEnumerable<FakeCategoryEntity>> categoriesProvider)
        {
            _itemsProvider = itemsProvider;
            _categoriesProvider = categoriesProvider;
        }

        public IEnumerable<FakeApiEntity> GetItems(RuntimeContext context) => _itemsProvider(context);
        public IEnumerable<FakeCategoryEntity> GetCategories(RuntimeContext context) => _categoriesProvider(context);
    }

    /// <summary>
    ///     Schema provider for multi-API that supports multiple table types.
    /// </summary>
    public class FakeMultiApiSchemaProvider : ISchemaProvider
    {
        private readonly FakeMultiApiDataSource _api;

        public FakeMultiApiSchemaProvider(FakeMultiApiDataSource api)
        {
            _api = api;
        }

        public ISchema GetSchema(string schema)
        {
            return new FakeMultiApiSchema(_api);
        }
    }

    /// <summary>
    ///     Schema for multi-API that can return items or categories tables.
    /// </summary>
    public class FakeMultiApiSchema : SchemaBase
    {
        private readonly FakeMultiApiDataSource _api;

        public FakeMultiApiSchema(FakeMultiApiDataSource api) : base("multiapi", CreateLibrary())
        {
            _api = api;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return name switch
            {
                "items" => new FakeApiTable(),
                "categories" => new FakeCategoryTable(),
                _ => throw new InvalidOperationException($"Unknown table: {name}")
            };
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return name switch
            {
                "items" => new FakeApiRowSource(_api.GetItems(runtimeContext)),
                "categories" => new FakeCategoryRowSource(_api.GetCategories(runtimeContext)),
                _ => throw new InvalidOperationException($"Unknown row source: {name}")
            };
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var lib = new Library();
            methodsManager.RegisterLibraries(lib);
            return new MethodsAggregator(methodsManager);
        }
    }

    /// <summary>
    ///     Table schema for category entities.
    /// </summary>
    public class FakeCategoryTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(FakeCategoryEntity.CategoryId), 0, typeof(string)),
            new SchemaColumn(nameof(FakeCategoryEntity.CategoryName), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata => new(typeof(FakeCategoryEntity));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.FirstOrDefault(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }
    }

    /// <summary>
    ///     Row source for category entities.
    /// </summary>
    public class FakeCategoryRowSource : RowSource
    {
        private readonly IEnumerable<FakeCategoryEntity> _entities;

        public FakeCategoryRowSource(IEnumerable<FakeCategoryEntity> entities)
        {
            _entities = entities;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                foreach (var entity in _entities)
                    yield return new Musoq.Schema.DataSources.EntityResolver<FakeCategoryEntity>(
                        entity,
                        FakeCategoryEntity.NameToIndexMap,
                        FakeCategoryEntity.IndexToObjectAccessMap);
            }
        }
    }

    #endregion

    #endregion
}
