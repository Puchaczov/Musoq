using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text features without E2E coverage - pattern (regex), token, whitespace.
///     These are specified in spec sections 5.2, 5.6, 5.11 but have no evaluator-level tests.
///     Any failure here reveals a code generation or runtime bug.
/// </summary>
[TestClass]
public class BugProbe_TextPatternTokenWhitespaceTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Text 'pattern' (regex) field.
    ///     Spec 5.2: "pattern '\d+' - Captures consecutive digits"
    ///     Expected: regex matches and captures the result.
    /// </summary>
    [TestMethod]
    public void Text_PatternRegex_ShouldCaptureMatch()
    {
        var query = @"
            text Data { Digits: pattern '\d+', Rest: rest };
            select d.Digits, d.Rest from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "123abc" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("123", table[0][0]);
        Assert.AreEqual("abc", table[0][1]);
    }

    /// <summary>
    ///     PROBE: Text 'pattern' with IP address regex.
    ///     Spec 5.2 example: "Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}'"
    /// </summary>
    [TestMethod]
    public void Text_PatternRegex_ShouldCaptureIpAddress()
    {
        var query = @"
            text Data { 
                Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',
                Rest: rest 
            };
            select d.Ip, d.Rest from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "192.168.1.1:8080" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("192.168.1.1", table[0][0]);
        Assert.AreEqual(":8080", table[0][1]);
    }

    /// <summary>
    ///     PROBE: Text 'token' field (whitespace-delimited).
    ///     Spec 5.6: "Captures consecutive non-whitespace characters"
    /// </summary>
    [TestMethod]
    public void Text_Token_ShouldCaptureNonWhitespace()
    {
        var query = @"
            text Data { 
                First: token,
                _: whitespace,
                Second: token,
                _: whitespace,
                Third: rest 
            };
            select d.First, d.Second, d.Third from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "hello world rest" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
        Assert.AreEqual("rest", table[0][2]);
    }

    /// <summary>
    ///     PROBE: Text 'whitespace*' (zero or more).
    ///     Spec 5.11: "whitespace* - Zero or more, Never fails"
    /// </summary>
    [TestMethod]
    public void Text_WhitespaceStarQuantifier_ShouldHandleZeroSpaces()
    {
        var query = @"
            text Data { 
                A: until ':',
                _: whitespace*,
                B: rest 
            };
            select d.A, d.B from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "key:value" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
    }

    /// <summary>
    ///     PROBE: Text 'whitespace+' (one or more) with actual whitespace present.
    ///     Spec 5.11: "whitespace+ - One or more, Error if no whitespace"
    /// </summary>
    [TestMethod]
    public void Text_WhitespacePlusQuantifier_ShouldConsumeMandatorySpaces()
    {
        var query = @"
            text Data { 
                A: token,
                _: whitespace+,
                B: rest 
            };
            select d.A, d.B from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "hello   world" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
    }
}
