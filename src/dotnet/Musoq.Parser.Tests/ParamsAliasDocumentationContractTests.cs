using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParamsAliasDocumentationContractTests
{
    [TestMethod]
    public void CoreLanguageSpec_ShouldDocumentAliasAndCanonicalSpellingPolicy()
    {
        var specification = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "specs", "musoq-core-language-spec.md"));

        StringAssert.Contains(specification, "Either `param(...)` or `params(...)`");
        StringAssert.Contains(
            specification,
            "`param(...)` remains the canonical spelling in this specification");
        StringAssert.Contains(
            specification,
            "`params(...)` is accepted only at that leading boundary");
        StringAssert.Contains(specification, "params(author: string)");
        StringAssert.Contains(specification, "parameter_keyword ::= PARAM | PARAMS");
        StringAssert.Contains(specification, "param(string author)");
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
