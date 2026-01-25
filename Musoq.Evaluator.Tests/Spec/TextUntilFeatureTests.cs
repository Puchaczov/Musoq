using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Text Schema 'until' Delimiter Capture (Section 5.4.1 of specification).
///     Tests capturing content up to a delimiter.
/// </summary>
[TestClass]
public class TextUntilFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 5.4.1: Until with Empty Captures

    /// <summary>
    ///     Tests until when delimiter is at start (empty capture).
    /// </summary>
    [TestMethod]
    public void Text_Until_DelimiterAtStart_ShouldReturnEmpty()
    {
        var query = @"
            text Data { 
                Key: until '=',
                Value: rest 
            };
            select d.Key, d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "=value" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
    }

    #endregion

    #region Section 5.4.1: Until in WHERE Clause

    /// <summary>
    ///     Tests filtering by until-captured field.
    /// </summary>
    [TestMethod]
    public void Text_Until_InWhereClause_ShouldFilter()
    {
        var query = @"
            text KeyValue { 
                Key: until '=',
                Value: rest 
            };
            select k.Value from #test.lines() l
            cross apply Parse(l.Line, 'KeyValue') k
            where k.Key = 'host'";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "host=localhost" },
            new TextEntity { Name = "line2", Text = "port=8080" },
            new TextEntity { Name = "line3", Text = "host=example.com" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Section 5.4.1: Until with Multiple Lines

    /// <summary>
    ///     Tests parsing multiple lines with until.
    /// </summary>
    [TestMethod]
    public void Text_Until_MultipleRows_ShouldParseAll()
    {
        var query = @"
            text KeyValue { 
                Key: until ':',
                Value: rest trim
            };
            select k.Key, k.Value from #test.lines() l
            cross apply Parse(l.Line, 'KeyValue') k
            order by k.Key";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "alpha: one" },
            new TextEntity { Name = "line2", Text = "beta: two" },
            new TextEntity { Name = "line3", Text = "gamma: three" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("one", table[0][1]);
        Assert.AreEqual("beta", table[1][0]);
        Assert.AreEqual("gamma", table[2][0]);
    }

    #endregion

    #region Section 5.4.1: Basic Until

    /// <summary>
    ///     Tests basic until delimiter capture.
    /// </summary>
    [TestMethod]
    public void Text_Until_BasicDelimiter_ShouldCapture()
    {
        var query = @"
            text KeyValue { 
                Key: until ':',
                Value: rest 
            };
            select k.Key, k.Value from #test.lines() l
            cross apply Parse(l.Line, 'KeyValue') k";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "name:John" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("name", table[0][0]);
        Assert.AreEqual("John", table[0][1]);
    }

    /// <summary>
    ///     Tests until with space delimiter.
    /// </summary>
    [TestMethod]
    public void Text_Until_SpaceDelimiter_ShouldCapture()
    {
        var query = @"
            text Words { 
                First: until ' ',
                Second: until ' ',
                Third: rest 
            };
            select w.First, w.Second, w.Third from #test.lines() l
            cross apply Parse(l.Line, 'Words') w";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "one two three" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("one", table[0][0]);
        Assert.AreEqual("two", table[0][1]);
        Assert.AreEqual("three", table[0][2]);
    }

    /// <summary>
    ///     Tests until with multi-character delimiter.
    /// </summary>
    [TestMethod]
    public void Text_Until_MultiCharDelimiter_ShouldCapture()
    {
        var query = @"
            text Data { 
                Before: until '::',
                After: rest 
            };
            select d.Before, d.After from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "header::body" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("header", table[0][0]);
        Assert.AreEqual("body", table[0][1]);
    }

    #endregion

    #region Section 5.4.1: Multiple Until Fields

    /// <summary>
    ///     Tests multiple until fields in sequence.
    /// </summary>
    [TestMethod]
    public void Text_Until_MultipleFields_ShouldParseSequentially()
    {
        var query = @"
            text LogEntry { 
                Date: until ' ',
                Time: until ' ',
                Level: until ':',
                Message: rest 
            };
            select e.Date, e.Time, e.Level, e.Message from #test.lines() l
            cross apply Parse(l.Line, 'LogEntry') e";

        var entities = new[]
            { new TextEntity { Name = "test.txt", Text = "2024-01-15 10:30:00 INFO:Application started" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15", table[0][0]);
        Assert.AreEqual("10:30:00", table[0][1]);
        Assert.AreEqual("INFO", table[0][2]);
        Assert.AreEqual("Application started", table[0][3]);
    }

    /// <summary>
    ///     Tests CSV-like parsing with comma delimiter.
    /// </summary>
    [TestMethod]
    public void Text_Until_CsvStyle_ShouldParseFields()
    {
        var query = @"
            text CsvRow { 
                Col1: until ',',
                Col2: until ',',
                Col3: rest 
            };
            select c.Col1, c.Col2, c.Col3 from #test.lines() l
            cross apply Parse(l.Line, 'CsvRow') c";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "apple,banana,cherry" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("apple", table[0][0]);
        Assert.AreEqual("banana", table[0][1]);
        Assert.AreEqual("cherry", table[0][2]);
    }

    #endregion
}
