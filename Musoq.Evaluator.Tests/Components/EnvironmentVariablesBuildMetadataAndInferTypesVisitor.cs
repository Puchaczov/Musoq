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
    IDictionary<uint, IEnumerable<EnvironmentVariableEntity>> sources,
    ILogger<EnvironmentVariablesBuildMetadataAndInferTypesVisitor> logger)
    : BuildMetadataAndInferTypesVisitor(provider, columns, logger)
{
    public List<Type> PassedSchemaArguments { get; private set; } = new();
    
    protected override IReadOnlyDictionary<string, string> RetrieveEnvironmentVariables(uint position, SchemaFromNode node)
    {
        PassedSchemaArguments.AddRange(node.Parameters.Args.Select(f => f.ReturnType));
        
        if (sources.TryGetValue(position, out var environmentVariables))
        {
            var loadEnvironmentVariables = environmentVariables.ToDictionary(
                x => x.Key,
                x => x.Value);
            
            InternalPositionalEnvironmentVariables.Add(position, loadEnvironmentVariables);
            
            return loadEnvironmentVariables;
        }
        
        return base.RetrieveEnvironmentVariables(position, node);
    }
}