using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

public class EnvironmentVariablesBuildMetadataAndInferTypesVisitor(
    ISchemaProvider provider,
    IReadOnlyDictionary<string, string[]> columns,
    IDictionary<string, IEnumerable<EnvironmentVariableEntity>> sources,
    ILogger<EnvironmentVariablesBuildMetadataAndInferTypesVisitor> logger,
    CompilationOptions? compilationOptions = null)
    : BuildMetadataAndInferTypesVisitor(provider, columns, logger, compilationOptions)
{
    public List<Type> PassedSchemaArguments { get; } = [];

    protected override IReadOnlyDictionary<string, string> RetrieveInitialSourceRuntimeSettings(string sourceContextId,
        SchemaFromNode node)
    {
        PassedSchemaArguments.AddRange(node.Parameters.Args.Select(f => f.ReturnType ?? typeof(object)));

        if (sources.TryGetValue(sourceContextId, out var environmentVariables) ||
            sources.TryGetValue("*", out environmentVariables))
        {
            var loadEnvironmentVariables = environmentVariables.ToDictionary(
                x => x.Key,
                x => x.Value);

            InternalSourceRuntimeSettingsBySourceContextId[sourceContextId] = loadEnvironmentVariables;

            return loadEnvironmentVariables;
        }

        return base.RetrieveInitialSourceRuntimeSettings(sourceContextId, node);
    }
}
