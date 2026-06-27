using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TwoModeDiagnosticsGuardrailTests
{
    private static readonly string[] TwoModeBehaviorTestFiles =
    [
        "DirectTypedProfilingTests.cs",
        "TypedExecutionTests.cs",
        "TypedInspectionTests.cs",
        "TypedProfilingTests.cs"
    ];

    [TestMethod]
    public void TwoModeBehaviorTests_ShouldNotUsePrivateReflectionForDiagnostics()
    {
        var forbiddenTokens = new[]
        {
            "BindingFlags.NonPublic",
            ".GetField(",
            ".GetProperty(",
            "\"_factory\"",
            "\"_runnableType\""
        };

        var failures = FindForbiddenTokens(TwoModeBehaviorTestFiles, forbiddenTokens);

        Assert.AreEqual(0, failures.Length, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void TypedInspectionBehaviorTests_ShouldNotClassifyRowsByGeneratedHelperNames()
    {
        var forbiddenTokens = new[]
        {
            "ComputeTable_",
            "ComputeRows_",
            "TypedProjectionRows",
            "TableProjectionRows",
            "ProjectRowsParallel",
            "TypedPostOperationRows"
        };

        var failures = FindForbiddenTokens(["TypedInspectionTests.cs"], forbiddenTokens);

        Assert.AreEqual(0, failures.Length, string.Join(Environment.NewLine, failures));
    }

    private static string[] FindForbiddenTokens(IEnumerable<string> fileNames, IEnumerable<string> forbiddenTokens)
    {
        var root = FindRepositoryRoot();
        var testRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter.Tests");

        return fileNames
            .Select(fileName => new
            {
                FileName = fileName,
                Text = File.ReadAllText(Path.Combine(testRoot, fileName))
            })
            .SelectMany(file => forbiddenTokens
                .Where(file.Text.Contains)
                .Select(token => $"{file.FileName} contains forbidden two-mode behavior test token: {token}"))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "dotnet", "Musoq.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing src/dotnet/Musoq.sln.");
    }
}
