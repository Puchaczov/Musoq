using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Formatter.Tests.Verification;

internal sealed record CompilerInputs(string[] Sources, string[] References, string[] Defines,
    string Language, string Nullable, string Target)
{
    internal static CompilerInputs Read(string project)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "VerificationInputs");
        if (project == "Tests")
        {
            var options = File.ReadAllLines(Path.Combine(directory, "Tests.options"));
            return new CompilerInputs(ReadLines(directory, "Tests.sources"), ReadLines(directory, "Tests.references"),
                options.Skip(2).ToArray(), options[0], options[1], ReadLines(directory, "Tests.target").Single());
        }
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, project + ".json")));
        var properties = json.RootElement.GetProperty("Properties");
        var items = json.RootElement.GetProperty("Items");
        return new CompilerInputs(Paths(items, "Compile"), Paths(items, "ReferencePath"),
            properties.GetProperty("DefineConstants").GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries),
            properties.GetProperty("LangVersion").GetString()!, properties.GetProperty("Nullable").GetString()!,
            properties.GetProperty("TargetPath").GetString()!);
    }

    internal CSharpCompilation Compile(string name)
    {
        ValidateFiles(Sources.Concat(References).Append(Target));
        if (!LanguageVersionFacts.TryParse(Language, out var language))
            throw new InvalidOperationException("Unknown evaluated C# language version: " + Language);
        var parse = new CSharpParseOptions(language, preprocessorSymbols: Defines);
        var nullable = Nullable switch
        {
            "enable" => NullableContextOptions.Enable,
            "disable" or "" => NullableContextOptions.Disable,
            "annotations" => NullableContextOptions.Annotations,
            "warnings" => NullableContextOptions.Warnings,
            _ => throw new InvalidOperationException("Unknown evaluated nullable setting: " + Nullable)
        };
        return CSharpCompilation.Create(name, Sources.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parse, path)),
            References.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullable));
    }

    private static string[] ReadLines(string directory, string name) => File.ReadAllLines(Path.Combine(directory, name))
        .Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

    private static string[] Paths(JsonElement items, string name) => items.GetProperty(name).EnumerateArray()
        .Select(item => item.GetProperty("FullPath").GetString()!).Distinct(StringComparer.Ordinal).ToArray();

    private static void ValidateFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            if (!File.Exists(path))
                throw new InvalidOperationException("Evaluated verification input does not exist: " + path);
    }
}
