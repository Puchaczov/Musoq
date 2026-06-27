namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    private static GeneratedCodeSample RuntimeV2Regression(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true));
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
        return RuntimeV2BenchmarkMaterializedWithOptions(name, query, new CompilationOptions());
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

    private static GeneratedCodeSample RuntimeV2ScriptParameter(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true)) with
        {
            Category = "Parameters"
        };
    }

    private static GeneratedCodeSample RuntimeV2ScriptVariable(string name, string query)
    {
        return RuntimeV2RegressionWithOptions(
            name,
            query,
            new CompilationOptions(useCommonSubexpressionElimination: true)) with
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
}
