using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    #region TryInterpret/TryParse E2E Tests

    [TestMethod]
    public void Query_SelectTryInterpret_WithValidData_ShouldReturnResult()
    {
        // Arrange: TryInterpret with valid data
        var query = @"
            binary SimpleHeader {
                Magic: int le,
                Version: byte
            };
            select
                d.Magic,
                d.Version
            from #test.files() f
            cross apply TryInterpret<SimpleHeader>(f.Content) d";

        var testData = new byte[5];
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 0); // Magic
        testData[4] = 1; // Version
        var entities = new[] { new BinaryEntity { Name = "valid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]); // Magic
        Assert.AreEqual((byte)1, table[0][1]); // Version
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithInvalidData_ShouldReturnNullValues()
    {
        // Arrange: TryInterpret with insufficient data returns null for field values
        // Using OUTER APPLY, the row is preserved with null values for the parsed fields
        var query = @"
            binary SimpleValue {
                Value: int le
            };
            select
                f.Name,
                d.Value
            from #test.files() f
            outer apply TryInterpret<SimpleValue>(f.Content) d";

        // This data is invalid - only 2 bytes for a 4-byte int
        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert - Row is present but Value is null due to failed parse
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Value is null because TryInterpret failed
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_CrossApply_ExcludesFailedParses()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + CROSS APPLY, failed parses (null result) are excluded from results
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            cross apply TryInterpret<MagicHeader>(f.Content) d";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "valid.bin", Content = validData },
            new BinaryEntity { Name = "invalid.bin", Content = invalidData }
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

        // Assert - Only one row (valid.bin) since invalid.bin's TryInterpret returns null
        // which is filtered out by CROSS APPLY behavior
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("valid.bin", table[0][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0][1]);
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_OuterApply_IncludesAllRows()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + OUTER APPLY, failed parses return rows with null field values
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret<MagicHeader>(f.Content) d
            order by f.Name asc";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "invalid.bin", Content = invalidData },
            new BinaryEntity { Name = "valid.bin", Content = validData }
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

        // Assert - Both rows present, invalid.bin has null Magic value
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Magic is null for failed parse
        Assert.AreEqual("valid.bin", table[1][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[1][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with empty data returns null.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_WithEmptyData_ShouldReturnNull()
    {
        var query = @"
            binary Header {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret<Header>(f.Content) d";

        var emptyData = Array.Empty<byte>();
        var entities = new[] { new BinaryEntity { Name = "empty.bin", Content = emptyData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("empty.bin", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with all invalid data returns no rows with CROSS APPLY.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_AllInvalid_CrossApply_ReturnsNoRows()
    {
        var query = @"
            binary Header {
                Value: int le
            };
            select d.Value
            from #test.files() f
            cross apply TryInterpret<Header>(f.Content) d";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [0x01] },
            new BinaryEntity { Name = "b.bin", Content = [0x02, 0x03] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Session 5: Tests counting valid vs invalid parses with TryInterpret.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_CountValidAndInvalid_ShouldCountCorrectly()
    {
        var query = @"
            binary Header {
                Value: short le
            };
            select
                Count(d.Value) as ValidCount
            from #test.files() f
            outer apply TryInterpret<Header>(f.Content) d";

        var entities = new[]
        {
            new BinaryEntity { Name = "valid1.bin", Content = [0x01, 0x00] },
            new BinaryEntity { Name = "valid2.bin", Content = [0x02, 0x00] },
            new BinaryEntity { Name = "invalid.bin", Content = [0x01] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2L, table[0][0]);
    }

    #endregion
}
