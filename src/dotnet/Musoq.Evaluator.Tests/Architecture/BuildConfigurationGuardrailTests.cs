using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class BuildConfigurationGuardrailTests
{
    private static readonly Regex SolutionProjectPath = new(
        "^Project\\(\"\\{[^}]+\\}\"\\) = \"[^\"]+\", \"([^\"]+\\.csproj)\"",
        RegexOptions.Compiled);

    [TestMethod]
    public void SolutionProjectReferences_ShouldPointToExistingProjects()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var solutionPath = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.sln");
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

        var missingProjects = File.ReadLines(solutionPath)
            .Select(line => SolutionProjectPath.Match(line))
            .Where(static match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Where(relativePath => !File.Exists(Path.Combine(solutionDirectory, relativePath)))
            .ToArray();

        Assert.IsEmpty(
            missingProjects,
            "Musoq.sln contains project references that do not exist: " +
            string.Join(", ", missingProjects));
    }

    [TestMethod]
    public void DotNetSdk_ShouldStayPinnedToNet10FeatureBand()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "global.json")));
        var sdk = globalJson.RootElement.GetProperty("sdk");

        Assert.AreEqual("10.0.300", sdk.GetProperty("version").GetString());
        Assert.AreEqual("latestFeature", sdk.GetProperty("rollForward").GetString());
    }

    [TestMethod]
    public void Workflows_ShouldUseNet10Sdk()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var workflowFiles = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, ".github", "workflows"),
                "*.yml",
                SearchOption.TopDirectoryOnly)
            .ToArray();

        var offenders = workflowFiles
            .Select(file => new
            {
                File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                Text = File.ReadAllText(file)
            })
            .Where(entry => entry.Text.Contains("dotnet-version:", StringComparison.Ordinal))
            .Where(entry => !entry.Text.Contains("dotnet-version: '10.0.x'", StringComparison.Ordinal) &&
                            !entry.Text.Contains("dotnet-version: \"10.0.x\"", StringComparison.Ordinal))
            .Select(static entry => entry.File)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Workflows that install .NET must use the 10.0.x SDK feature band: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void Projects_ShouldTargetNet10()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var projectFiles = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "src", "dotnet"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(static file => !IsUnderBuildOutputDirectory(file))
            .ToArray();

        var offenders = projectFiles
            .Select(file => new
            {
                File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                Document = XDocument.Load(file)
            })
            .Select(entry => new
            {
                entry.File,
                Frameworks = entry.Document
                    .Descendants()
                    .Where(static element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
                    .Select(static element => element.Value.Trim())
                    .ToArray()
            })
            .Where(static entry => entry.Frameworks.Length == 0 ||
                                   entry.Frameworks.Any(static framework => framework != "net10.0"))
            .Select(static entry => $"{entry.File}: {string.Join(", ", entry.Frameworks)}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "All projects in src/dotnet must target net10.0: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void DirectoryBuildProps_ShouldTreatWarningsAsErrors()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var props = XDocument.Load(Path.Combine(repositoryRoot, "Directory.Build.props"));
        var treatWarningsAsErrors = props
            .Descendants()
            .Single(static element => element.Name.LocalName == "TreatWarningsAsErrors")
            .Value
            .Trim();

        Assert.AreEqual("true", treatWarningsAsErrors);
    }

    [TestMethod]
    public void CentralPackageManagement_ShouldOwnPackageVersions()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var propsPath = Path.Combine(repositoryRoot, "Directory.Packages.props");
        var props = XDocument.Load(propsPath);
        var centralManagement = props
            .Descendants()
            .Single(static element => element.Name.LocalName == "ManagePackageVersionsCentrally")
            .Value
            .Trim();

        Assert.AreEqual("true", centralManagement);

        var offenders = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "src", "dotnet"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(static file => !IsUnderBuildOutputDirectory(file))
            .Select(file => new
            {
                File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                Document = XDocument.Load(file)
            })
            .SelectMany(entry => entry.Document
                .Descendants()
                .Where(static element => element.Name.LocalName == "PackageReference" &&
                                         element.Attribute("Version") != null)
                .Select(_ => entry.File))
            .Distinct()
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "PackageReference versions must live in Directory.Packages.props: " +
            string.Join(", ", offenders));
    }

    private static bool IsUnderBuildOutputDirectory(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Contains("bin") || parts.Contains("obj");
    }
}
