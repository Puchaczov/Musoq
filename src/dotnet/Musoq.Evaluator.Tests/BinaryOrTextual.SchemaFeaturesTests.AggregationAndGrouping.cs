using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    #region Aggregation and Grouping E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithAggregation_ShouldSumValues()
    {
        var query = @"
            binary Amount {
                Value: int le
            };
            select
                Sum(a.Value) as TotalValue,
                Count(a.Value) as RecordCount,
                Avg(a.Value) as AvgValue
            from #test.files() f
            cross apply Interpret<Amount>(f.Content) a";

        var entities = new[]
        {
            new BinaryEntity { Name = "a1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "a2.bin", Content = BitConverter.GetBytes(200) },
            new BinaryEntity { Name = "a3.bin", Content = BitConverter.GetBytes(300) },
            new BinaryEntity { Name = "a4.bin", Content = BitConverter.GetBytes(400) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TotalValue", typeof(int?)),
            ("RecordCount", typeof(long)),
            ("AvgValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1000, 4L, 250]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithGroupBy_ShouldGroupByField()
    {
        // Arrange: Group by category and sum values
        var query = @"
            binary Transaction {
                Category: byte,
                Amount: int le
            };
            select
                t.Category,
                Sum(t.Amount) as TotalAmount,
                Count(t.Amount) as TransactionCount
            from #test.files() f
            cross apply Interpret<Transaction>(f.Content) t
            group by t.Category
            order by t.Category";

        var entities = new[]
        {
            // Category 1
            CreateTransactionEntity("t1.bin", 1, 100),
            CreateTransactionEntity("t2.bin", 1, 150),
            // Category 2
            CreateTransactionEntity("t3.bin", 2, 200),
            CreateTransactionEntity("t4.bin", 2, 250),
            CreateTransactionEntity("t5.bin", 2, 300),
            // Category 3
            CreateTransactionEntity("t6.bin", 3, 500)
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("t.Category", typeof(byte)),
            ("TotalAmount", typeof(int?)),
            ("TransactionCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)1, 250, 2L],
            [(byte)2, 750, 3L],
            [(byte)3, 500, 1L]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithHaving_ShouldFilterGroups()
    {
        // Arrange: Filter groups by aggregate value
        var query = @"
            binary Sale {
                Region: byte,
                Amount: int le
            };
            select
                s.Region,
                Sum(s.Amount) as TotalSales
            from #test.files() f
            cross apply Interpret<Sale>(f.Content) s
            group by s.Region
            having Sum(s.Amount) > 500
            order by Sum(s.Amount) desc";

        var entities = new[]
        {
            // Region 1: Total = 300 (excluded by HAVING)
            CreateTransactionEntity("s1.bin", 1, 100),
            CreateTransactionEntity("s2.bin", 1, 200),
            // Region 2: Total = 900
            CreateTransactionEntity("s3.bin", 2, 400),
            CreateTransactionEntity("s4.bin", 2, 500),
            // Region 3: Total = 600
            CreateTransactionEntity("s5.bin", 3, 250),
            CreateTransactionEntity("s6.bin", 3, 350)
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("s.Region", typeof(byte)),
            ("TotalSales", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)2, 900],
            [(byte)3, 600]);
    }

    private static BinaryEntity CreateTransactionEntity(string name, byte category, int amount)
    {
        var data = new byte[5];
        data[0] = category;
        BitConverter.GetBytes(amount).CopyTo(data, 1);
        return new BinaryEntity { Name = name, Content = data };
    }

    #endregion
}
