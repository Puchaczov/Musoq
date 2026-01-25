using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Text Schema 'between' Delimiter Capture (Section 5.4.2 of specification).
///     Tests capturing content between opening and closing delimiters.
/// </summary>
[TestClass]
public class TextBetweenFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 5.4.2: Between Empty Content

    /// <summary>
    ///     Tests between with empty content.
    /// </summary>
    [TestMethod]
    public void Text_Between_EmptyContent_ShouldReturnEmpty()
    {
        var query = @"
            text Data { 
                Content: between '(' ')'
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "()" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("", table[0][0]);
    }

    #endregion

    #region Section 5.4.2: Multiple Between Fields

    /// <summary>
    ///     Tests multiple between captures in sequence.
    /// </summary>
    [TestMethod]
    public void Text_Between_MultipleBetweenFields_ShouldParseSequentially()
    {
        var query = @"
            text Data { 
                First: between '[' ']',
                Second: between '(' ')'
            };
            select d.First, d.Second from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "[alpha](beta)" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("beta", table[0][1]);
    }

    #endregion

    #region Section 5.4.2: Between in WHERE Clause

    /// <summary>
    ///     Tests filtering by between-captured field.
    /// </summary>
    [TestMethod]
    public void Text_Between_InWhereClause_ShouldFilter()
    {
        var query = @"
            text Tag { 
                Name: between '<' '>'
            };
            select t.Name from #test.lines() l
            cross apply Parse(l.Line, 'Tag') t
            where t.Name = 'div'";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "<div>" },
            new TextEntity { Name = "line2", Text = "<span>" },
            new TextEntity { Name = "line3", Text = "<div>" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("div", table[0][0]);
        Assert.AreEqual("div", table[1][0]);
    }

    #endregion

    #region Section 5.4.2: Between with Multi-Character Delimiters

    /// <summary>
    ///     Tests between with multi-character opening and closing.
    /// </summary>
    [TestMethod]
    public void Text_Between_MultiCharDelimiters_ShouldCapture()
    {
        var query = @"
            text Comment { 
                Content: between '/*' '*/'
            };
            select c.Content from #test.lines() l
            cross apply Parse(l.Line, 'Comment') c";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "/* this is a comment */" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(" this is a comment ", table[0][0]);
    }

    #endregion

    #region Section 5.4.2: Real-World Examples

    /// <summary>
    ///     Tests parsing log timestamp in brackets.
    /// </summary>
    [TestMethod]
    public void Text_Between_LogTimestamp_ShouldCapture()
    {
        var query = @"
            text LogLine { 
                Timestamp: between '[' ']',
                Message: rest trim
            };
            select l.Timestamp, l.Message from #test.lines() f
            cross apply Parse(f.Line, 'LogLine') l";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "[2024-01-15 10:30:00] Application started" },
            new TextEntity { Name = "line2", Text = "[2024-01-15 10:30:05] Connected to database" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var timestamps = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        var messages = new HashSet<string> { (string)table[0][1], (string)table[1][1] };
        Assert.IsTrue(timestamps.Contains("2024-01-15 10:30:00"));
        Assert.IsTrue(timestamps.Contains("2024-01-15 10:30:05"));
        Assert.IsTrue(messages.Contains("Application started"));
        Assert.IsTrue(messages.Contains("Connected to database"));
    }

    #endregion

    #region Section 5.4.2: Basic Between

    /// <summary>
    ///     Tests basic between delimiters capture.
    /// </summary>
    [TestMethod]
    public void Text_Between_Brackets_ShouldCapture()
    {
        var query = @"
            text Data { 
                Content: between '[' ']'
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "[hello world]" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    /// <summary>
    ///     Tests between with parentheses.
    /// </summary>
    [TestMethod]
    public void Text_Between_Parentheses_ShouldCapture()
    {
        var query = @"
            text Data { 
                Args: between '(' ')'
            };
            select d.Args from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "(arg1, arg2)" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("arg1, arg2", table[0][0]);
    }

    /// <summary>
    ///     Tests between with quotes.
    /// </summary>
    [TestMethod]
    public void Text_Between_Quotes_ShouldCapture()
    {
        var query = @"
            text Data { 
                Quoted: between '""' '""'
            };
            select d.Quoted from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "\"quoted text\"" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("quoted text", table[0][0]);
    }

    #endregion

    #region Section 5.4.2: Between with Different Open/Close

    /// <summary>
    ///     Tests between with angle brackets.
    /// </summary>
    [TestMethod]
    public void Text_Between_AngleBrackets_ShouldCapture()
    {
        var query = @"
            text Tag { 
                Name: between '<' '>'
            };
            select t.Name from #test.lines() l
            cross apply Parse(l.Line, 'Tag') t";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "<html>" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("html", table[0][0]);
    }

    /// <summary>
    ///     Tests between with curly braces.
    /// </summary>
    [TestMethod]
    public void Text_Between_CurlyBraces_ShouldCapture()
    {
        var query = @"
            text Placeholder { 
                Name: between '{' '}'
            };
            select p.Name from #test.lines() l
            cross apply Parse(l.Line, 'Placeholder') p";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "{username}" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("username", table[0][0]);
    }

    #endregion

    #region Section 5.4.2: Between with Content Before/After

    /// <summary>
    ///     Tests between with content before delimiter.
    /// </summary>
    [TestMethod]
    public void Text_Between_WithPrefix_ShouldCapture()
    {
        var query = @"
            text Data { 
                Prefix: until '[',
                Content: between '[' ']'
            };
            select d.Prefix, d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "prefix[content]" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("prefix", table[0][0]);
        Assert.AreEqual("content", table[0][1]);
    }

    /// <summary>
    ///     Tests between with content after.
    /// </summary>
    [TestMethod]
    public void Text_Between_WithSuffix_ShouldCapture()
    {
        var query = @"
            text Data { 
                Content: between '[' ']',
                Suffix: rest
            };
            select d.Content, d.Suffix from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "[content]suffix" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("content", table[0][0]);
        Assert.AreEqual("suffix", table[0][1]);
    }

    #endregion
}
