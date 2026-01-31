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
///     Integration tests for QueryHints and RuntimeContext.
///     These tests simulate a fake API datasource that captures the RuntimeContext
///     to verify that QueryHints (Skip, Take, Distinct) and other context information
///     flow correctly through the query compilation and execution pipeline.
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

    #region Environment Variables Tests

    [TestMethod]
    public void FakeApi_ShouldReceiveEnvironmentVariables()
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
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.EnvironmentVariables, "EnvironmentVariables should not be null");
    }

    #endregion

    #region CancellationToken Tests

    [TestMethod]
    public void FakeApi_ShouldReceiveCancellationToken()
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
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.EndWorkToken);
    }

    #endregion

    #region Helper Methods

    private CompiledQuery CreateCompiledQuery(string query, FakeApiDataSource api)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new FakeApiSchemaProvider(api),
            LoggerResolver,
            TestCompilationOptions);
    }

    #endregion

    #region QueryHints Capture Tests

    [TestMethod]
    public void FakeApi_SimpleSelect_ShouldReceiveEmptyQueryHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Item1" },
                new FakeApiEntity { Id = 2, Name = "Item2" }
            };
        });

        // Act
        var query = "select Id, Name from #api.items()";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext, "RuntimeContext should be captured");
        Assert.IsNotNull(capturedContext.QueryHints, "QueryHints should not be null");
        Assert.IsNull(capturedContext.QueryHints.SkipValue, "SkipValue should be null for simple select");
        Assert.IsNull(capturedContext.QueryHints.TakeValue, "TakeValue should be null for simple select");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false for simple select");
        Assert.IsFalse(capturedContext.QueryHints.HasOptimizationHints, "HasOptimizationHints should be false");

        Assert.AreEqual(2, table.Count);
        // Verify both items are present (order may vary)
        var ids = new HashSet<int> { (int)table[0][0], (int)table[1][0] };
        Assert.IsTrue(ids.Contains(1), "Should contain Id=1");
        Assert.IsTrue(ids.Contains(2), "Should contain Id=2");
    }

    [TestMethod]
    public void FakeApi_WithSkip_ShouldReceiveSkipHint()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 10).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        // Act
        var query = "select Id, Name from #api.items() order by Id skip 3";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QueryHints);
        Assert.AreEqual(3L, capturedContext.QueryHints.SkipValue, "SkipValue should be 3");
        Assert.IsNull(capturedContext.QueryHints.TakeValue, "TakeValue should be null");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "HasOptimizationHints should be true");

        // Verify correct rows returned after skip
        Assert.AreEqual(7, table.Count); // 10 - 3 = 7 rows
        Assert.AreEqual(4, table[0][0]); // First row should be Id=4 (after skipping 3)
    }

    [TestMethod]
    public void FakeApi_WithTake_ShouldReceiveTakeHint()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 10).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        // Act
        var query = "select Id, Name from #api.items() order by Id take 5";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QueryHints);
        Assert.IsNull(capturedContext.QueryHints.SkipValue, "SkipValue should be null");
        Assert.AreEqual(5L, capturedContext.QueryHints.TakeValue, "TakeValue should be 5");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "HasOptimizationHints should be true");
        Assert.AreEqual(5L, capturedContext.QueryHints.EffectiveMaxRowsToFetch, "EffectiveMaxRowsToFetch should be 5");

        // Verify correct rows returned after take
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(5, table[4][0]);
    }

    [TestMethod]
    public void FakeApi_WithSkipAndTake_ShouldReceiveBothHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return Enumerable.Range(1, 20).Select(i => new FakeApiEntity { Id = i, Name = $"Item{i}" });
        });

        // Act
        var query = "select Id, Name from #api.items() order by Id skip 5 take 3";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QueryHints);
        Assert.AreEqual(5L, capturedContext.QueryHints.SkipValue, "SkipValue should be 5");
        Assert.AreEqual(3L, capturedContext.QueryHints.TakeValue, "TakeValue should be 3");
        Assert.IsFalse(capturedContext.QueryHints.IsDistinct, "IsDistinct should be false");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "HasOptimizationHints should be true");
        Assert.AreEqual(8L, capturedContext.QueryHints.EffectiveMaxRowsToFetch,
            "EffectiveMaxRowsToFetch should be Skip+Take=8");

        // Verify correct rows - skip 5, take 3 means rows 6, 7, 8
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(6, table[0][0]);
        Assert.AreEqual(7, table[1][0]);
        Assert.AreEqual(8, table[2][0]);
    }

    [TestMethod]
    public void FakeApi_WithDistinctAndSkip_ShouldReceiveBothHints()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Apple" },
                new FakeApiEntity { Id = 2, Name = "Apple" },
                new FakeApiEntity { Id = 3, Name = "Banana" },
                new FakeApiEntity { Id = 4, Name = "Cherry" }
            };
        });

        // Act
        var query = "select distinct Name from #api.items() order by Name skip 1";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QueryHints);
        Assert.AreEqual(1L, capturedContext.QueryHints.SkipValue, "SkipValue should be 1");
        Assert.IsNull(capturedContext.QueryHints.TakeValue, "TakeValue should be null");
        Assert.IsTrue(capturedContext.QueryHints.IsDistinct, "IsDistinct should be true (combined with skip)");

        // Verify results - distinct names are Apple, Banana, Cherry, skip 1 means Banana, Cherry
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void FakeApi_WithDistinct_ShouldReceiveDistinctHint()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Apple" },
                new FakeApiEntity { Id = 2, Name = "Apple" },
                new FakeApiEntity { Id = 3, Name = "Banana" },
                new FakeApiEntity { Id = 4, Name = "Banana" },
                new FakeApiEntity { Id = 5, Name = "Cherry" }
            };
        });

        // Act
        var query = "select distinct Name from #api.items()";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QueryHints);
        Assert.IsNull(capturedContext.QueryHints.SkipValue, "SkipValue should be null");
        Assert.IsNull(capturedContext.QueryHints.TakeValue, "TakeValue should be null");
        Assert.IsTrue(capturedContext.QueryHints.IsDistinct, "IsDistinct should be true");
        Assert.IsTrue(capturedContext.QueryHints.HasOptimizationHints, "HasOptimizationHints should be true");

        // Verify distinct results
        Assert.AreEqual(3, table.Count);
        var names = new HashSet<string> { (string)table[0][0], (string)table[1][0], (string)table[2][0] };
        Assert.IsTrue(names.Contains("Apple"));
        Assert.IsTrue(names.Contains("Banana"));
        Assert.IsTrue(names.Contains("Cherry"));
    }

    #endregion

    #region QueryInformation Capture Tests

    [TestMethod]
    public void FakeApi_WithWhereClause_ShouldReceiveWhereInformation()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Active", Status = "active" },
                new FakeApiEntity { Id = 2, Name = "Inactive", Status = "inactive" },
                new FakeApiEntity { Id = 3, Name = "Pending", Status = "pending" }
            };
        });

        // Act
        var query = "select Id, Name from #api.items() where Status = 'active'";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QuerySourceInfo);

        // The WhereNode should be captured
        var whereNode = capturedContext.QuerySourceInfo.WhereNode;
        Assert.IsNotNull(whereNode, "WhereNode should be captured for predicate pushdown");

        // Verify query result
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Active", table[0][1]);
    }

    [TestMethod]
    public void FakeApi_WithMultipleConditions_ShouldReceivePredicates()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "A", Status = "active", Priority = 1 },
                new FakeApiEntity { Id = 2, Name = "B", Status = "active", Priority = 2 },
                new FakeApiEntity { Id = 3, Name = "C", Status = "inactive", Priority = 1 },
                new FakeApiEntity { Id = 4, Name = "D", Status = "active", Priority = 3 }
            };
        });

        // Act
        var query = "select Id, Name from #api.items() where Status = 'active' and Priority <= 2";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QuerySourceInfo.WhereNode);

        // Verify filtered results (order not guaranteed without ORDER BY)
        Assert.AreEqual(2, table.Count);
        var ids = new HashSet<int> { (int)table[0][0], (int)table[1][0] };
        Assert.IsTrue(ids.Contains(1), "Expected Id=1 in results");
        Assert.IsTrue(ids.Contains(2), "Expected Id=2 in results");
    }

    [TestMethod]
    public void FakeApi_WithOrCondition_ShouldReceiveOrPredicate()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Urgent", Priority = 1 },
                new FakeApiEntity { Id = 2, Name = "Normal", Priority = 2 },
                new FakeApiEntity { Id = 3, Name = "Low", Priority = 3 }
            };
        });

        // Act
        var query = "select Id, Name from #api.items() where Priority = 1 or Priority = 3";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QuerySourceInfo.WhereNode);

        // Verify filtered results
        Assert.AreEqual(2, table.Count);
        var ids = new HashSet<int> { (int)table[0][0], (int)table[1][0] };
        Assert.IsTrue(ids.Contains(1));
        Assert.IsTrue(ids.Contains(3));
    }

    #endregion

    #region Column Projection Tests

    [TestMethod]
    public void FakeApi_SelectSpecificColumns_ShouldReceiveColumnInfo()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Item1", Status = "active", Priority = 1 }
            };
        });

        // Act
        var query = "select Id, Name from #api.items()";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.IsNotNull(capturedContext.QuerySourceInfo.Columns);

        // Verify we only get the columns we need
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void FakeApi_SelectAllColumns_ShouldReturnAllData()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "Item1", Status = "active", Priority = 5 }
            };
        });

        // Act
        var query = "select Id, Name, Status, Priority from #api.items()";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual(1, table[0][0]); // Id
        Assert.AreEqual("Item1", table[0][1]); // Name
        Assert.AreEqual("active", table[0][2]); // Status
        Assert.AreEqual(5, table[0][3]); // Priority
    }

    #endregion

    #region Complex Query Tests

    [TestMethod]
    public void FakeApi_WithGroupByAndAggregate_ShouldWork()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "A", Status = "active", Priority = 1 },
                new FakeApiEntity { Id = 2, Name = "B", Status = "active", Priority = 2 },
                new FakeApiEntity { Id = 3, Name = "C", Status = "inactive", Priority = 1 },
                new FakeApiEntity { Id = 4, Name = "D", Status = "active", Priority = 3 }
            };
        });

        // Act
        var query = "select Status, Count(Status) as Cnt from #api.items() group by Status";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.AreEqual(2, table.Count); // active and inactive
    }

    [TestMethod]
    public void FakeApi_WithOrderBy_ShouldReturnOrderedResults()
    {
        // Arrange
        var api = new FakeApiDataSource(_ => new[]
        {
            new FakeApiEntity { Id = 3, Name = "Charlie" },
            new FakeApiEntity { Id = 1, Name = "Alice" },
            new FakeApiEntity { Id = 2, Name = "Bob" }
        });

        // Act
        var query = "select Id, Name from #api.items() order by Name asc";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0][1]);
        Assert.AreEqual("Bob", table[1][1]);
        Assert.AreEqual("Charlie", table[2][1]);
    }

    [TestMethod]
    public void FakeApi_WithCTE_ShouldWork()
    {
        // Arrange
        RuntimeContext capturedContext = null;
        var api = new FakeApiDataSource(ctx =>
        {
            capturedContext = ctx;
            return new[]
            {
                new FakeApiEntity { Id = 1, Name = "A", Priority = 1 },
                new FakeApiEntity { Id = 2, Name = "B", Priority = 2 },
                new FakeApiEntity { Id = 3, Name = "C", Priority = 3 }
            };
        });

        // Act
        var query = @"
            with high_priority as (
                select Id, Name from #api.items() where Priority <= 2
            )
            select Id, Name from high_priority";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(capturedContext);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void FakeApi_EmptyResult_ShouldReturnEmptyTable()
    {
        // Arrange
        var api = new FakeApiDataSource(_ => Array.Empty<FakeApiEntity>());

        // Act
        var query = "select Id, Name from #api.items()";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void FakeApi_LargeDataset_ShouldHandleEfficiently()
    {
        // Arrange
        var api = new FakeApiDataSource(_ =>
            Enumerable.Range(1, 1000).Select(i => new FakeApiEntity
            {
                Id = i,
                Name = $"Item{i}",
                Priority = i % 10
            }));

        // Act
        var query = "select Id, Name from #api.items() where Priority = 5 order by Id take 10";
        var vm = CreateCompiledQuery(query, api);
        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(10, table.Count);
        Assert.AreEqual(5, table[0][0]); // First item with Priority=5 is Id=5
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

    #endregion
}
