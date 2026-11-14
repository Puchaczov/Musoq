using System.Text.Json;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Examples.DataSources.Csv;

namespace Musoq.Benchmarks;

public static class ReadmeComparison
{
    public static void Write(string scriptPath, TextWriter output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);
        ArgumentNullException.ThrowIfNull(output);

        using var query = InstanceCreator.CompileForExecution(
            File.ReadAllText(scriptPath),
            "readme-comparison",
            new CsvSchemaProvider(),
            BenchmarkLogger.Instance,
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var table = query.Run();
        var rows = table.Rows.Select(row => new Comparison(
            row.GetValue<int?>(0) ?? throw new InvalidDataException("Missing comparison sequence."),
            row.GetValue<string>(1),
            row.GetValue<string>(2),
            row.GetValue<double?>(3) ?? throw new InvalidDataException("Missing comparison mean."),
            row.GetValue<double?>(4) ?? throw new InvalidDataException("Missing comparison allocation."),
            row.GetValue<double?>(5),
            row.GetValue<double?>(6)
        ));
        output.WriteLine(JsonSerializer.Serialize(rows));
    }

    private sealed record Comparison(
        int Sequence,
        string VersionLabel,
        string CaseId,
        double MeanNanoseconds,
        double AllocatedBytes,
        double? TimeChangePercent,
        double? AllocationChangePercent
    );
}
