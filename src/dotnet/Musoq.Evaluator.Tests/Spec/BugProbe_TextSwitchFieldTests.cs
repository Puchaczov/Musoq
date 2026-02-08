using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Text 'switch' field (pattern-based dispatch).
///     Per spec section 5.10: "Choose parsing strategy based on lookahead"
///     ZERO test coverage anywhere in the codebase.
/// </summary>
[TestClass]
public class BugProbe_TextSwitchFieldTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Switch text field dispatching to different schemas based on leading character.
    ///     Spec 5.10: "Patterns are tested in order using lookahead...First matching pattern determines which type to parse"
    ///     Expected: Each line dispatched to the correct sub-schema.
    /// </summary>
    [TestMethod]
    public void Text_SwitchField_ShouldDispatchByPattern()
    {
        var query = @"
            text Comment { _: literal '#', Text: rest };
            text KeyValue { Key: until '=', Value: rest };
            text ConfigLine {
                Content: switch {
                    pattern '#' => Comment,
                    _ => KeyValue
                }
            };
            select d.Content.Key from #test.lines() l
            cross apply Parse(l.Line, 'ConfigLine') d
            where d.Content.Key is not null";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host=localhost" },
            new TextEntity { Name = "2.txt", Text = "#comment line" },
            new TextEntity { Name = "3.txt", Text = "port=8080" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(results, "host");
        CollectionAssert.Contains(results, "port");
    }
}
