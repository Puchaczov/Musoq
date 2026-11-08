using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RepositoryPathsTests
{
    private readonly string _fixtureDirectory = Path.Combine(
        RepositoryPaths.FindRoot(), ".artifacts", "repository-path-tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_fixtureDirectory))
            Directory.Delete(_fixtureDirectory, recursive: true);
    }

    [TestMethod]
    public void FindRoot_WhenNearerGlobalJsonIsUnrelated_ShouldFindContainingRepository()
    {
        CreateRepositoryMarkers(_fixtureDirectory);
        var unrelated = Directory.CreateDirectory(Path.Combine(_fixtureDirectory, "unrelated"));
        File.WriteAllText(Path.Combine(unrelated.FullName, "global.json"), "{}");
        var startingDirectory = Directory.CreateDirectory(Path.Combine(unrelated.FullName, "output"));

        Assert.AreEqual(_fixtureDirectory, RepositoryPaths.FindRoot(startingDirectory.FullName));
    }

    [TestMethod]
    public void FindRoot_WhenNearerSolutionHasNoSdkMarker_ShouldFindContainingRepository()
    {
        CreateRepositoryMarkers(_fixtureDirectory);
        var unrelated = Directory.CreateDirectory(Path.Combine(_fixtureDirectory, "unrelated"));
        var solutionDirectory = Directory.CreateDirectory(Path.Combine(unrelated.FullName, "src", "dotnet"));
        File.WriteAllText(Path.Combine(solutionDirectory.FullName, "Musoq.sln"), string.Empty);

        Assert.AreEqual(_fixtureDirectory, RepositoryPaths.FindRoot(solutionDirectory.FullName));
    }

    private static void CreateRepositoryMarkers(string root)
    {
        var solutionDirectory = Directory.CreateDirectory(Path.Combine(root, "src", "dotnet"));
        File.WriteAllText(Path.Combine(root, "global.json"), "{}");
        File.WriteAllText(Path.Combine(solutionDirectory.FullName, "Musoq.sln"), string.Empty);
    }
}
