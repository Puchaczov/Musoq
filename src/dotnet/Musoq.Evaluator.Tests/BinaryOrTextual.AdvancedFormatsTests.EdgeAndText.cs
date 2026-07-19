using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Edge Case Tests

    [TestMethod]
    public void Query_SelectInterpret_WithEmptyByteArray_ShouldParseEmpty()
    {
        // Arrange: Zero-length byte array
        var query = @"
            binary EmptyArrayData {
                Count: byte,
                Data: byte[Count]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret<EmptyArrayData>(f.Content) h";

        // Count=0, no data bytes
        var testData = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.Count", typeof(byte)),
            ("h.Data", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)0, Array.Empty<byte>()]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMaxValues_ShouldHandleCorrectly()
    {
        // Arrange: Maximum values for various types
        var query = @"
            binary MaxValues {
                MaxByte: byte,
                MaxShort: short le,
                MaxInt: int le
            };
            select
                h.MaxByte,
                h.MaxShort,
                h.MaxInt
            from #test.files() f
            cross apply Interpret<MaxValues>(f.Content) h";

        // Max values: byte=255, short=32767, int=2147483647
        var testData = new byte[]
        {
            0xFF, // byte max
            0xFF, 0x7F, // short max (little-endian)
            0xFF, 0xFF, 0xFF, 0x7F // int max (little-endian)
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.MaxByte", typeof(byte)),
            ("h.MaxShort", typeof(short)),
            ("h.MaxInt", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)255, (short)32767, 2147483647]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMinValues_ShouldHandleCorrectly()
    {
        // Arrange: Minimum values for signed types
        var query = @"
            binary MinValues {
                MinSByte: sbyte,
                MinShort: short le,
                MinInt: int le
            };
            select
                h.MinSByte,
                h.MinShort,
                h.MinInt
            from #test.files() f
            cross apply Interpret<MinValues>(f.Content) h";

        // Min values: sbyte=-128, short=-32768, int=-2147483648
        var testData = new byte[]
        {
            0x80, // sbyte min (-128)
            0x00, 0x80, // short min (-32768)
            0x00, 0x00, 0x00, 0x80 // int min (-2147483648)
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.MinSByte", typeof(sbyte)),
            ("h.MinShort", typeof(short)),
            ("h.MinInt", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(sbyte)-128, (short)-32768, -2147483648]);
    }

    #endregion

    #region Text Schema Query Tests

    [TestMethod]
    public void Query_SelectParse_WithTextSchema_ShouldParseData()
    {
        // Arrange
        var query = @"
            text LogEntry {
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                p.Level,
                p.Message
            from #test.logs() f
            cross apply Parse<LogEntry>(f.Text) p
            order by p.Level";

        var entities = new[]
        {
            new TextEntity { Name = "log1", Text = "INFO: Application started" },
            new TextEntity { Name = "log2", Text = "ERROR: Failed to connect" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Level", typeof(string)),
            ("p.Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["ERROR", "Failed to connect"],
            ["INFO", "Application started"]);
    }

    [TestMethod]
    public void Query_SelectParse_WithCsvSchema_ShouldParseDelimitedData()
    {
        // Arrange
        var query = @"
            text CsvRow {
                Name: until ',',
                Age: until ',',
                City: rest
            };
            select
                p.Name,
                p.Age,
                p.City
            from #test.csv() f
            cross apply Parse<CsvRow>(f.Line) p
            order by p.Name";

        var entities = new[]
        {
            new TextEntity { Name = "row1", Text = "John Doe,30,New York" },
            new TextEntity { Name = "row2", Text = "Jane Smith,25,Los Angeles" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert - ordered by Name (Jane before John alphabetically)
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)),
            ("p.Age", typeof(string)),
            ("p.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Jane Smith", "25", "Los Angeles"],
            ["John Doe", "30", "New York"]);
    }

    #endregion
}
