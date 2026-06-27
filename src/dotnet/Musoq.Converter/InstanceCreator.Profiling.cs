using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static QueryProfileResult Profile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return Profile(script, assemblyName, schemaProvider, loggerResolver, null, CancellationToken.None);
    }

    public static QueryProfileResult Profile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken token)
    {
        return Profile(script, assemblyName, schemaProvider, loggerResolver, null, token);
    }

    public static QueryProfileResult Profile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions)
    {
        return Profile(script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);
    }

    public static QueryProfileResult Profile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CancellationToken token)
    {
        return CompileForProfile(script, assemblyName, schemaProvider, loggerResolver, compilationOptions)
            .RunWithProfile(token);
    }

    public static CompiledQuery CompileForProfile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForProfile(script, assemblyName, schemaProvider, loggerResolver, null);
    }

    public static CompiledQuery CompileForProfile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions)
    {
        return CompileForExecution(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            CreateProfileCompilationOptions(compilationOptions));
    }

    private static CompilationOptions CreateProfileCompilationOptions(CompilationOptions? compilationOptions)
    {
        var options = compilationOptions ?? new CompilationOptions();

        return options.InstrumentationMode == QueryInstrumentationMode.Disabled
            ? options.WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries)
            : options;
    }
}
