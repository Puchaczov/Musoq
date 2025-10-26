using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesTestBase
{
    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();
    
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IDictionary<uint, IEnumerable<EnvironmentVariableEntity>> sources)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver,
            () =>
            {
                var chain =
                    new CreateTree(
                        new TransformTree(
                            new TurnQueryIntoRunnableCode(null),
                            LoggerResolver
                        )
                    );
                
                return chain;
            }, items =>
            {
                items.CreateBuildMetadataAndInferTypesVisitor = (provider, columns) =>
                    new EnvironmentVariablesBuildMetadataAndInferTypesVisitor(provider, columns, sources, LoggerResolver.ResolveLogger<EnvironmentVariablesBuildMetadataAndInferTypesVisitor>());
            });
    }

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> basicSources,
        IDictionary<string, IEnumerable<EnvironmentVariableEntity>> environmentSources,
        IDictionary<uint, IEnumerable<EnvironmentVariableEntity>> sources)
    {
        var schemas = new Dictionary<string, ISchemaProvider>();
            
        foreach (var basicSource in basicSources)
        {
            schemas.Add(basicSource.Key, new BasicSchemaProvider<BasicEntity>(basicSources));
        }
            
        foreach (var environmentSource in environmentSources)
        {
            schemas.Add(environmentSource.Key, new EnvironmentVariablesSchemaProvider());
        }

        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new MultipleSchemasSchemaProvider(schemas), LoggerResolver, () => new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), LoggerResolver)
            ), 
            items =>
            {
                items.CreateBuildMetadataAndInferTypesVisitor = (provider, columns) =>
                    new EnvironmentVariablesBuildMetadataAndInferTypesVisitor(provider, columns, sources, LoggerResolver.ResolveLogger<EnvironmentVariablesBuildMetadataAndInferTypesVisitor>());
            });
    }

    static EnvironmentVariablesTestBase()
    {
        Culture.ApplyWithDefaultCulture();
    }
}
