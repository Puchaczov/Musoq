using System.Collections.Generic;
using Musoq.Evaluator.Visitors;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private BuildMetadataAndInferTypesVisitor CreateMetadataVisitor(
        TransformPipelineContext context,
        IReadOnlyDictionary<string, string[]> columns)
    {
        var visitor = context.CreateBuildMetadataAndInferTypesVisitor?.Invoke(
            context.SchemaProvider, columns, context.CompilationOptions, context.SchemaRegistry, loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>()) ??
               new BuildMetadataAndInferTypesVisitor(
                   context.SchemaProvider,
                   columns,
                   loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>(),
                   context.DiagnosticContext,
                   context.CompilationOptions,
                   context.SchemaRegistry,
                   context.CancellationToken);
        visitor.SetCancellationToken(context.CancellationToken);
        return visitor;
    }
}
