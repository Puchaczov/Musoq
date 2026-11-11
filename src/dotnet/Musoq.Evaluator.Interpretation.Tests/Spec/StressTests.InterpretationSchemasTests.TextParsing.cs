using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsInterpretationSchemasTests
{
    #region Step 2: Text Pattern with Regex Capture

    /// <summary>
    ///     Tests text schema with pattern regex matching a simple digit sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternSimpleDigits_ShouldCapture()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Digits: pattern '\\d+',\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.Digits, d.Rest from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "12345abc" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("12345", table[0].ReadValue(0));
        Assert.AreEqual("abc", table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests text pattern with IP address regex.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternIpAddress_ShouldCapture()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Ip: pattern '\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}',\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.Ip from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "192.168.1.100 connected" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("192.168.1.100", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests text pattern with hex value capture.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternHexValue_ShouldCapture()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                _: pattern '0x',\n"
            + "                Hex: pattern '[0-9A-Fa-f]+',\n"
            + "                Rest: rest\n"
            + "            };\n"
            + "            select d.Hex, d.Rest from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "0xDEADBEEF end" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("DEADBEEF", table[0].ReadValue(0));
        Assert.AreEqual(" end", table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests text pattern combined with until and between.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternCombinedWithUntilAndBetween_ShouldParseAll()
    {
        var query = (
            "\n"
            + "            text LogLine {\n"
            + "                Timestamp: between '[' ']',\n"
            + "                _: pattern '\\s+',\n"
            + "                Level: pattern '[A-Z]+',\n"
            + "                _: pattern '\\s+',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select d.Timestamp, d.Level, d.Message from #test.lines() l\n"
            + "            cross apply Parse<LogLine>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "[2024-01-15] ERROR something failed" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15", table[0].ReadValue(0));
        Assert.AreEqual("ERROR", table[0].ReadValue(1));
        Assert.AreEqual("something failed", table[0].ReadValue(2));
    }

    /// <summary>
    ///     Tests pattern across multiple rows with filtering.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternMultipleRowsWithFilter_ShouldFilter()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Code: pattern '[A-Z]+',\n"
            + "                _: pattern '-',\n"
            + "                Number: rest\n"
            + "            };\n"
            + "            select d.Code, d.Number from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d\n"
            + "            where d.Code = 'ERR'"
        );

        var entities = new[]
        {
            new TextEntity { Name = "l1", Text = "ERR-404" },
            new TextEntity { Name = "l2", Text = "OK-200" },
            new TextEntity { Name = "l3", Text = "ERR-500" },
            new TextEntity { Name = "l4", Text = "WARN-301" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Step 3: Text Literal, Token, and Whitespace

    /// <summary>
    ///     Tests literal matching as a discard between named fields.
    ///     Note: 'until' consumes the delimiter, so literal only needs to match what follows.
    /// </summary>
    [TestMethod]
    public void Stress_Text_LiteralAsDiscard_ShouldAdvanceCursor()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Key: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select d.Key, d.Value from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "host: localhost" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0].ReadValue(0));
        Assert.AreEqual("localhost", table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests literal with multi-character sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Text_LiteralMultiChar_ShouldMatch()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                _: literal '---',\n"
            + "                Content: rest\n"
            + "            };\n"
            + "            select d.Content from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "---Hello World" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello World", table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests token capture (whitespace-delimited).
    /// </summary>
    [TestMethod]
    public void Stress_Text_Token_ShouldCaptureNonWhitespace()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                First: token,\n"
            + "                _: whitespace,\n"
            + "                Second: token,\n"
            + "                _: whitespace,\n"
            + "                Third: rest\n"
            + "            };\n"
            + "            select d.First, d.Second, d.Third from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "hello world remainder" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0].ReadValue(0));
        Assert.AreEqual("world", table[0].ReadValue(1));
        Assert.AreEqual("remainder", table[0].ReadValue(2));
    }

    /// <summary>
    ///     Tests whitespace* (zero or more) with no whitespace present.
    /// </summary>
    [TestMethod]
    public void Stress_Text_WhitespaceStar_NoWhitespace_ShouldSucceed()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                A: until ',',\n"
            + "                _: whitespace*,\n"
            + "                B: rest\n"
            + "            };\n"
            + "            select d.A, d.B from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "hello,world" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0].ReadValue(0));
        Assert.AreEqual("world", table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests whitespace* with multiple spaces and tabs.
    /// </summary>
    [TestMethod]
    public void Stress_Text_WhitespaceStar_MultipleSpacesAndTabs_ShouldConsume()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                A: until ':',\n"
            + "                _: whitespace*,\n"
            + "                B: rest\n"
            + "            };\n"
            + "            select d.A, d.B from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "key:   \t  value" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0].ReadValue(0));
        Assert.AreEqual("value", table[0].ReadValue(1));
    }

    #endregion

    #region Step 4: Text Optional Fields

    /// <summary>
    ///     Tests optional field that is present.
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalPresent_ShouldCapture()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Name: until '=',\n"
            + "                Value: until ';',\n"
            + "                Extra: optional rest\n"
            + "            };\n"
            + "            select d.Name, d.Value, d.Extra from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "key=value;extra info" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0].ReadValue(0));
        Assert.AreEqual("value", table[0].ReadValue(1));
        Assert.AreEqual("extra info", table[0].ReadValue(2));
    }

    /// <summary>
    ///     Tests optional field that is absent (at end of input).
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalAbsent_ShouldReturnNull()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                Name: until ':',\n"
            + "                Value: rest,\n"
            + "                TraceId: optional pattern '[a-f0-9]{8}'\n"
            + "            };\n"
            + "            select d.Name, d.Value, d.TraceId from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "key:value" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0].ReadValue(0));
        Assert.AreEqual("value", table[0].ReadValue(1));
        Assert.IsNull(table[0].ReadValue(2));
    }

    /// <summary>
    ///     Tests optional literal that is absent doesn't consume input.
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalLiteralAbsent_ShouldNotConsume()
    {
        var query = (
            "\n"
            + "            text Data {\n"
            + "                A: until ',',\n"
            + "                _: optional literal ' - ',\n"
            + "                B: rest\n"
            + "            };\n"
            + "            select d.A, d.B from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        // No ' - ' separator, so optional should fail silently and B gets rest
        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "hello,world" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0].ReadValue(0));
        Assert.AreEqual("world", table[0].ReadValue(1));
    }

    #endregion
}
