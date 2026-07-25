using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class NamedDatasourceDocumentationRatchetTests
{
    [TestMethod]
    public void NormativeSpecAndGuidesDescribeNamedDatasourceArguments()
    {
        var root = FindRepositoryRoot();
        var coreSpec = File.ReadAllText(Path.Combine(root, "specs", "musoq-core-language-spec.md"));
        var coverage = File.ReadAllText(Path.Combine(root, "specs", "runtime-testability-coverage.md"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));

        StringAssert.Contains(coreSpec, "source_arg_list");
        StringAssert.Contains(coreSpec, "MQ3083");
        StringAssert.Contains(coreSpec, "DESC FUNCTIONS");
        StringAssert.Contains(coreSpec, "positional-prefix/named-suffix");
        StringAssert.Contains(coreSpec, "SourceExecutionContext");
        StringAssert.Contains(coreSpec, "known canonical prefix");
        StringAssert.Contains(coreSpec, "reflection enumeration order");
        StringAssert.Contains(coverage, "Named datasource arguments");
        StringAssert.Contains(coverage, "16710 passed and 4 skipped");
        StringAssert.Contains(coverage, "generated C# label erasure");
        StringAssert.Contains(readme, "reflected optional constructor defaults");
        StringAssert.Contains(readme, "never a dictionary");
        StringAssert.Contains(changelog, "case-insensitive named datasource arguments");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "specs", "musoq-core-language-spec.md")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Musoq repository root.");
    }
}
