using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ReleasePackagingGuardrailTests
{
    [TestMethod]
    public void InternalTargetProjects_ShouldNotBePackableAndShouldGenerateDocumentation()
    {
        foreach (var project in InternalTargetProjects)
        {
            var text = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "dotnet", project, $"{project}.csproj"));

            StringAssert.Contains(text, "<IsPackable>false</IsPackable>");
            StringAssert.Contains(text, "<GenerateDocumentationFile>true</GenerateDocumentationFile>");
        }
    }

    [TestMethod]
    public void NonReleaseProjects_ShouldNotBePackable()
    {
        foreach (var projectPath in NonReleaseProjectPaths)
        {
            var text = File.ReadAllText(Path.Combine(RepositoryRoot, projectPath));
            StringAssert.Contains(text, "<IsPackable>false</IsPackable>");
        }
    }

    [TestMethod]
    public void ConverterPackage_ShouldBundleOnlyItsInternalTargetAssemblies()
    {
        var converterProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Musoq.Converter.csproj"));

        foreach (var targetProject in InternalTargetProjects)
        {
            StringAssert.Contains(converterProject, $"..\\{targetProject}\\{targetProject}.csproj\" PrivateAssets=\"All\"");
            StringAssert.Contains(converterProject, targetProject);
        }

        StringAssert.Contains(converterProject, "IncludeBundledTargetAssemblies");
        StringAssert.Contains(converterProject, "IncludeBundledTargetAssemblySymbols");
        StringAssert.Contains(converterProject, "Microsoft.CodeAnalysis.CSharp");
        Assert.IsFalse(converterProject.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EvaluatorPackage_ShouldBundleTargetAbstractionsForStandaloneEvaluatorConsumers()
    {
        var evaluatorProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Musoq.Evaluator.csproj"));

        StringAssert.Contains(evaluatorProject, "..\\Musoq.Targets.Abstractions\\Musoq.Targets.Abstractions.csproj\" PrivateAssets=\"All\"");
        StringAssert.Contains(evaluatorProject, "IncludeBundledTargetAbstractions");
        StringAssert.Contains(evaluatorProject, "IncludeBundledTargetAbstractionSymbols");
        Assert.IsFalse(evaluatorProject.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReleaseRegistry_ShouldContainOnlyPublicPackages()
    {
        var registryPath = Path.Combine(RepositoryRoot, "scripts", "release", "packages.json");
        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var packageIds = document.RootElement
            .GetProperty("packages")
            .EnumerateArray()
            .Select(static package => package.GetProperty("packageId").GetString())
            .OrderBy(static packageId => packageId, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "Musoq.Converter",
                "Musoq.Evaluator",
                "Musoq.Parser",
                "Musoq.Plugins",
                "Musoq.Schema"
            },
            packageIds);
    }

    [TestMethod]
    public void PackageSmoke_ShouldValidateBundledTargetAssembliesBeforeConsumerExecution()
    {
        var smokeScript = File.ReadAllText(Path.Combine(RepositoryRoot, "scripts", "release", "Test-PackageSmoke.ps1"));

        StringAssert.Contains(smokeScript, "Test-ConverterPackageContents");
        StringAssert.Contains(smokeScript, "Test-EvaluatorPackageContents");
        StringAssert.Contains(smokeScript, "Test-only portable target files must not be included");
        StringAssert.Contains(smokeScript, "Consumer restore must not resolve internal Musoq.Targets NuGet packages.");
    }

    [TestMethod]
    public async Task ReleaseScripts_ShouldPassBehavioralTests()
    {
        var scriptPath = Path.Combine(RepositoryRoot, "scripts", "release", "Test-ReleaseScripts.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PowerShell release-script tests.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var exitTask = process.WaitForExitAsync();

        try
        {
            await exitTask.WaitAsync(TestSynchronization.DeadlockTimeout);
        }
        catch (TimeoutException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await exitTask.WaitAsync(TestSynchronization.DeadlockTimeout);
            var timedOutOutput = await standardOutputTask.WaitAsync(TestSynchronization.DeadlockTimeout);
            var timedOutError = await standardErrorTask.WaitAsync(TestSynchronization.DeadlockTimeout);
            throw new AssertFailedException(
                $"Release-script tests did not exit within {TestSynchronization.DeadlockTimeout}." +
                $"{Environment.NewLine}{timedOutOutput}{Environment.NewLine}{timedOutError}");
        }

        var output = await Task.WhenAll(standardOutputTask, standardErrorTask)
            .WaitAsync(TestSynchronization.DeadlockTimeout);

        Assert.AreEqual(
            0,
            process.ExitCode,
            $"Release-script tests failed.{Environment.NewLine}{output[0]}{Environment.NewLine}{output[1]}");
    }

    [TestMethod]
    public void ReleaseWorkflows_ShouldSupportBranchSpecificQualification()
    {
        var ciWorkflow = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "ci.yml"));
        var publishWorkflow = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "publish.yml"));

        StringAssert.Contains(ciWorkflow, "- 'release/**'");
        StringAssert.Contains(ciWorkflow, "- 'v*.*.*-alpha.*'");
        StringAssert.Contains(ciWorkflow, "- '*/v*.*.*-alpha.*'");
        Assert.IsFalse(
            ciWorkflow.Contains("- '**'", StringComparison.Ordinal),
            "CI must not run the full validation matrix on every arbitrary branch push.");

        StringAssert.Contains(publishWorkflow, "requires_release_branch");
        StringAssert.Contains(publishWorkflow, "Get-CiQualificationDecision");
        StringAssert.Contains(publishWorkflow, "head_branch");
        StringAssert.Contains(publishWorkflow, "event=push");
        StringAssert.Contains(publishWorkflow, "AddMinutes(90)");
        Assert.IsFalse(
            publishWorkflow.Contains("$attempt -le 3", StringComparison.Ordinal),
            "Publish must wait for tag-triggered alpha CI instead of using the old short retry window.");
    }

    private static string[] InternalTargetProjects { get; } =
    [
        "Musoq.Targets.Abstractions",
        "Musoq.Targets.Execution",
        "Musoq.Targets.Execution.Analysis",
        "Musoq.Targets.CSharpClr"
    ];

    private static string[] NonReleaseProjectPaths { get; } =
    [
        Path.Combine("src", "dotnet", "Musoq.Tests.Common", "Musoq.Tests.Common.csproj"),
        Path.Combine("src", "dotnet", "Musoq.Playground", "Musoq.Playground.csproj"),
        Path.Combine("src", "dotnet", "Musoq.Benchmarks", "Musoq.Benchmarks.csproj"),
        Path.Combine("src", "dotnet", "examples", "data-sources", "csv", "Musoq.Examples.DataSources.Csv.csproj"),
        Path.Combine("src", "dotnet", "examples", "data-sources", "git", "Musoq.Examples.DataSources.Git.csproj")
    ];

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "scripts", "release", "packages.json")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root was not found.");
        }
    }
}
