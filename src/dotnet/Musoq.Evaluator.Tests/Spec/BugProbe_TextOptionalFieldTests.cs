using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text 'optional' fields.
///     Per spec section 5.8: "Field may or may not be present. Results in null if not matched."
///     No E2E coverage exists for optional fields.
/// </summary>
[TestClass]
public class BugProbe_TextOptionalFieldTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Optional field present - should capture value.
    ///     Spec 5.8: "On success: field has value, cursor advances as normal"
    /// </summary>
    [TestMethod]
    public void Text_OptionalFieldPresent_ShouldCaptureValue()
    {
        var query = @"
            text Data { 
                Key: until ':',
                Value: rest,
                Extra: optional pattern '\d+'
            };
            select d.Key, d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host:local" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("local", table[0][1]);
    }

    /// <summary>
    ///     PROBE: Optional literal field absent - should produce null and not fail.
    ///     Spec 5.8: "On failure: field is null, cursor is UNCHANGED"
    /// </summary>
    [TestMethod]
    public void Text_OptionalLiteralAbsent_ShouldReturnNullWithoutFailing()
    {
        var query = @"
            text Data { 
                Main: until ';',
                _: optional literal ' ',
                Extra: optional rest
            };
            select d.Main, d.Extra from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "value;" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("value", table[0][0]);
    }
}
