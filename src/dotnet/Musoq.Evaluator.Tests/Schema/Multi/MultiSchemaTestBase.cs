using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchemaTestBase : MSTestContextTestBase
{
    static MultiSchemaTestBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        FirstEntity[] first,
        SecondEntity[] second)
    {
        var schema = new MultiSchema(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            {
                "first",
                (new FirstEntityTable(), new MultiRowSource<FirstEntity>(first))
            },
            {
                "second",
                (new SecondEntityTable(), new MultiRowSource<SecondEntity>(second))
            }
        });
        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<string>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    private CompiledQuery CreateAndRunVirtualMachine(
        string script,
        ISchema schema,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? SourceRuntimeSettingsBySourceContextId = null)
    {
        _ = SourceRuntimeSettingsBySourceContextId;

        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new MultiSchemaProvider(new Dictionary<string, ISchema>
            {
                { "#schema", schema }
            }),
            LoggerResolver);
    }
}
