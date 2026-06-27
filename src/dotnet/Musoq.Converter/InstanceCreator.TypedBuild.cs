using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Runtime;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static BuildItems BuildTypedItems<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        Func<ILoggerResolver, BuildChain> createBuildChain,
        QueryResultMode resultMode = QueryResultMode.TypedEnumerable,
        bool emitExecutionPlanText = false)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        ArgumentNullException.ThrowIfNull(additionalReferenceTypes);
        ArgumentNullException.ThrowIfNull(createBuildChain);

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.EmitExecutionPlanText = emitExecutionPlanText;
        items.CompilationOptions = compilationOptions;
        items.QueryResultMode = resultMode;
        items.OutputType = typeof(TOut);
        items.AdditionalReferenceTypes = CreateTypedReferenceTypes<TOut>(additionalReferenceTypes);

        RuntimeLibraries.CreateReferences();
        Build(items, createBuildChain(loggerResolver));
        RejectUnsupportedMultiStatementQuery(items.RawQueryTree);

        return items;
    }

    private static Type[] CreateTypedReferenceTypes<TOut>(IEnumerable<Type> additionalReferenceTypes)
    {
        return additionalReferenceTypes
            .Concat([typeof(TOut)])
            .Distinct()
            .ToArray();
    }
}
