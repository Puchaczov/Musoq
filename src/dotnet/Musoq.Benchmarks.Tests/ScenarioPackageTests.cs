using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class ScenarioPackageTests
{
    [TestMethod]
    public void SharedLibrary_UsesItsDedicatedPackageVersion()
    {
        var project = Project("Musoq.Benchmarks.Scenarios");
        Assert.AreEqual("true", project.Descendants("IsPackable").Single().Value);
        Assert.AreEqual("$(MusoqBenchmarksScenariosVersion)", project.Descendants("Version").Single().Value);
        var versions = XDocument.Load(Path.Combine(ProvenanceCollector.RepositoryRoot(), "scripts", "Versions.props"));
        Assert.AreEqual(versions.Descendants("MusoqBenchmarksScenariosVersion").Single().Value, ScenarioCatalog.PackageVersion);
    }

    [TestMethod]
    public void SharedLibrary_HasOnlySchemaAndPluginsRuntimeDependencies()
    {
        var project = Project("Musoq.Benchmarks.Scenarios");
        var actual = project.Descendants("ProjectReference").Select(reference =>
            Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/'))).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Musoq.Schema", "Musoq.Plugins" }, actual);
        Assert.IsEmpty(project.Descendants("PackageReference").Where(reference => reference.Attribute("PrivateAssets")?.Value != "All").ToArray());
    }

    [TestMethod]
    [DataRow("Musoq.Benchmarks")]
    [DataRow("Musoq.Benchmarks.Tests")]
    public void BenchmarkInfrastructure_RemainsNonPackable(string name) =>
        Assert.AreEqual("false", Project(name).Descendants("IsPackable").Single().Value);

    [TestMethod]
    public void MeasuredSuiteIdentity_ComesFromTheSharedLibrary()
    {
        var stamp = MeasuredProcess.Capture();
        Assert.AreEqual(ScenarioCatalog.InformationalVersion, stamp.SuiteInformationalVersion);
        StringAssert.StartsWith(stamp.SuiteInformationalVersion, ScenarioCatalog.PackageVersion);
        StringAssert.Contains(stamp.SuiteInformationalVersion, stamp.Source!.Commit);
    }

    private static XDocument Project(string name) => XDocument.Load(Path.Combine(
        ProvenanceCollector.RepositoryRoot(), "src", "dotnet", name, name + ".csproj"));
}
