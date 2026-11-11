using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Advanced Text Schema Tests

    [TestMethod]
    public void Query_SelectParse_KeyValueConfig_WithTrim_ShouldParse()
    {
        // Arrange: Simple key=value configuration parsing
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                kv.Key,\n"
            + "                kv.Value\n"
            + "            from #test.lines() l\n"
            + "            cross apply Parse<KeyValue>(l.Text) kv\n"
            + "            order by kv.Key"
        );

        var entities = new[]
        {
            new TextEntity { Name = "config.txt", Text = "host=localhost" },
            new TextEntity { Name = "config.txt", Text = "port=8080" },
            new TextEntity { Name = "config.txt", Text = "debug=true" },
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
        TableMaterializationTestHelper.AssertColumns(table, ("Key", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["debug", "true"],
            ["host", "localhost"],
            ["port", "8080"]
        );
    }

    [TestMethod]
    public void Query_SelectParse_CsvLikeFormat_MultipleFields_ShouldParse()
    {
        // Arrange: CSV-like format with multiple delimiters
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = (
            "\n"
            + "            text CsvRecord {\n"
            + "                Id: until ',',\n"
            + "                Name: until ',',\n"
            + "                Amount: rest trim\n"
            + "            };\n"
            + "            select\n"
            + "                r.Id,\n"
            + "                r.Name,\n"
            + "                r.Amount\n"
            + "            from #test.lines() l\n"
            + "            cross apply Parse<CsvRecord>(l.Text) r\n"
            + "            order by r.Id"
        );

        var entities = new[]
        {
            new TextEntity { Name = "data.csv", Text = "001,Product A,100.50" },
            new TextEntity { Name = "data.csv", Text = "002,Product B,250.00" },
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(string)),
            ("Name", typeof(string)),
            ("Amount", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["001", "Product A", "100.50"],
            ["002", "Product B", "250.00"]
        );
    }

    [TestMethod]
    public void Query_SelectParse_BracketedTimestamp_Between_ShouldExtract()
    {
        // Arrange: Log format with bracketed timestamp
        // Note: 'until' consumes the delimiter, 'between' consumes both brackets
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Timestamp: between '[' ']',\n"
            + "                _: literal ' ',\n"
            + "                Level: until ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select\n"
            + "                e.Timestamp,\n"
            + "                e.Level,\n"
            + "                e.Message\n"
            + "            from #test.lines() l\n"
            + "            cross apply Parse<LogEntry>(l.Text) e\n"
            + "            order by e.Timestamp"
        );

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:00] INFO Application started" },
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:05] ERROR Connection failed" },
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Timestamp", typeof(string)),
            ("Level", typeof(string)),
            ("Message", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["2024-01-01 10:00:00", "INFO", "Application started"],
            ["2024-01-01 10:00:05", "ERROR", "Connection failed"]
        );
    }

    [TestMethod]
    public void Query_SelectParse_FixedWidthRecord_WithChars_ShouldParse()
    {
        // Arrange: Fixed-width record (COBOL-style)
        var query = (
            "\n"
            + "            text FixedRecord {\n"
            + "                Id: chars[5],\n"
            + "                Name: chars[20] trim,\n"
            + "                Amount: chars[10] trim\n"
            + "            };\n"
            + "            select\n"
            + "                r.Id,\n"
            + "                r.Name,\n"
            + "                r.Amount\n"
            + "            from #test.lines() l\n"
            + "            cross apply Parse<FixedRecord>(l.Text) r\n"
            + "            order by r.Id"
        );

        var entities = new[]
        {
            new TextEntity { Name = "data.dat", Text = "00001John Smith          0000100.50" },
            new TextEntity { Name = "data.dat", Text = "00002Jane Doe            0000250.00" },
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(string)),
            ("Name", typeof(string)),
            ("Amount", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["00001", "John Smith", "0000100.50"],
            ["00002", "Jane Doe", "0000250.00"]
        );
    }

    [TestMethod]
    public void Query_SelectParse_PatternMatching_ShouldExtractTokens()
    {
        // Arrange: Extract specific patterns from text
        // Note: 'pattern' does NOT consume any delimiter, so we use 'until' to get rest
        var query = (
            "\n"
            + "            text StatusLine {\n"
            + "                Code: until ' ',\n"
            + "                Status: rest\n"
            + "            };\n"
            + "            select\n"
            + "                s.Code,\n"
            + "                s.Status\n"
            + "            from #test.lines() l\n"
            + "            cross apply Parse<StatusLine>(l.Text) s\n"
            + "            order by s.Code"
        );

        var entities = new[]
        {
            new TextEntity { Name = "responses.txt", Text = "200 OK" },
            new TextEntity { Name = "responses.txt", Text = "404 Not Found" },
            new TextEntity { Name = "responses.txt", Text = "500 Internal Server Error" },
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
        TableMaterializationTestHelper.AssertColumns(table, ("Code", typeof(string)), ("Status", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["200", "OK"],
            ["404", "Not Found"],
            ["500", "Internal Server Error"]
        );
    }

    #endregion
}
