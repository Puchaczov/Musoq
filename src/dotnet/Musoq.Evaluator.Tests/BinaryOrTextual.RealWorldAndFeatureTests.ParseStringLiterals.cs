using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Parse With String Literal Tests

    [TestMethod]
    public void Query_ParseWithStringLiteral_ShouldWork()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.lines() f
            cross apply Parse<LogEntry>('[2026-03-09] INFO: booted') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2026-03-09", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("booted", table[0][2]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectConstant_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select
                1 as X
            from #test.lines() f
            cross apply Parse<KeyValue>('host=localhost') kv";

        var entities = new[] { new TextEntity { Name = "data.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectParsedFields_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select
                kv.Key,
                kv.Value
            from #test.lines() f
            cross apply Parse<KeyValue>('host=localhost') kv";

        var entities = new[] { new TextEntity { Name = "data.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_ShouldWork()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.lines() f
            outer apply TryParse<LogEntry>('[2026-03-09] INFO: booted') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2026-03-09", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("booted", table[0][2]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_WhenParsingFails_ShouldReturnNull()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp
            from #test.lines() f
            outer apply TryParse<LogEntry>('not a valid log entry') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion
}
