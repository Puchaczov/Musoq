using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RequiredSourceInventoryTests
{
    [TestMethod]
    public void MissingDirectory_ShouldFailBeforeCheckingViolations()
    {
        using var fixture = new SourceFixture();
        Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
            RequiredSourceInventory.Files(Path.Combine(fixture.Root, "missing"), "*.cs")
        );
    }

    [TestMethod]
    public void EmptyInventory_ShouldFailBeforeCheckingViolations()
    {
        using var fixture = new SourceFixture();
        Assert.ThrowsExactly<InvalidOperationException>(() => RequiredSourceInventory.Files(fixture.Root, "*.cs"));
    }

    [TestMethod]
    [DataRow("bin")]
    [DataRow("obj")]
    public void BuildOutputAlone_ShouldNotSatisfyAuthoredInventory(string outputDirectory)
    {
        using var fixture = new SourceFixture();
        var output = Path.Combine(fixture.Root, outputDirectory);
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "Generated.cs"), "class Generated {}");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RequiredSourceInventory.Files(fixture.Root, "*.cs", SearchOption.AllDirectories)
        );
    }

    [TestMethod]
    [DataRow("Allowed", 0)]
    [DataRow("Forbidden", 1)]
    public void AuthoredInventory_ShouldIncludeTheExpectedFileAndReportViolations(
        string declaration,
        int expectedViolations
    )
    {
        using var fixture = new SourceFixture();
        var file = Path.Combine(fixture.Root, "Contract.cs");
        File.WriteAllText(file, "class " + declaration + " {}");
        var files = RequiredSourceInventory.Files(fixture.Root, "*.cs");
        CollectionAssert.AreEqual(new[] { file }, files);
        Assert.AreEqual(
            expectedViolations,
            files.Count(path => File.ReadAllText(path).Contains("Forbidden", StringComparison.Ordinal))
        );
    }

    [TestMethod]
    public void SearchScope_ShouldRequireMatchingFilesWithinTheRequestedScope()
    {
        using var fixture = new SourceFixture();
        var child = Path.Combine(fixture.Root, "child");
        Directory.CreateDirectory(child);
        var file = Path.Combine(child, "Contract.cs");
        File.WriteAllText(file, "class Contract {}");
        Assert.ThrowsExactly<InvalidOperationException>(() => RequiredSourceInventory.Files(fixture.Root, "*.cs"));
        CollectionAssert.AreEqual(
            new[] { file },
            RequiredSourceInventory.Files(fixture.Root, "*.cs", SearchOption.AllDirectories)
        );
    }

    private sealed class SourceFixture : IDisposable
    {
        public string Root { get; } =
            Path.Combine(
                RepositoryPaths.FindRoot(),
                ".artifacts",
                "test-reliability",
                "remediation-followup",
                "source-inventory-fixtures",
                Guid.NewGuid().ToString("N")
            );

        public SourceFixture() => Directory.CreateDirectory(Root);

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
