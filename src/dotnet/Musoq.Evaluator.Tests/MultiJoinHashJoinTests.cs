using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests to verify hash join optimization works correctly with multiple joins (more than 2 tables).
///     These tests are designed to expose potential issues where hash join may not be properly applied
///     to subsequent joins after the first join in a chain.
/// </summary>
[TestClass]
public partial class MultiJoinHashJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }








    #region Helper Methods

    private string CompileAndGetGeneratedCode(
        string query,
        Dictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions compilationOptions)
    {
        RuntimeLibraries.CreateReferences();

        var items = new BuildItems
        {
            SchemaProvider = new BasicSchemaProvider<BasicEntity>(sources),
            RawQuery = query,
            AssemblyName = Guid.NewGuid().ToString(),
            CompilationOptions = compilationOptions,
            CreateBuildMetadataAndInferTypesVisitor = null,
            DiagnosticContext = new DiagnosticContext(new SourceText(query))
        };


        var chain = new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), LoggerResolver)));

        chain.Build(items);


        var builder = new StringBuilder();
        if (items.Compilation?.SyntaxTrees != null)
            foreach (var tree in items.Compilation.SyntaxTrees)
            {
                using var writer = new StringWriter();
                tree.GetRoot().WriteTo(writer);
                builder.AppendLine(writer.ToString());
                builder.AppendLine();
            }

        return builder.ToString();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static int CountHashDictionaryDeclarations(string generatedCode)
    {
        return CountOccurrences(generatedCode, "Hashed = new Dictionary<") +
               CountOccurrences(generatedCode, "Hash = new Dictionary<");
    }

    private static void AssertContainsAny(string text, string message, params string[] patterns)
    {
        if (patterns.Any(pattern => text.Contains(pattern, StringComparison.Ordinal)))
            return;

        Assert.Fail(message);
    }

    #endregion
}
