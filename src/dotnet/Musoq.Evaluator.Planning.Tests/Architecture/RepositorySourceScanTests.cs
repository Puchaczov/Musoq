using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class RepositorySourceScanTests
{
    [TestMethod]
    public void MissingSourceRoot_ShouldFailBeforeCheckingViolations()
    {
        using var source = new SourceFixture();
        Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
            RepositorySourceScan.ProductionSourceFiles(source.Root, "Missing")
        );
        Assert.ThrowsExactly<DirectoryNotFoundException>(() =>
            RepositorySourceScan.FilesUnder(source.Root, "Missing", "*.cs")
        );
    }

    [TestMethod]
    public void EmptySourceRoot_ShouldFailBeforeCheckingViolations()
    {
        using var source = new SourceFixture();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RepositorySourceScan.ProductionSourceFiles(source.Root, "Example")
        );
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RepositorySourceScan.FilesUnder(source.Root, "src/dotnet/Example", "*.cs")
        );
        var other = Path.Combine(source.Root, "src", "dotnet", "Other");
        Directory.CreateDirectory(other);
        File.WriteAllText(Path.Combine(other, "Valid.cs"), "public sealed class Valid {}");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RepositorySourceScan.ProductionSourceFiles(source.Root, "Other", "Example")
        );
    }

    [TestMethod]
    public void ValidInventory_ShouldIncludeTheExpectedFileAndReportNoViolation()
    {
        using var source = new SourceFixture();
        File.WriteAllText(source.File, "public sealed class Valid {}");
        var files = RepositorySourceScan.ProductionSourceFiles(source.Root, "Example");
        CollectionAssert.AreEqual(new[] { source.File }, files.ToArray());
        Assert.AreEqual(0, RepositorySourceScan.CountMatchingLines(files, new Regex("ForbiddenContract")));
    }

    [TestMethod]
    public void ViolatingInventory_ShouldReportTheForbiddenContract()
    {
        using var source = new SourceFixture();
        File.WriteAllText(source.File, "public sealed class ForbiddenContract {}");
        var files = RepositorySourceScan.FilesUnder(source.Root, "src/dotnet/Example", "*.cs");
        CollectionAssert.AreEqual(new[] { source.File }, files.ToArray());
        Assert.AreEqual(1, RepositorySourceScan.CountMatchingLines(files, new Regex("ForbiddenContract")));
    }

    private sealed class SourceFixture : IDisposable
    {
        internal string Root { get; } =
            Path.Combine(
                RepositorySourceScan.RepositoryRoot(),
                ".artifacts",
                "test-reliability",
                "source-fixtures",
                "Workspace.Tests",
                Guid.NewGuid().ToString("N")
            );
        internal string File => Path.Combine(Root, "src", "dotnet", "Example", "Contract.cs");

        internal SourceFixture() => Directory.CreateDirectory(Path.GetDirectoryName(File)!);

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
