using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Advanced Text Schema Tests

    [TestMethod]
    public void Query_SelectParse_KeyValueConfig_WithTrim_ShouldParse()
    {
        // Arrange: Simple key=value configuration parsing
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest trim
            };
            select
                kv.Key,
                kv.Value
            from #test.lines() l
            cross apply Parse<KeyValue>(l.Text) kv
            order by kv.Key";

        var entities = new[]
        {
            new TextEntity { Name = "config.txt", Text = "host=localhost" },
            new TextEntity { Name = "config.txt", Text = "port=8080" },
            new TextEntity { Name = "config.txt", Text = "debug=true" }
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
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("debug", table[0][0]);
        Assert.AreEqual("true", table[0][1]);
        Assert.AreEqual("host", table[1][0]);
        Assert.AreEqual("localhost", table[1][1]);
        Assert.AreEqual("port", table[2][0]);
        Assert.AreEqual("8080", table[2][1]);
    }

    [TestMethod]
    public void Query_SelectParse_CsvLikeFormat_MultipleFields_ShouldParse()
    {
        // Arrange: CSV-like format with multiple delimiters
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text CsvRecord {
                Id: until ',',
                Name: until ',',
                Amount: rest trim
            };
            select
                r.Id,
                r.Name,
                r.Amount
            from #test.lines() l
            cross apply Parse<CsvRecord>(l.Text) r
            order by r.Id";

        var entities = new[]
        {
            new TextEntity { Name = "data.csv", Text = "001,Product A,100.50" },
            new TextEntity { Name = "data.csv", Text = "002,Product B,250.00" }
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
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("001", table[0][0]);
        Assert.AreEqual("Product A", table[0][1]);
        Assert.AreEqual("100.50", table[0][2]);
        Assert.AreEqual("002", table[1][0]);
        Assert.AreEqual("Product B", table[1][1]);
        Assert.AreEqual("250.00", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_BracketedTimestamp_Between_ShouldExtract()
    {
        // Arrange: Log format with bracketed timestamp
        // Note: 'until' consumes the delimiter, 'between' consumes both brackets
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ' ',
                Message: rest
            };
            select
                e.Timestamp,
                e.Level,
                e.Message
            from #test.lines() l
            cross apply Parse<LogEntry>(l.Text) e
            order by e.Timestamp";

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:00] INFO Application started" },
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:05] ERROR Connection failed" }
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
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("2024-01-01 10:00:00", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("Application started", table[0][2]);
        Assert.AreEqual("2024-01-01 10:00:05", table[1][0]);
        Assert.AreEqual("ERROR", table[1][1]);
        Assert.AreEqual("Connection failed", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_FixedWidthRecord_WithChars_ShouldParse()
    {
        // Arrange: Fixed-width record (COBOL-style)
        var query = @"
            text FixedRecord {
                Id: chars[5],
                Name: chars[20] trim,
                Amount: chars[10] trim
            };
            select
                r.Id,
                r.Name,
                r.Amount
            from #test.lines() l
            cross apply Parse<FixedRecord>(l.Text) r
            order by r.Id";

        var entities = new[]
        {
            new TextEntity { Name = "data.dat", Text = "00001John Smith          0000100.50" },
            new TextEntity { Name = "data.dat", Text = "00002Jane Doe            0000250.00" }
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
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("00001", table[0][0]);
        Assert.AreEqual("John Smith", table[0][1]);
        Assert.AreEqual("0000100.50", table[0][2]);
        Assert.AreEqual("00002", table[1][0]);
        Assert.AreEqual("Jane Doe", table[1][1]);
        Assert.AreEqual("0000250.00", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_PatternMatching_ShouldExtractTokens()
    {
        // Arrange: Extract specific patterns from text
        // Note: 'pattern' does NOT consume any delimiter, so we use 'until' to get rest
        var query = @"
            text StatusLine {
                Code: until ' ',
                Status: rest
            };
            select
                s.Code,
                s.Status
            from #test.lines() l
            cross apply Parse<StatusLine>(l.Text) s
            order by s.Code";

        var entities = new[]
        {
            new TextEntity { Name = "responses.txt", Text = "200 OK" },
            new TextEntity { Name = "responses.txt", Text = "404 Not Found" },
            new TextEntity { Name = "responses.txt", Text = "500 Internal Server Error" }
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
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("200", table[0][0]);
        Assert.AreEqual("OK", table[0][1]);
        Assert.AreEqual("404", table[1][0]);
        Assert.AreEqual("Not Found", table[1][1]);
        Assert.AreEqual("500", table[2][0]);
        Assert.AreEqual("Internal Server Error", table[2][1]);
    }

    #endregion
}
