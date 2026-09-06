using System.Reflection;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static ITableRunnable CreateRunnableForDebug(BuildItems items, Func<Assembly> loadAssembly)
    {
        return CreateRunnable(items, loadAssembly);
    }

    private static BuildItems CreateBuildItems(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        DiagnosticContext diagnosticContext,
        CancellationToken cancellationToken = default)
    {
        return new BuildItems
        {
            CancellationToken = cancellationToken,
            SchemaProvider = schemaProvider,
            RawQuery = script,
            AssemblyName = assemblyName,
            ExecutionTarget = ExecutionTargetIds.CSharpClr,
            CreateBuildMetadataAndInferTypesVisitor = null,
            DiagnosticContext = diagnosticContext
        };
    }

    private static void Build(BuildItems items, BuildChain chain)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(chain);
        items.CancellationToken.ThrowIfCancellationRequested();
        try
        {
            chain.Build(items);
        }
        catch
        {
            items.CancellationToken.ThrowIfCancellationRequested();
            throw;
        }

        items.CancellationToken.ThrowIfCancellationRequested();
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
