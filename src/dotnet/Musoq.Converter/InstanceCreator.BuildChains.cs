using System.Reflection;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Runtime;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static ITableRunnable CreateRunnableForDebug(BuildItems items, Func<Assembly> loadAssembly)
    {
        return CreateRunnable(items, loadAssembly);
    }

    private static BuildItems CreateBuildItems(string script, string assemblyName, ISchemaProvider schemaProvider, DiagnosticContext diagnosticContext)
    {
        return new BuildItems
        {
            SchemaProvider = schemaProvider,
            RawQuery = script,
            AssemblyName = assemblyName,
            CreateBuildMetadataAndInferTypesVisitor = null,
            DiagnosticContext = diagnosticContext
        };
    }

    private static void Build(BuildItems items, BuildChain chain)
    {
        RuntimeLibraries.CreateReferences();
        chain.Build(items);
    }

    private static CreateTree CreateExecutableBuildChain(ILoggerResolver loggerResolver)
    {
        return new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), loggerResolver)));
    }

    private static CreateTree CreateInspectionBuildChain(ILoggerResolver loggerResolver)
    {
        return new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TerminalBuildChain(), loggerResolver)));
    }
}
