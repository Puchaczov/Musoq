using System;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private QueryInspectionResult CreateInspection()
    {
        return Inspect("select d.Dummy from #system.dual() d");
    }

    private QueryInspectionResult Inspect(string query, CompilationOptions? compilationOptions = null)
    {
        return Inspect(query, _schemaProvider, compilationOptions);
    }

    private QueryInspectionResult Inspect(
        string query,
        ISchemaProvider schemaProvider,
        CompilationOptions? compilationOptions = null)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            compilationOptions);
    }

    private CompiledQuery CompileForExecution(string query, CompilationOptions? compilationOptions = null)
    {
        return CompileForExecution(query, _schemaProvider, compilationOptions);
    }

    private CompiledQuery CompileForExecution(
        string query,
        ISchemaProvider schemaProvider,
        CompilationOptions? compilationOptions = null)
    {
        return compilationOptions == null
            ? InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver)
            : InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                compilationOptions);
    }

    private string GetGeneratedCSharpCode(string query)
    {
        return InstanceCreator.GetGeneratedCSharpCode(
            query,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
    }
}
