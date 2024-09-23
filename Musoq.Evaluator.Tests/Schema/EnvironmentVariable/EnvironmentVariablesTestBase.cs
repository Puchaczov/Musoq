using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesTestBase
{
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IDictionary<string, IEnumerable<EnvironmentVariableEntity>> sources,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new EnvironmentVariablesSchemaProvider(sources),
            positionalEnvironmentVariables ?? CreateMockedEnvironmentVariables());
    }
        
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> basicSources,
        IDictionary<string, IEnumerable<EnvironmentVariableEntity>> environmentSources,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
    {
        var schemas = new Dictionary<string, ISchemaProvider>();
            
        foreach (var basicSource in basicSources)
        {
            schemas.Add(basicSource.Key, new BasicSchemaProvider<BasicEntity>(basicSources));
        }
            
        foreach (var environmentSource in environmentSources)
        {
            schemas.Add(environmentSource.Key, new EnvironmentVariablesSchemaProvider(environmentSources));
        }

        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new MultipleSchemasSchemaProvider(schemas),
            positionalEnvironmentVariables ?? CreateMockedEnvironmentVariables());
    }

    private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    static EnvironmentVariablesTestBase()
    {
        new Plugins.Environment()
            .SetValue(
                Constants.NetStandardDllEnvironmentVariableName, 
                EnvironmentUtils.GetOrCreateEnvironmentVariable());

        Culture.ApplyWithDefaultCulture();
    }
}