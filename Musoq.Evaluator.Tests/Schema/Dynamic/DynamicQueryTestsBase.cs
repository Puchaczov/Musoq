using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Moq;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicQueryTestsBase
{
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyCollection<dynamic> values)
    {
        var schema = ((IDictionary<string, object>) values.First()).ToDictionary(f => f.Key, f => f.Value?.GetType());
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(),
            new DynamicSchemaProvider(schema, values),
            CreateMockedEnvironmentVariables());
    }

    private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    static DynamicQueryTestsBase()
    {
        new Plugins.Environment()
            .SetValue(
                Constants.NetStandardDllEnvironmentName, 
                EnvironmentUtils.GetOrCreateEnvironmentVariable());

        Culture.ApplyWithDefaultCulture();
    }
}