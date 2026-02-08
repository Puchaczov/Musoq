using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text field modifiers 'lower' and 'upper'.
///     Per spec section 5.12: lower/upper are valid text modifiers.
///     Known issue: code generator's GenerateModifierArgs only handles trim/ltrim/rtrim,
///     silently ignoring lower/upper even though parser accepts them.
/// </summary>
[TestClass]
public class BugProbe_TextModifierLowerUpperTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: 'lower' modifier on an 'until' field should produce lowercase output.
    ///     Spec 5.12: "lower - Convert to lowercase"
    ///     Expected: The captured text should be lowercased.
    /// </summary>
    [TestMethod]
    public void Text_LowerModifier_ShouldConvertToLowerCase()
    {
        var query = @"
            text Data { Key: until ':' lower, Value: rest };
            select d.Key, d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "HOST:localhost" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]); // Should be lowercased
        Assert.AreEqual("localhost", table[0][1]);
    }

    /// <summary>
    ///     BUG: 'upper' modifier on a 'rest' field should produce uppercase output.
    ///     Spec 5.12: "upper - Convert to uppercase"
    ///     Expected: The captured text should be uppercased.
    /// </summary>
    [TestMethod]
    public void Text_UpperModifier_ShouldConvertToUpperCase()
    {
        var query = @"
            text Data { Key: until ':', Value: rest upper };
            select d.Key, d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host:localhost" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("LOCALHOST", table[0][1]); // Should be uppercased
    }

    /// <summary>
    ///     BUG: 'lower' modifier on 'between' field.
    ///     Spec 5.12: lower applies to "All capture types"
    /// </summary>
    [TestMethod]
    public void Text_LowerModifierOnBetween_ShouldConvertToLowerCase()
    {
        var query = @"
            text Data { Value: between '[' ']' lower };
            select d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "[WARNING]" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("warning", table[0][0]); // Should be lowercased
    }

    /// <summary>
    ///     BUG: 'upper' modifier on 'chars' field.
    ///     Spec 5.12: upper applies to "All capture types"
    /// </summary>
    [TestMethod]
    public void Text_UpperModifierOnChars_ShouldConvertToUpperCase()
    {
        var query = @"
            text Data { Code: chars[3] upper, Rest: rest };
            select d.Code, d.Rest from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "abc123" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABC", table[0][0]); // Should be uppercased
        Assert.AreEqual("123", table[0][1]);
    }
}
