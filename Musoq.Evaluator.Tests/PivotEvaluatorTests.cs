using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotEvaluatorTests : BasicEntityTestBase
{
    [TestMethod]
    public void BasicPivotWithSum_ShouldReturnCorrectResults()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics', 'Fashion')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),
                    new SalesEntity("Books", "Book2", 5, 50m),
                    new SalesEntity("Electronics", "Phone", 3, 300m),
                    new SalesEntity("Fashion", "Shirt", 8, 80m),
                    new SalesEntity("Electronics", "Laptop", 2, 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Columns.Count());
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Books"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Electronics"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Fashion"));

        Assert.AreEqual(1, table.Count);
        var row = table[0];
        
        // Verify aggregated values
        var booksColumn = table.Columns.Single(c => c.ColumnName == "Books");
        var electronicsColumn = table.Columns.Single(c => c.ColumnName == "Electronics");
        var fashionColumn = table.Columns.Single(c => c.ColumnName == "Fashion");
        
        Assert.AreEqual(15, row[booksColumn.ColumnIndex]); // 10 + 5
        Assert.AreEqual(5, row[electronicsColumn.ColumnIndex]); // 3 + 2
        Assert.AreEqual(8, row[fashionColumn.ColumnIndex]); // 8
    }

    [TestMethod]
    public void PivotWithMultipleAggregations_ShouldReturnCorrectResults()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity), Avg(Revenue), Count(Product)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),
                    new SalesEntity("Books", "Book2", 5, 50m),
                    new SalesEntity("Electronics", "Phone", 3, 300m),
                    new SalesEntity("Electronics", "Laptop", 2, 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(6, table.Columns.Count()); // 2 categories * 3 aggregations
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Books_Sum_Quantity"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Books_Avg_Revenue"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Books_Count_Product"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Electronics_Sum_Quantity"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Electronics_Avg_Revenue"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Electronics_Count_Product"));

        Assert.AreEqual(1, table.Count);
        var row = table[0];
        
        var booksSumColumn = table.Columns.Single(c => c.ColumnName == "Books_Sum_Quantity");
        var booksAvgColumn = table.Columns.Single(c => c.ColumnName == "Books_Avg_Revenue");
        var booksCountColumn = table.Columns.Single(c => c.ColumnName == "Books_Count_Product");
        
        Assert.AreEqual(15, row[booksSumColumn.ColumnIndex]); // 10 + 5
        Assert.AreEqual(75m, row[booksAvgColumn.ColumnIndex]); // (100 + 50) / 2
        Assert.AreEqual(2, row[booksCountColumn.ColumnIndex]); // 2 products
    }

    [TestMethod]
    public void PivotByMonth_ShouldReturnCorrectResults()
    {
        var query = @"
            SELECT Category, Jan, Feb, Mar
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Month IN ('Jan', 'Feb', 'Mar')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", "Jan", 10, 100m),
                    new SalesEntity("Books", "Book2", "Feb", 15, 150m),
                    new SalesEntity("Electronics", "Phone", "Jan", 5, 500m),
                    new SalesEntity("Electronics", "Tablet", "Mar", 8, 800m),
                    new SalesEntity("Fashion", "Shirt", "Feb", 12, 120m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Columns.Count()); // Category + 3 months
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Category"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Jan"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Feb"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Mar"));

        Assert.AreEqual(3, table.Count); // 3 categories

        // Find Books row
        var booksRow = table.Single(row => 
            (string)row[table.Columns.Single(c => c.ColumnName == "Category").ColumnIndex] == "Books");
        Assert.AreEqual(10, booksRow[table.Columns.Single(c => c.ColumnName == "Jan").ColumnIndex]);
        Assert.AreEqual(15, booksRow[table.Columns.Single(c => c.ColumnName == "Feb").ColumnIndex]);
        Assert.AreEqual(null, booksRow[table.Columns.Single(c => c.ColumnName == "Mar").ColumnIndex]); // No data for Mar

        // Find Electronics row
        var electronicsRow = table.Single(row => 
            (string)row[table.Columns.Single(c => c.ColumnName == "Category").ColumnIndex] == "Electronics");
        Assert.AreEqual(5, electronicsRow[table.Columns.Single(c => c.ColumnName == "Jan").ColumnIndex]);
        Assert.AreEqual(null, electronicsRow[table.Columns.Single(c => c.ColumnName == "Feb").ColumnIndex]); // No data for Feb
        Assert.AreEqual(8, electronicsRow[table.Columns.Single(c => c.ColumnName == "Mar").ColumnIndex]);
    }

    [TestMethod]
    public void PivotWithNumericColumns_ShouldReturnCorrectResults()
    {
        var query = @"
            SELECT Category, [2020], [2021], [2022]
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Year IN (2020, 2021, 2022)
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity { Category = "Books", Year = 2020, Quantity = 100 },
                    new SalesEntity { Category = "Books", Year = 2021, Quantity = 150 },
                    new SalesEntity { Category = "Electronics", Year = 2020, Quantity = 80 },
                    new SalesEntity { Category = "Electronics", Year = 2022, Quantity = 120 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Columns.Count()); // Category + 3 years
        Assert.AreEqual(2, table.Count); // 2 categories

        var booksRow = table.Single(row => 
            (string)row[table.Columns.Single(c => c.ColumnName == "Category").ColumnIndex] == "Books");
        Assert.AreEqual(100, booksRow[table.Columns.Single(c => c.ColumnName == "2020").ColumnIndex]);
        Assert.AreEqual(150, booksRow[table.Columns.Single(c => c.ColumnName == "2021").ColumnIndex]);
        Assert.AreEqual(null, booksRow[table.Columns.Single(c => c.ColumnName == "2022").ColumnIndex]);
    }

    [TestMethod]
    public void PivotWithWhereClause_ShouldFilterData()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            WHERE Quantity > 5
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics', 'Fashion')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),  // Included
                    new SalesEntity("Books", "Book2", 3, 30m),    // Excluded (Quantity <= 5)
                    new SalesEntity("Electronics", "Phone", 8, 300m), // Included
                    new SalesEntity("Fashion", "Shirt", 2, 20m)   // Excluded (Quantity <= 5)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        var row = table[0];
        
        var booksColumn = table.Columns.Single(c => c.ColumnName == "Books");
        var electronicsColumn = table.Columns.Single(c => c.ColumnName == "Electronics");
        var fashionColumn = table.Columns.Single(c => c.ColumnName == "Fashion");
        
        Assert.AreEqual(10, row[booksColumn.ColumnIndex]); // Only Book1
        Assert.AreEqual(8, row[electronicsColumn.ColumnIndex]); // Only Phone
        Assert.AreEqual(null, row[fashionColumn.ColumnIndex]); // No items after filter
    }

    [TestMethod]
    public void PivotWithDynamicColumns_ShouldReturnCorrectResults()
    {
        var query = @"
            WITH Categories AS (
                SELECT DISTINCT Category 
                FROM #A.Entities()
                WHERE Quantity > 0
            )
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN (SELECT Category FROM Categories)
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),
                    new SalesEntity("Electronics", "Phone", 5, 300m),
                    new SalesEntity("Fashion", "Shirt", 0, 0m), // Should be excluded from dynamic columns
                    new SalesEntity("Sports", "Ball", 3, 30m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        // Should have columns for Books, Electronics, Sports (Fashion excluded due to Quantity = 0)
        Assert.AreEqual(3, table.Columns.Count());
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Books"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Electronics"));
        Assert.IsTrue(table.Columns.Any(c => c.ColumnName == "Sports"));
        Assert.IsFalse(table.Columns.Any(c => c.ColumnName == "Fashion"));
    }

    [TestMethod]
    public void PivotWithComplexAggregation_ShouldReturnCorrectResults()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity * Revenue)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 2, 50m),    // 2 * 50 = 100
                    new SalesEntity("Books", "Book2", 3, 30m),    // 3 * 30 = 90
                    new SalesEntity("Electronics", "Phone", 1, 300m) // 1 * 300 = 300
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        var row = table[0];
        
        var booksColumn = table.Columns.Single(c => c.ColumnName == "Books");
        var electronicsColumn = table.Columns.Single(c => c.ColumnName == "Electronics");
        
        Assert.AreEqual(190m, row[booksColumn.ColumnIndex]); // 100 + 90
        Assert.AreEqual(300m, row[electronicsColumn.ColumnIndex]); // 300
    }

    [TestMethod]
    public void PivotWithEmptyData_ShouldReturnEmptyResult()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            { "#A", new List<SalesEntity>() }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count()); // Books, Electronics columns
        Assert.AreEqual(0, table.Count); // No data rows
    }

    [TestMethod]
    public void PivotWithNullValues_ShouldHandleCorrectly()
    {
        var query = @"
            SELECT *
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics', 'Fashion')
            ) AS p";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity("Books", "Book1", 10, 100m),
                    new SalesEntity(null, "Unknown", 5, 50m), // Null category
                    new SalesEntity("Electronics", "Phone", 3, 300m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        var row = table[0];
        
        var booksColumn = table.Columns.Single(c => c.ColumnName == "Books");
        var electronicsColumn = table.Columns.Single(c => c.ColumnName == "Electronics");
        var fashionColumn = table.Columns.Single(c => c.ColumnName == "Fashion");
        
        Assert.AreEqual(10, row[booksColumn.ColumnIndex]);
        Assert.AreEqual(3, row[electronicsColumn.ColumnIndex]);
        Assert.AreEqual(null, row[fashionColumn.ColumnIndex]); // No Fashion data
        // Null category data should be ignored in pivot
    }

    [TestMethod]
    public void PivotWithGroupBy_ShouldGroupAndPivot()
    {
        var query = @"
            SELECT Region, Books, Electronics
            FROM #A.Entities()
            PIVOT (
                Sum(Quantity)
                FOR Category IN ('Books', 'Electronics')
            ) AS p
            GROUP BY Region";

        var sources = new Dictionary<string, IEnumerable<SalesEntity>>
        {
            {
                "#A", [
                    new SalesEntity { Category = "Books", Region = "North", Quantity = 10 },
                    new SalesEntity { Category = "Books", Region = "North", Quantity = 5 },
                    new SalesEntity { Category = "Electronics", Region = "North", Quantity = 8 },
                    new SalesEntity { Category = "Books", Region = "South", Quantity = 12 },
                    new SalesEntity { Category = "Electronics", Region = "South", Quantity = 6 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Columns.Count()); // Region, Books, Electronics
        Assert.AreEqual(2, table.Count); // North, South regions

        var northRow = table.Single(row => 
            (string)row[table.Columns.Single(c => c.ColumnName == "Region").ColumnIndex] == "North");
        Assert.AreEqual(15, northRow[table.Columns.Single(c => c.ColumnName == "Books").ColumnIndex]); // 10 + 5
        Assert.AreEqual(8, northRow[table.Columns.Single(c => c.ColumnName == "Electronics").ColumnIndex]);

        var southRow = table.Single(row => 
            (string)row[table.Columns.Single(c => c.ColumnName == "Region").ColumnIndex] == "South");
        Assert.AreEqual(12, southRow[table.Columns.Single(c => c.ColumnName == "Books").ColumnIndex]);
        Assert.AreEqual(6, southRow[table.Columns.Single(c => c.ColumnName == "Electronics").ColumnIndex]);
    }

    protected new CompiledQuery CreateAndRunVirtualMachine<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources)
        where T : SalesEntity
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new SalesSchemaProvider<T>(sources),
            LoggerResolver);
    }
}

// Schema provider for SalesEntity
public class SalesSchemaProvider<T>(IDictionary<string, IEnumerable<T>> sources) : ISchemaProvider
    where T : SalesEntity
{
    public ISchema GetSchema(string schema)
    {
        if (sources.TryGetValue(schema, out var value) == false)
            throw new Musoq.Evaluator.Tests.Exceptions.SchemaNotFoundException();
        
        return new GenericSchema<T, SalesEntityTable>(value, SalesEntity.TestNameToIndexMap, 
            SalesEntity.TestIndexToObjectAccessMap.ToDictionary(kvp => kvp.Key, kvp => (Func<T, object>)(obj => kvp.Value((SalesEntity)(object)obj))));
    }
}