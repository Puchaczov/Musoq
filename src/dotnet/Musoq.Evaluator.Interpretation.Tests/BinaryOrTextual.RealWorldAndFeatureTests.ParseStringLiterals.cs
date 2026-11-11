using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Parse With String Literal Tests

    [TestMethod]
    public void Query_ParseWithStringLiteral_ShouldWork()
    {
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Timestamp: between '[' ']',\n"
            + "                _: literal ' ',\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select\n"
            + "                log.Timestamp,\n"
            + "                log.Level,\n"
            + "                log.Message\n"
            + "            from #test.lines() f\n"
            + "            cross apply Parse<LogEntry>('[2026-03-09] INFO: booted') log"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Timestamp", typeof(string)),
            ("Level", typeof(string)),
            ("Message", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2026-03-09", "INFO", "booted"]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectConstant_ShouldWork()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                1 as X\n"
            + "            from #test.lines() f\n"
            + "            cross apply Parse<KeyValue>('host=localhost') kv"
        );

        var entities = new[]
        {
            new TextEntity { Name = "data.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("X", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectParsedFields_ShouldWork()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                kv.Key,\n"
            + "                kv.Value\n"
            + "            from #test.lines() f\n"
            + "            cross apply Parse<KeyValue>('host=localhost') kv"
        );

        var entities = new[]
        {
            new TextEntity { Name = "data.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Key", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["host", "localhost"]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_ShouldWork()
    {
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Timestamp: between '[' ']',\n"
            + "                _: literal ' ',\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select\n"
            + "                log.Timestamp,\n"
            + "                log.Level,\n"
            + "                log.Message\n"
            + "            from #test.lines() f\n"
            + "            outer apply TryParse<LogEntry>('[2026-03-09] INFO: booted') log"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Timestamp", typeof(string)),
            ("Level", typeof(string)),
            ("Message", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2026-03-09", "INFO", "booted"]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_WhenParsingFails_ShouldReturnNull()
    {
        var query = (
            "\n"
            + "            text LogEntry {\n"
            + "                Timestamp: between '[' ']',\n"
            + "                _: literal ' ',\n"
            + "                Level: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select\n"
            + "                log.Timestamp\n"
            + "            from #test.lines() f\n"
            + "            outer apply TryParse<LogEntry>('not a valid log entry') log"
        );

        var entities = new[]
        {
            new TextEntity { Name = "log.txt", Text = "dummy" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Timestamp", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    #endregion
}
