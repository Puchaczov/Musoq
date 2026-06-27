using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public enum GeneratedCodeSampleFormat
{
    GeneratedCodeOnly,
    QueryHeaderAndGeneratedCode
}

public sealed record GeneratedCodeSample
{
    public required string Name { get; init; }

    public required string FileName { get; init; }

    public required string Query { get; init; }

    public required string Category { get; init; }

    public required GeneratedCodeSampleFormat Format { get; init; }

    public required Func<ISchemaProvider> CreateSchemaProvider { get; init; }

    public CompilationOptions CompilationOptions { get; init; } = new();

    public override string ToString()
    {
        return FileName;
    }
}
