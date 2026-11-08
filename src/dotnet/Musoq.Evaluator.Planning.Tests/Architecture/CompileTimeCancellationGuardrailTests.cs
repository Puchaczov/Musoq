using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CompileTimeCancellationGuardrailTests
{
    private static readonly HashSet<string> IntentionalRuntimeOrTestConstructionAllowlist =
    [
        "src/dotnet/Musoq.Schema.Tests/SchemaExtendedTests.SourceRuntimeSettings.cs",
        "src/dotnet/examples/data-sources/git/tests/GitProviderAndSchemaTests.cs"
    ];

    [TestMethod]
    public void CompileTimeSourceMetadataContexts_MustNotUseCancellationTokenNone()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var sourceFiles = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet", "*.cs")
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        var violations = new List<string>();
        var observedAllowlistEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in sourceFiles)
        {
            var source = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(
                         source,
                         @"\bnew\s+SourceMetadataContext\s*\(",
                         RegexOptions.CultureInvariant))
            {
                var invocation = ReadInvocation(source, match.Index);
                if (!invocation.Contains("CancellationToken.None", StringComparison.Ordinal))
                    continue;

                var relativePath = RepositorySourceScan.ToRelative(repositoryRoot, file);
                observedAllowlistEntries.Add(relativePath);
                if (!IntentionalRuntimeOrTestConstructionAllowlist.Contains(relativePath))
                    violations.Add($"{relativePath}:{GetLineNumber(source, match.Index)}");
            }
        }

        var staleAllowlistEntries = IntentionalRuntimeOrTestConstructionAllowlist
            .Where(path => !sourceFiles.Any(file =>
                string.Equals(RepositorySourceScan.ToRelative(repositoryRoot, file), path, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            staleAllowlistEntries,
            $"The SourceMetadataContext allowlist contains missing files: {string.Join(", ", staleAllowlistEntries)}");
        var unusedAllowlistEntries = IntentionalRuntimeOrTestConstructionAllowlist
            .Where(path => !observedAllowlistEntries.Contains(path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(
            unusedAllowlistEntries,
            "The SourceMetadataContext allowlist must contain only active intentional CancellationToken.None sites: " +
            string.Join(", ", unusedAllowlistEntries));
        Assert.IsEmpty(
            violations,
            "Compile-time SourceMetadataContext construction must receive the current invocation token. " +
            $"Unexpected CancellationToken.None sites: {string.Join(", ", violations)}");
    }

    private static string ReadInvocation(string source, int startIndex)
    {
        var parenthesisDepth = 0;
        var openingParenthesis = source.IndexOf('(', startIndex);
        if (openingParenthesis < 0)
            return string.Empty;

        for (var index = openingParenthesis; index < source.Length; index++)
        {
            switch (source[index])
            {
                case '(':
                    parenthesisDepth++;
                    break;
                case ')' when --parenthesisDepth == 0:
                    return source[startIndex..(index + 1)];
            }
        }

        return source[startIndex..];
    }

    private static int GetLineNumber(string source, int index)
    {
        var lineNumber = 1;
        for (var characterIndex = 0; characterIndex < index; characterIndex++)
        {
            if (source[characterIndex] == '\n')
                lineNumber++;
        }

        return lineNumber;
    }
}
