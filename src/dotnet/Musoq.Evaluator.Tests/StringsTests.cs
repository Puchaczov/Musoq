using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StringsTests : BasicEntityTestBase
{
    private static readonly char[] SpecialCharacterCases =
    [
        '{', '}', '(', ')', '-', '/', '*', '+', '=', '!', '<', '>', '&', '|', '^', '%', '~', '`',
        '[', ']', ';', ':', ',', '.', '?', '@', '#', '$', ' ', '"'
    ];

    private static readonly CompiledQueryBatchRepository<char> SpecialCharacterQueries =
        new(CreateSpecialCharacterQueries);

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenQuoteUsed_MustNotThrow()
    {
        var query = """select '"' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("\"", table[0].Values[0]);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("\"", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenQuotePrecededByTextUsed_MustNotThrow()
    {
        var query = """select 'text "' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("text \"", table[0].Values[0]);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("text \"", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenQuoteFollowedByTextUsed_MustNotThrow()
    {
        var query = """select '"text' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("\"text", table[0].Values[0]);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("\"text", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenQuoteFollowedAndPrecededByTextUsed_MustNotThrow()
    {
        var query = """select '"text"' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("\"text\"", table[0].Values[0]);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("\"text\"", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEscapeCharacterUsed_MustNotThrow()
    {
        const string query = """select '\'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("'", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEscapeCharacterUsedInText_MustNotThrow()
    {
        const string query = """select 'text \'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("text '", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleEscapeCharactersUsedInText_MustNotThrow()
    {
        const string query = """select 'lorem\' ipsum dolor\'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("lorem' ipsum dolor'", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleEscapeCharactersUsedInTextWithQuote_MustNotThrow()
    {
        const string query = """select 'lorem\' " ipsum dolor\'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("lorem' \" ipsum dolor'", table[0].Values[0]);
    }

    [DataRow('{')]
    [DataRow('}')]
    [DataRow('(')]
    [DataRow(')')]
    [DataRow('-')]
    [DataRow('/')]
    [DataRow('*')]
    [DataRow('+')]
    [DataRow('=')]
    [DataRow('!')]
    [DataRow('<')]
    [DataRow('>')]
    [DataRow('&')]
    [DataRow('|')]
    [DataRow('^')]
    [DataRow('%')]
    [DataRow('~')]
    [DataRow('`')]
    [DataRow('[')]
    [DataRow(']')]
    [DataRow(';')]
    [DataRow(':')]
    [DataRow(',')]
    [DataRow('.')]
    [DataRow('?')]
    [DataRow('@')]
    [DataRow('#')]
    [DataRow('$')]
    [DataRow(' ')]
    [DataRow('"')]
    [TestMethod]
    public void WhenSpecialCharacterStartBracketUsedInTextWith_MustNotThrow(char specialCharacter)
    {
        using var measurement = EvaluatorTestCaseMeasurement.Begin(
            nameof(WhenSpecialCharacterStartBracketUsedInTextWith_MustNotThrow),
            specialCharacter.ToString());
        using var vm = measurement.MeasureCompilation(() => SpecialCharacterQueries.Take(specialCharacter));

        using var table = measurement.MeasureExecution(() => vm.Run(TestContext.CancellationToken));
        measurement.MeasureMaterialization(() => TableMaterializationTestHelper.Materialize(table));

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual(specialCharacter.ToString(), table[0].Values[0]);
    }

    [ClassCleanup]
    public static void DisposeSpecialCharacterBatch()
    {
        SpecialCharacterQueries.Dispose();
    }

    private static IReadOnlyDictionary<char, CompiledQuery> CreateSpecialCharacterQueries()
    {
        var requests = SpecialCharacterCases
            .Select((specialCharacter, index) => new ExecutionBatchCompilationRequest(
                specialCharacter.ToString(),
                $"select '{specialCharacter}' from #A.entities()",
                $"SpecialCharacterBatch_{index}",
                new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] = [new BasicEntity("test")]
                }),
                new TestsLoggerResolver(),
                TestCompilationOptions,
                ConsumerFamily: "string-format-cases",
                ConsumerTestName: nameof(CreateSpecialCharacterQueries),
                BatchOrigin: "string-format-cases"))
            .ToArray();

        var results = InstanceCreator.CompileForExecutionBatch(requests);
        var queries = new Dictionary<char, CompiledQuery>();
        try
        {
            foreach (var result in results)
            {
                if (!result.Result.Succeeded)
                    throw new InvalidOperationException(
                        $"Special-character query '{result.Key}' failed to compile.",
                        result.Result.CaughtException);

                queries.Add(result.Key[0], result.Result.CompiledQuery);
            }

            return queries;
        }
        catch
        {
            foreach (var query in queries.Values)
                query.Dispose();
            foreach (var result in results)
            {
                if (result.Result.Succeeded && !queries.ContainsKey(result.Key[0]))
                    result.Result.CompiledQuery.Dispose();
            }

            throw;
        }
    }

    [TestMethod]
    public void WhenIndexOfCalled_ShouldReturnFirstIndex()
    {
        const string query = """select IndexOf('a/b/c', '/') from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(string.Empty)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNthIndexOfCalled_ShouldReturnSecondIndex()
    {
        const string query = """select NthIndexOf('a/b/c', '/', 1) from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(string.Empty)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(3, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenLastIndexOfCalled_ShouldReturnLastIndex()
    {
        const string query = """select LastIndexOf('a/b/c', '/') from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(string.Empty)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(3, table[0].Values[0]);
    }
}
