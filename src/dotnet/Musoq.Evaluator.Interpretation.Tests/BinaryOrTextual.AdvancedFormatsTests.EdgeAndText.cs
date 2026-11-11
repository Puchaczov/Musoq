using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Edge Case Tests

    [TestMethod]
    public void Query_SelectInterpret_WithEmptyByteArray_ShouldParseEmpty()
    {
        // Arrange: Zero-length byte array
        var query = (
            "\n"
            + "            binary EmptyArrayData {\n"
            + "                Count: byte,\n"
            + "                Data: byte[Count]\n"
            + "            };\n"
            + "            select\n"
            + "                h.Count,\n"
            + "                h.Data\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<EmptyArrayData>(f.Content) h"
        );

        // Count=0, no data bytes
        var testData = new byte[] { 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(table, ("Count", typeof(byte)), ("Data", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)0, Array.Empty<byte>()]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMaxValues_ShouldHandleCorrectly()
    {
        // Arrange: Maximum values for various types
        var query = (
            "\n"
            + "            binary MaxValues {\n"
            + "                MaxByte: byte,\n"
            + "                MaxShort: short le,\n"
            + "                MaxInt: int le\n"
            + "            };\n"
            + "            select\n"
            + "                h.MaxByte,\n"
            + "                h.MaxShort,\n"
            + "                h.MaxInt\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<MaxValues>(f.Content) h"
        );

        // Max values: byte=255, short=32767, int=2147483647
        var testData = new byte[]
        {
            0xFF, // byte max
            0xFF,
            0x7F, // short max (little-endian)
            0xFF,
            0xFF,
            0xFF,
            0x7F, // int max (little-endian)
        };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("MaxByte", typeof(byte)),
            ("MaxShort", typeof(short)),
            ("MaxInt", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)255, (short)32767, 2147483647]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMinValues_ShouldHandleCorrectly()
    {
        // Arrange: Minimum values for signed types
        var query = (
            "\n"
            + "            binary MinValues {\n"
            + "                MinSByte: sbyte,\n"
            + "                MinShort: short le,\n"
            + "                MinInt: int le\n"
            + "            };\n"
            + "            select\n"
            + "                h.MinSByte,\n"
            + "                h.MinShort,\n"
            + "                h.MinInt\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<MinValues>(f.Content) h"
        );

        // Min values: sbyte=-128, short=-32768, int=-2147483648
        var testData = new byte[]
        {
            0x80, // sbyte min (-128)
            0x00,
            0x80, // short min (-32768)
            0x00,
            0x00,
            0x00,
            0x80, // int min (-2147483648)
        };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("MinSByte", typeof(sbyte)),
            ("MinShort", typeof(short)),
            ("MinInt", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(sbyte)-128, (short)-32768, -2147483648]);
    }

    #endregion

    #region Text Schema Query Tests

    [TestMethod]
    public void Query_SelectParse_WithTextSchema_ShouldParseData()
    {
        // Arrange
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select\n"
            + "                p.Level,\n"
            + "                p.Message\n"
            + "            from #test.logs() f\n"
            + "            cross apply Parse<LogEntry>(f.Text) p\n"
            + "            order by p.Level"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log1", Text = "INFO: Application started" },
            new TextEntity { Name = "log2", Text = "ERROR: Failed to connect" },
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        TableMaterializationTestHelper.AssertColumns(table, ("Level", typeof(string)), ("Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["ERROR", "Failed to connect"],
            ["INFO", "Application started"]
        );
    }

    [TestMethod]
    public void Query_SelectParse_WithCsvSchema_ShouldParseDelimitedData()
    {
        // Arrange
        var query = (
            "\n"
            + "            text CsvRow {\n"
            + "                Name: until ',',\n"
            + "                Age: until ',',\n"
            + "                City: rest\n"
            + "            };\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                p.Age,\n"
            + "                p.City\n"
            + "            from #test.csv() f\n"
            + "            cross apply Parse<CsvRow>(f.Line) p\n"
            + "            order by p.Name"
        );

        var entities = new[]
        {
            new TextEntity { Name = "row1", Text = "John Doe,30,New York" },
            new TextEntity { Name = "row2", Text = "Jane Smith,25,Los Angeles" },
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert - ordered by Name (Jane before John alphabetically)
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Age", typeof(string)),
            ("City", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Jane Smith", "25", "Los Angeles"],
            ["John Doe", "30", "New York"]
        );
    }

    #endregion
}
