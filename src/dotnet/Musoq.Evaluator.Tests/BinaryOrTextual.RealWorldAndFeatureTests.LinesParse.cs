using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Lines Function With Parse Tests

    /// <summary>
    ///     Tests the Lines() + Parse() pattern: split multi-line content then parse each line.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_ShouldSplitAndParseEachLine()
    {
        var query = @"
            text LogEntry {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse<LogEntry>(line.Value) log";

        var multiLineContent = string.Join("\n",
            "2024-01-15T10:30:00 INFO Application started",
            "2024-01-15T10:30:01 WARN Low memory detected",
            "2024-01-15T10:30:02 ERROR Connection failed");

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = multiLineContent }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("log.Timestamp", typeof(string)),
            ("log.Level", typeof(string)),
            ("log.Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["2024-01-15T10:30:00", "INFO", "Application started"],
            ["2024-01-15T10:30:01", "WARN", "Low memory detected"],
            ["2024-01-15T10:30:02", "ERROR", "Connection failed"]);
    }

    /// <summary>
    ///     Tests Lines() + Parse() with Windows-style line endings.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_WindowsNewlines_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest trim
            };
            select
                kv.Key,
                kv.Value
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse<KeyValue>(line.Value) kv
            order by kv.Key asc";

        var multiLineContent = "host=localhost\r\nport=8080\r\ndebug=true";

        var entities = new[]
        {
            new TextEntity { Name = "config.ini", Text = multiLineContent }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("kv.Key", typeof(string)),
            ("kv.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["debug", "true"],
            ["host", "localhost"],
            ["port", "8080"]);
    }

    /// <summary>
    ///     Tests that using old Parse(data, 'SchemaName') syntax produces a clear error
    ///     directing the user to the new Parse&lt;SchemaName&gt;(data) syntax.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_OldSyntax_ShouldFailWithClearError()
    {
        var query = @"
            text LogEntry {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse(line.Value, 'LogEntry') log";

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "2024-01-15 INFO Started" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        Assert.IsTrue(
            innerMessage.Contains("no longer supported", StringComparison.OrdinalIgnoreCase),
            $"Expected error about old syntax being unsupported, got: {innerMessage}");
        Assert.IsTrue(
            innerMessage.Contains("Parse<LogEntry>", StringComparison.Ordinal),
            $"Expected error to suggest new syntax 'Parse<LogEntry>', got: {innerMessage}");
    }

    #endregion
}
