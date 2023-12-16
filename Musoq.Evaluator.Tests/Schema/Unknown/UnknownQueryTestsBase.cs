using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Plugins;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownQueryTestsBase
{
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyCollection<dynamic> values)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(),
            new UnknownSchemaProvider(values),
            CreateMockedEnvironmentVariables());
    }

    private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    static UnknownQueryTestsBase()
    {
        new Plugins.Environment()
            .SetValue(
                Constants.NetStandardDllEnvironmentName, 
                EnvironmentUtils.GetOrCreateEnvironmentVariable());

        Culture.ApplyWithDefaultCulture();
    }
}