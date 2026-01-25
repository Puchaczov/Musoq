using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Text Schema 'rest' Capture (Section 5.7 of specification).
///     Tests capturing all remaining input.
/// </summary>
[TestClass]
public class TextRestFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 5.7: Rest with WHERE Clause

    /// <summary>
    ///     Tests filtering by rest content.
    /// </summary>
    [TestMethod]
    public void Text_Rest_InWhereClause_ShouldFilter()
    {
        var query = @"
            text Record { 
                Type: chars[3],
                Data: rest trim
            };
            select r.Type, r.Data from #test.lines() l
            cross apply Parse(l.Line, 'Record') r
            where r.Data like '%error%'";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "LOG An error occurred" },
            new TextEntity { Name = "line2", Text = "LOG Normal operation" },
            new TextEntity { Name = "line3", Text = "ERR Another error here" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Section 5.7: Basic Rest

    /// <summary>
    ///     Tests basic rest capturing all input.
    /// </summary>
    [TestMethod]
    public void Text_Rest_CaptureAll_ShouldReturnEntireInput()
    {
        var query = @"
            text Data { 
                Content: rest
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "This is the entire content" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("This is the entire content", table[0][0]);
    }

    /// <summary>
    ///     Tests rest after chars field.
    /// </summary>
    [TestMethod]
    public void Text_Rest_AfterChars_ShouldCaptureRemaining()
    {
        var query = @"
            text Data { 
                Prefix: chars[5],
                Remainder: rest
            };
            select d.Prefix, d.Remainder from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "HelloWorld123" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
        Assert.AreEqual("World123", table[0][1]);
    }

    /// <summary>
    ///     Tests rest after until field.
    /// </summary>
    [TestMethod]
    public void Text_Rest_AfterUntil_ShouldCaptureRemaining()
    {
        var query = @"
            text Data { 
                Field1: until ':',
                Field2: rest
            };
            select d.Field1, d.Field2 from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "key:value and more" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value and more", table[0][1]);
    }

    #endregion

    #region Section 5.7: Rest with Trim Modifier

    /// <summary>
    ///     Tests rest with trim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Rest_WithTrim_ShouldRemoveWhitespace()
    {
        var query = @"
            text Data { 
                Content: rest trim
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "  trimmed content  " } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("trimmed content", table[0][0]);
    }

    /// <summary>
    ///     Tests rest with rtrim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Rest_WithRtrim_ShouldRemoveTrailingOnly()
    {
        var query = @"
            text Data { 
                Content: rest rtrim
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "  content  " } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("  content", table[0][0]);
    }

    /// <summary>
    ///     Tests rest with ltrim modifier.
    /// </summary>
    [TestMethod]
    public void Text_Rest_WithLtrim_ShouldRemoveLeadingOnly()
    {
        var query = @"
            text Data { 
                Content: rest ltrim
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "  content  " } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("content  ", table[0][0]);
    }

    #endregion

    #region Section 5.7: Rest in Complex Schemas

    /// <summary>
    ///     Tests rest after multiple fields.
    /// </summary>
    [TestMethod]
    public void Text_Rest_AfterMultipleFields_ShouldCaptureRemaining()
    {
        var query = @"
            text LogEntry { 
                Level: chars[5],
                Sep1: chars[1],
                Code: until ' ',
                Message: rest trim
            };
            select e.Level, e.Code, e.Message from #test.lines() l
            cross apply Parse(l.Line, 'LogEntry') e";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "ERROR:E001 Something went wrong" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual("E001", table[0][1]);
        Assert.AreEqual("Something went wrong", table[0][2]);
    }

    /// <summary>
    ///     Tests rest used for message capture in structured format.
    /// </summary>
    [TestMethod]
    public void Text_Rest_ForMessageCapture_ShouldWork()
    {
        var query = @"
            text Message { 
                Sender: until ':',
                Body: rest trim
            };
            select m.Sender, m.Body from #test.lines() l
            cross apply Parse(l.Line, 'Message') m
            order by m.Sender";

        var entities = new[]
        {
            new TextEntity { Name = "msg1", Text = "Alice: Hello there!" },
            new TextEntity { Name = "msg2", Text = "Bob: Hi Alice!" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Hello there!", table[0][1]);
        Assert.AreEqual("Bob", table[1][0]);
        Assert.AreEqual("Hi Alice!", table[1][1]);
    }

    #endregion

    #region Section 5.7: Rest Edge Cases

    /// <summary>
    ///     Tests rest with empty remaining content.
    /// </summary>
    [TestMethod]
    public void Text_Rest_EmptyRemaining_ShouldReturnEmpty()
    {
        var query = @"
            text Data { 
                Full: chars[10],
                Extra: rest
            };
            select d.Full, d.Extra from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "1234567890" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("1234567890", table[0][0]);
        Assert.AreEqual("", table[0][1]);
    }

    /// <summary>
    ///     Tests rest with special characters.
    /// </summary>
    [TestMethod]
    public void Text_Rest_WithSpecialCharacters_ShouldCapture()
    {
        var query = @"
            text Data { 
                Prefix: chars[3],
                Content: rest
            };
            select d.Prefix, d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = ">>>Special: @#$%^&*()!" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(">>>", table[0][0]);
        Assert.AreEqual("Special: @#$%^&*()!", table[0][1]);
    }

    #endregion
}
