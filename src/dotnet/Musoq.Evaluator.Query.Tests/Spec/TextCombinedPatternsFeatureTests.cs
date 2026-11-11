using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Text Schema combined field patterns (Sections 5.1-5.4).
///     Tests combinations of until, between, chars, and rest in a single schema.
/// </summary>
[TestClass]
public class TextCombinedPatternsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Until + Rest Combination

    /// <summary>
    ///     Tests text schema with until field followed by rest to capture remainder.
    /// </summary>
    [TestMethod]
    public void Text_UntilAndRest_ShouldSplitAtDelimiter()
    {
        var query = (
            "\n"
            + "            text Data { \n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select d.Key, d.Value from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "name=John Doe" },
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
        Assert.AreEqual("name", table[0].ReadValue(0));
        Assert.AreEqual("John Doe", table[0].ReadValue(1));
    }

    #endregion

    #region Between + Rest Combination

    /// <summary>
    ///     Tests text schema with between field followed by rest.
    /// </summary>
    [TestMethod]
    public void Text_BetweenAndRest_ShouldExtractAndContinue()
    {
        var query = (
            "\n"
            + "            text Data { \n"
            + "                Tag: between '[' ']',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select d.Tag, d.Message from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "[ERROR] Something went wrong" },
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
        Assert.AreEqual("ERROR", table[0].ReadValue(0));
        Assert.AreEqual(" Something went wrong", table[0].ReadValue(1));
    }

    #endregion

    #region Chars + Until + Rest Combination

    /// <summary>
    ///     Tests text schema with chars, until, and rest combined for structured parsing.
    /// </summary>
    [TestMethod]
    public void Text_CharsUntilRest_ShouldParseStructuredLine()
    {
        var query = (
            "\n"
            + "            text LogEntry { \n"
            + "                Level: chars[4],\n"
            + "                _: literal ' ',\n"
            + "                Timestamp: until ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select d.Level, d.Timestamp, d.Message from #test.lines() l\n"
            + "            cross apply Parse<LogEntry>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "INFO 2024-01-15 Application started successfully" },
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
        Assert.AreEqual("INFO", table[0].ReadValue(0));
        Assert.AreEqual("2024-01-15", table[0].ReadValue(1));
        Assert.AreEqual("Application started successfully", table[0].ReadValue(2));
    }

    #endregion

    #region Multiple Until Fields

    /// <summary>
    ///     Tests text schema with multiple until fields using different delimiters.
    /// </summary>
    [TestMethod]
    public void Text_MultipleUntil_ShouldParseEachSegment()
    {
        var query = (
            "\n"
            + "            text CsvRow { \n"
            + "                Col1: until ',',\n"
            + "                Col2: until ',',\n"
            + "                Col3: rest\n"
            + "            };\n"
            + "            select d.Col1, d.Col2, d.Col3 from #test.lines() l\n"
            + "            cross apply Parse<CsvRow>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "Alice,30,Engineer" },
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
        Assert.AreEqual("Alice", table[0].ReadValue(0));
        Assert.AreEqual("30", table[0].ReadValue(1));
        Assert.AreEqual("Engineer", table[0].ReadValue(2));
    }

    #endregion

    #region Multiple Between Fields

    /// <summary>
    ///     Tests text schema with multiple between fields using different delimiters.
    /// </summary>
    [TestMethod]
    public void Text_MultipleBetween_ShouldExtractEach()
    {
        var query = (
            "\n"
            + "            text Data { \n"
            + "                First: between '(' ')',\n"
            + "                Second: between '[' ']'\n"
            + "            };\n"
            + "            select d.First, d.Second from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "(hello)[world]" },
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

    #region Chars + Between + Chars

    /// <summary>
    ///     Tests text schema with chars followed by between followed by chars.
    /// </summary>
    [TestMethod]
    public void Text_CharsBetweenChars_ShouldParseFixedAndDelimited()
    {
        var query = (
            "\n"
            + "            text Data { \n"
            + "                Prefix: chars[3],\n"
            + "                Value: between '<' '>',\n"
            + "                Suffix: chars[3]\n"
            + "            };\n"
            + "            select d.Prefix, d.Value, d.Suffix from #test.lines() l\n"
            + "            cross apply Parse<Data>(l.Line) d"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "ABC<data>XYZ" },
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
        Assert.AreEqual("ABC", table[0].ReadValue(0));
        Assert.AreEqual("data", table[0].ReadValue(1));
        Assert.AreEqual("XYZ", table[0].ReadValue(2));
    }

    #endregion

    #region Complex Key-Value Parsing

    /// <summary>
    ///     Tests parsing a key-value pair with colon separator and surrounding brackets.
    /// </summary>
    [TestMethod]
    public void Text_ComplexKeyValue_ShouldParseStructuredFormat()
    {
        var query = (
            "\n"
            + "            text KeyValue { \n"
            + "                Key: until ':',\n"
            + "                _: literal ' ',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select d.Key, d.Value from #test.lines() l\n"
            + "            cross apply Parse<KeyValue>(l.Line) d\n"
            + "            order by d.Key asc"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1.txt", Text = "Host: example.com" },
            new TextEntity { Name = "line2.txt", Text = "Port: 8080" },
            new TextEntity { Name = "line3.txt", Text = "Path: /api/v1" },
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Host", table[0].ReadValue(0));
        Assert.AreEqual("example.com", table[0].ReadValue(1));
        Assert.AreEqual("Path", table[1].ReadValue(0));
        Assert.AreEqual("/api/v1", table[1].ReadValue(1));
        Assert.AreEqual("Port", table[2].ReadValue(0));
        Assert.AreEqual("8080", table[2].ReadValue(1));
    }

    #endregion

    #region Text Parsing with WHERE filter

    /// <summary>
    ///     Tests parsing multiple text lines and filtering with WHERE clause.
    /// </summary>
    [TestMethod]
    public void Text_WithWhereFilter_ShouldFilterParsedResults()
    {
        var query = (
            "\n"
            + "            text LogLine { \n"
            + "                Level: until ' ',\n"
            + "                Message: rest\n"
            + "            };\n"
            + "            select l2.Message from #test.lines() l\n"
            + "            cross apply Parse<LogLine>(l.Line) l2\n"
            + "            where l2.Level = 'ERROR'\n"
            + "            order by l2.Message asc"
        );

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "INFO Application started" },
            new TextEntity { Name = "2.txt", Text = "ERROR Connection failed" },
            new TextEntity { Name = "3.txt", Text = "WARN Low memory" },
            new TextEntity { Name = "4.txt", Text = "ERROR Timeout reached" },
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
        Assert.AreEqual("Connection failed", table[0].ReadValue(0));
        Assert.AreEqual("Timeout reached", table[1].ReadValue(0));
    }

    #endregion

    #region Text Parsing with Multiple Lines and ORDER BY

    /// <summary>
    ///     Tests parsing multiple text lines with ordering by parsed field.
    /// </summary>
    [TestMethod]
    public void Text_WithOrderBy_ShouldSortByParsedField()
    {
        var query = (
            "\n"
            + "            text Entry { \n"
            + "                Priority: until ':',\n"
            + "                Name: rest\n"
            + "            };\n"
            + "            select e.Priority, e.Name from #test.lines() l\n"
            + "            cross apply Parse<Entry>(l.Line) e\n"
            + "            order by e.Priority asc"
        );

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "C:Charlie" },
            new TextEntity { Name = "2.txt", Text = "A:Alpha" },
            new TextEntity { Name = "3.txt", Text = "B:Bravo" },
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("A", table[0].ReadValue(0));
        Assert.AreEqual("Alpha", table[0].ReadValue(1));
        Assert.AreEqual("B", table[1].ReadValue(0));
        Assert.AreEqual("Bravo", table[1].ReadValue(1));
        Assert.AreEqual("C", table[2].ReadValue(0));
        Assert.AreEqual("Charlie", table[2].ReadValue(1));
    }

    #endregion
}
