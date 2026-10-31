using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Musoq.Formatter.Tests.Verification;

internal static class DependencyAudit
{
    internal static void VerifyAssets(JsonElement assets, IReadOnlySet<string> permitted, string project)
    {
        foreach (var library in assets.GetProperty("libraries").EnumerateObject())
        {
            var name = library.Name.Split('/')[0];
            if (name.StartsWith("Musoq.", StringComparison.Ordinal) && !permitted.Contains(name))
                throw new InvalidOperationException("Forbidden package/project dependency: " + name);
            if (project == "Formatter" && name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
                throw new InvalidOperationException("Compiler packages must remain test-only.");
        }
    }

    internal static void VerifyEvaluatedInputs(string project, IReadOnlySet<string> permitted)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "VerificationInputs");
        string assets;
        string[] projects;
        if (project == "Tests")
        {
            assets = File.ReadAllLines(Path.Combine(directory, "Tests.assets")).Single();
            projects = File.ReadAllLines(Path.Combine(directory, "Tests.projects"));
            var packages = File.ReadAllLines(Path.Combine(directory, "Tests.packages"));
            if (!packages.Any(package => package.Equals("Microsoft.CodeAnalysis.CSharp|All", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Roslyn must be a private test dependency.");
        }
        else
        {
            using var inputs = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, project + ".json")));
            assets = inputs.RootElement.GetProperty("Properties").GetProperty("ProjectAssetsFile").GetString()!;
            projects = inputs.RootElement.GetProperty("Items").GetProperty("ProjectReference").EnumerateArray()
                .Select(item => item.GetProperty("FullPath").GetString()!).ToArray();
        }
        foreach (var path in projects)
            if (!permitted.Contains(Path.GetFileNameWithoutExtension(path)))
                throw new InvalidOperationException("Forbidden evaluated ProjectReference: " + path);
        using var document = JsonDocument.Parse(File.ReadAllText(assets));
        VerifyAssets(document.RootElement, permitted, project);
    }
}
