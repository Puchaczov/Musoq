using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Complex Query Pattern Tests

    [TestMethod]
    public void Query_SelectInterpret_CteWithInterpret_ShouldWorkWithCommonTableExpression()
    {
        // Arrange: Use CTE with interpretation results
        var query = @"
            binary Header {
                Version: short le,
                Count: int le
            };
            with ParsedHeaders as (
                select
                    f.Name as FileName,
                    h.Version as FileVersion,
                    h.Count as FileCount
                from #test.files() f
                cross apply Interpret<Header>(f.Content) h
            )
            select
                FileName,
                FileVersion,
                FileCount
            from ParsedHeaders
            where FileCount > 50
            order by FileCount desc";

        var entities = new[]
        {
            new BinaryEntity { Name = "file1.bin", Content = CreateHeader(1, 100) },
            new BinaryEntity { Name = "file2.bin", Content = CreateHeader(2, 25) },
            new BinaryEntity { Name = "file3.bin", Content = CreateHeader(1, 75) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FileName", typeof(string)),
            ("FileVersion", typeof(short)),
            ("FileCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["file1.bin", (short)1, 100],
            ["file3.bin", (short)1, 75]);
    }

    private static byte[] CreateHeader(short version, int count)
    {
        var data = new byte[6];
        BitConverter.GetBytes(version).CopyTo(data, 0);
        BitConverter.GetBytes(count).CopyTo(data, 2);
        return data;
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDistinct_ShouldReturnUniqueRecords()
    {
        // Arrange: Use DISTINCT with interpretation results
        var query = @"
            binary Record {
                Category: int le,
                Value: int le
            };
            select distinct
                r.Category
            from #test.files() f
            cross apply Interpret<Record>(f.Content) r";

        // Create records with duplicate categories
        var entities = new[]
        {
            new BinaryEntity { Name = "rec1.bin", Content = CreateRecord(1, 100) },
            new BinaryEntity { Name = "rec2.bin", Content = CreateRecord(1, 200) },
            new BinaryEntity { Name = "rec3.bin", Content = CreateRecord(2, 300) },
            new BinaryEntity { Name = "rec4.bin", Content = CreateRecord(2, 400) },
            new BinaryEntity { Name = "rec5.bin", Content = CreateRecord(3, 500) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("r.Category", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2], [3]);
    }

    private static byte[] CreateRecord(int id, int value)
    {
        var data = new byte[8];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(value).CopyTo(data, 4);
        return data;
    }

    [TestMethod]
    public void Query_SelectInterpret_SelfJoin_ShouldCorrelate()
    {
        // Arrange: Use a CTE with self-join on materialized records
        // Single interpretation, then self-join the CTE
        var query = @"
            binary Record {
                Id: int le,
                ParentId: int le,
                Value: int le
            };
            with AllRecords as (
                select
                    r.Id as Id,
                    r.ParentId as ParentId,
                    r.Value as Value
                from #test.files() f
                cross apply Interpret<Record>(f.Content) r
            )
            select
                ch.Id as ChildId,
                ch.Value as ChildValue,
                pa.Id as ParentId,
                pa.Value as ParentValue
            from AllRecords ch inner join AllRecords pa on ch.ParentId = pa.Id
            order by ch.Id";

        // Create a hierarchy:
        // Record 1: Parent=0 (root), Value=100
        // Record 2: Parent=1, Value=200
        // Record 3: Parent=1, Value=300
        var entities = new[]
        {
            new BinaryEntity { Name = "rec1.bin", Content = CreateRecordWithParent(1, 0, 100) }, // root
            new BinaryEntity { Name = "rec2.bin", Content = CreateRecordWithParent(2, 1, 200) }, // child of 1
            new BinaryEntity { Name = "rec3.bin", Content = CreateRecordWithParent(3, 1, 300) } // child of 1
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ChildId", typeof(int)),
            ("ChildValue", typeof(int)),
            ("ParentId", typeof(int)),
            ("ParentValue", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [2, 200, 1, 100],
            [3, 300, 1, 100]);
    }

    private static byte[] CreateRecordWithParent(int id, int parentId, int value)
    {
        var data = new byte[12];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(parentId).CopyTo(data, 4);
        BitConverter.GetBytes(value).CopyTo(data, 8);
        return data;
    }

    #endregion
}
