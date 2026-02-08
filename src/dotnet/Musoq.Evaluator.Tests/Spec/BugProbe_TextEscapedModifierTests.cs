using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text 'between' with 'escaped' modifier.
///     Per spec section 5.4.3: "Handles escape sequences within delimited content"
///     Known issue: escaped modifier is parsed but never generates escape-handling code.
/// </summary>
[TestClass]
public class BugProbe_TextEscapedModifierTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: 'between' with 'escaped' should handle escaped delimiters.
    ///     Spec 5.4.3: "Default escape character is backslash (\)"
    ///     Expected: Escaped closing delimiter should be included in captured content.
    ///     Input: "hello \"world\" end" → captured: hello \"world\" end
    /// </summary>
    [TestMethod]
    public void Text_BetweenEscaped_ShouldHandleEscapedDelimiters()
    {
        var query = @"
            text Data { Value: between '""' '""' escaped, Rest: rest };
            select d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            // The input is: "hello \"world\" end"more
            // After between with escaped: captured = hello \"world\" end (escaped quotes not delimiters)
            new TextEntity { Name = "1.txt", Text = "\"hello \\\"world\\\" end\"more" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello \\\"world\\\" end", table[0][0]);
    }
}
