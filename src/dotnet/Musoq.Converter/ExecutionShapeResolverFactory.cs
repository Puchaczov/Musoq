using System.Collections.Generic;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema;

namespace Musoq.Converter;

internal static class ExecutionShapeResolverFactory
{
    internal static ExecutionShapeResolver Create(SemanticBuildArtifacts semantic, SchemaRegistry? registry) =>
        new(semantic.ScopeArtifact.CreateScope(),
            semantic.PipelineInferredColumns ?? new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal),
            schemaRegistry: registry,
            scriptVariables: semantic.ScriptVariableDefinitions,
            scriptParameters: semantic.ScriptParameterDefinitions);
}
