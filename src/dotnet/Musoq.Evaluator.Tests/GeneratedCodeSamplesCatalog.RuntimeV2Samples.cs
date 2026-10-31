using System;
using System.Linq;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    private static GeneratedCodeSample RuntimeV2Regression(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true)
                .WithStabilityAwareScalarReuse());
    }

    private static GeneratedCodeSample RuntimeV2RegressionWithOptions(
        string name,
        string query,
        CompilationOptions compilationOptions)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "RuntimeV2",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateRuntimeV2RegressionSchemaProvider,
            CompilationOptions = compilationOptions
        };
    }

    private static GeneratedCodeSample RuntimeV2BenchmarkMaterialized(string name, string query)
    {
        return RuntimeV2BenchmarkMaterializedWithOptions(
            name,
            query,
            new CompilationOptions().WithStabilityAwareScalarReuse());
    }

    private static GeneratedCodeSample RuntimeV2BenchmarkMaterializedWithOptions(
        string name,
        string query,
        CompilationOptions compilationOptions)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "RuntimeV2",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateBenchmarkParitySchemaProvider,
            CompilationOptions = compilationOptions.WithTableResultMaterialization()
        };
    }

    private static GeneratedCodeSample RuntimeV2WeatherSingleAggregate()
    {
        return new GeneratedCodeSample
        {
            Name = "Q185_RuntimeV2WeatherSingleAggregate",
            FileName = "Q185_RuntimeV2WeatherSingleAggregate.cs",
            Query = @"select Min(Temperature::Single) as MinTemperature,
                         Max(Temperature::Single) as MaxTemperature,
                         Avg(Temperature::Single) as AvgTemperature
                  from #weather.measurements()
                  group by City",
            Category = "RuntimeV2",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateWeatherMeasurementSchemaProvider,
            CompilationOptions = new CompilationOptions(useCommonSubexpressionElimination: true)
                .WithStabilityAwareScalarReuse(false)
        };
    }

    private static GeneratedCodeSample RuntimeV2ScriptParameter(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true)
                .WithStabilityAwareScalarReuse()) with
        {
            Category = "Parameters"
        };
    }

    private static GeneratedCodeSample RuntimeV2ScriptVariable(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true)
                .WithStabilityAwareScalarReuse()) with
        {
            Category = "Variables"
        };
    }

    private static GeneratedCodeSample ScriptParameterSourceArgument()
    {
        return new GeneratedCodeSample
        {
            Name = "Q122_ScriptParameterSourceArgument",
            FileName = "Q122_ScriptParameterSourceArgument.cs",
            Query = @"param(key: string = 'KEY_1')
                      select Key, Value
                      from #parameterized.items($key)",
            Category = "Parameters",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateScriptParameterSampleSchemaProvider
        };
    }

    private static GeneratedCodeSample ScriptVariableSourceArgument()
    {
        return new GeneratedCodeSample
        {
            Name = "Q132_ScriptVariableSourceArgument",
            FileName = "Q132_ScriptVariableSourceArgument.cs",
            Query = @"let prefix: string = 'KEY'
                      let key: string = $prefix + '_1'
                      select Key, Value
                      from #parameterized.items($key)",
            Category = "Variables",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateScriptParameterSampleSchemaProvider
        };
    }

    private const int BenchmarkSampleInputCount = 100_000;
    private const int BenchmarkSampleParallelism = 24;

    private static GeneratedCodeSample[] CreateBenchmarkSamples() =>
    [
        .. CreateBenchmarkRelationalSamples(),
        .. CreateBenchmarkInterpretationSamples(),
        .. CreateBenchmarkCompositionSamples()
    ];

    private static GeneratedCodeSample[] CreateBenchmarkRelationalSamples()
    {
        const string projectionQuery = """
        select r.Id as Id, r.Name as Name from bench.rows() r order by r.Id
        """;
        const string windowQuery = """
        select r.Id as Id, Sum(r.Amount) over (order by r.Id rows between 1 preceding and current row) as Total, FirstValue(r.Amount) over (order by r.Id rows between 1 preceding and current row) as First
        from bench.rows() r
        order by r.Id
        """;
        return
        [
            BenchmarkSample("B01_Scan", projectionQuery),
            BenchmarkSample("B01_Scan", projectionQuery, outputType: typeof(BenchmarkProjectionOutput)),
            BenchmarkSample("B16_RowsFrame", windowQuery),
            BenchmarkSample("B16_RowsFrame", windowQuery, outputType: typeof(BenchmarkWindowOutput)),
            CreateBenchmarkWideProjection(),
            BenchmarkSample("B13_Apply", """
                select r.Id as Id, i.Value as Value, i.Ordinal as Position
                from bench.rows() r
                cross apply r.Items i with ordinality
                order by r.Id, Position
                """),
            BenchmarkSample("B22_Recursive", """
                with recursive walk as (select r.Id as Id
                from bench.rows() r
                where r.Id = 0
                union all select e.Id
                from walk w
                inner join bench.rows() e on e.Id = w.Id + 1) select Id
                from walk
                order by Id
                """,
                new CompilationOptions(maxDegreeOfParallelismOverride: BenchmarkSampleParallelism)
                    .WithRecursiveCteLimits(new RecursiveCteExecutionLimits(maxIterations: BenchmarkSampleInputCount)))
        ];
    }

    private static GeneratedCodeSample CreateBenchmarkWideProjection()
    {
        const int columnCount = 64;
        var columns = Enumerable.Range(0, columnCount).Select(index => $"r.Amount + {index}d as C{index}");
        var query = "select r.Id as Id, " + string.Join(", ", columns) + " from bench.rows() r order by r.Id";
        return BenchmarkSample("B01_Wide", query);
    }

    private static GeneratedCodeSample[] CreateBenchmarkInterpretationSamples() =>
    [
        BenchmarkSample("B31_TextCaptures", """
            text Captured {
                Fields: pattern r'(?<Level>[A-Z]+)\|(?<Number>[0-9]+)\|(?<Label>.*)' capture (Level, Number, Label)
            };
            select r.Id as Id, p.Fields.Label as Label
            from bench.rows() r
            cross apply Parse<Captured>(r.Text) p
            order by r.Id
            """),
        BenchmarkSample("B32_Partial", """
            binary Header {
                Kind: byte,
                Value: int le,
                Count: byte,
                Tail: byte[Count]
            };
            select r.Id as Id, p.ParsedFields.Kind as Kind, p.BytesConsumed as Consumed, p.ErrorField as Error
            from bench.rows() r
            cross apply PartialInterpret<Header>(r.Truncated) p
            order by r.Id
            """),
        BenchmarkSample("B32_TryParse", """
            text Valid { Prefix: literal 'INFO|', Message: rest }; select r.Id as Id, p.Message as Message
            from bench.rows() r
            outer apply TryParse<Valid>(case when r.Id % 2 = 0 then r.Text else 'bad' end) p
            order by r.Id
            """),
        BenchmarkSample("B30_TextDelimited", """
            text Entry { Level: until '|', Number: until '|', Label: rest }; select r.Id as Id, p.Level as Level, p.Label as Label
            from bench.rows() r
            cross apply Parse<Entry>(r.Text) p
            order by r.Id
            """)
    ];

    private static GeneratedCodeSample[] CreateBenchmarkCompositionSamples()
    {
        const int malformedChunkTerminationSteps = 2;
        var chunkOptions = new CompilationOptions(usePrimitiveTypeValidation: false,
                maxDegreeOfParallelismOverride: BenchmarkSampleParallelism)
            .WithRecursiveCteLimits(new RecursiveCteExecutionLimits(
                maxIterations: BenchmarkSampleInputCount + malformedChunkTerminationSteps));
        return
        [
            BenchmarkSample("X05_CollectionPivot", """
                with expanded as (select r.Key as Id, i.Ordinal as Bucket, r.Amount as Amount
                from bench.rows() r
                outer apply r.Items i with ordinality
                left join bench.lookup() d on i.Value = d.Id) pivot expanded e on e.Bucket in (0 as Even, 1 as Odd) using Sum(e.Amount) as Total
                group by e.Id
                order by Id
                """),
            BenchmarkSample("X08_PacketAnalytics", """
                binary Body { Value: int le };
                binary Packet {
                    Kind: byte,
                    Body: substream[4] as Body exact,
                    Count: byte,
                    Tail: byte[Count]
                };
                text Entry { Level: until '|', Number: until '|', Label: rest }; select t.Level as Level, Sum(p.Body.Value::long) as Total, Count(*) as Count
                from bench.rows() r
                cross apply Interpret<Packet>(r.Payload) p
                cross apply p.Tail b
                cross apply Parse<Entry>(r.Text) t
                inner join bench.lookup() d on r.Key = d.Key
                group by t.Level
                order by t.Level
                """),
            BenchmarkSample("X09_RecursiveChunks", """
                binary Header {
                    Kind: byte,
                    Value: int le,
                    Count: byte,
                    Tail: byte[Count]
                };
                with recursive walk (Bytes, Off, Used, Error) as (select r.Payload, 0, 0, null
                from bench.stream() r
                union all select w.Bytes, w.Off + w.Used, p.BytesConsumed, p.ErrorField
                from walk w
                cross apply PartialInterpret<Header>(FromBase64(ToBase64(w.Bytes, w.Off + w.Used, Length(w.Bytes) - w.Off - w.Used))) p
                where w.Off + w.Used < Length(w.Bytes) and w.Error is null) select Off as Id, Used as Consumed, Error
                from walk
                order by Id, Consumed
                """, chunkOptions)
        ];
    }

    private static GeneratedCodeSample BenchmarkSample(string scenarioId, string query,
        CompilationOptions? compilationOptions = null, Type? outputType = null)
    {
        var resultApi = outputType is null ? "Table" : "Typed";
        var name = $"{scenarioId}_Benchmark{resultApi}";
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = name + ".cs",
            Query = query,
            Category = "OfficialBenchmark",
            Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
            CreateSchemaProvider = () => new BenchmarkSampleSchemaProvider(BenchmarkSampleInputCount),
            CompilationOptions = compilationOptions ?? new CompilationOptions(
                maxDegreeOfParallelismOverride: BenchmarkSampleParallelism),
            OutputType = outputType
        };
    }
}
