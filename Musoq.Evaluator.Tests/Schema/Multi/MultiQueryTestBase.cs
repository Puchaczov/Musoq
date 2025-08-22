using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchemaTestBase
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
        var schema = new MultiSchema(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>()
        {
            {"first", (new FirstEntityTable(), new MultiRowSource<FirstEntity>(first, FirstEntity.TestNameToIndexMap, FirstEntity.TestIndexToObjectAccessMap))},
            {"second", (new SecondEntityTable(), new MultiRowSource<SecondEntity>(second, SecondEntity.TestNameToIndexMap, SecondEntity.TestIndexToObjectAccessMap))}
        });
        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    private CompiledQuery CreateAndRunVirtualMachine(
        string script,
        ISchema schema,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new MultiSchemaProvider(new Dictionary<string, ISchema>()
            {
                {"@schema", schema}
            }),
            LoggerResolver);
    }
}