using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericEntityTestBase
{
    static GenericEntityTestBase()
    {
        new Plugins.Environment().SetValue(Constants.NetStandardDllEnvironmentVariableName, EnvironmentUtils.GetOrCreateEnvironmentVariable());

        Culture.ApplyWithDefaultCulture();
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity>(
        string script,
        TFirstEntity[] first,
        Func<TFirstEntity, bool> filter = null 
    )
    {
        
        var schema = new GenericSchema<GenericLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>()
        {
            {"first", (new GenericEntityTable<TFirstEntity>(), new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap, GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filter))}
        });
        
        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TSecondEntity>(
        string script,
        TFirstEntity[] first,
        TSecondEntity[] second,
        Func<TFirstEntity, bool> filterFirst = null,
        Func<TSecondEntity, bool> filterSecond = null,
        Func<object[], RowSource, RowSource> filterFirstRowsSource = null,
        Func<object[], RowSource, RowSource> filterSecondRowsSource = null
    )
    {
        
        var schema = new GenericSchema<GenericLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
        {
            {"first", (new GenericEntityTable<TFirstEntity>(), new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap, GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filterFirst))},
            {"second", (new GenericEntityTable<TSecondEntity>(), new GenericRowsSource<TSecondEntity>(second, GenericEntityTable<TSecondEntity>.NameToIndexMap, GenericEntityTable<TSecondEntity>.IndexToObjectAccessMap, filterSecond))}
        }, new Dictionary<string, Func<object[], RowSource, RowSource>>()
        {
            {"first", filterFirstRowsSource},
            {"second", filterSecondRowsSource}
        });
        
        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TSecondEntity, TThirdEntity>(
        string script,
        TFirstEntity[] first,
        TSecondEntity[] second,
        TThirdEntity[] third,
        Func<TFirstEntity, bool> filterFirst = null ,
        Func<TSecondEntity, bool> filterSecond = null,
        Func<TThirdEntity, bool> filterThird = null
    )
    {
        
        var schema = new GenericSchema<GenericLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>()
        {
            {"first", (new GenericEntityTable<TFirstEntity>(), new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap, GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap))},
            {"second", (new GenericEntityTable<TSecondEntity>(), new GenericRowsSource<TSecondEntity>(second, GenericEntityTable<TSecondEntity>.NameToIndexMap, GenericEntityTable<TSecondEntity>.IndexToObjectAccessMap))},
            {"third", (new GenericEntityTable<TThirdEntity>(), new GenericRowsSource<TThirdEntity>(third, GenericEntityTable<TThirdEntity>.NameToIndexMap, GenericEntityTable<TThirdEntity>.IndexToObjectAccessMap))}
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
            new GenericSchemaProvider(new Dictionary<string, ISchema>()
            {
                {"#schema", schema}
            }),
            positionalEnvironmentVariables ?? CreateMockedEnvironmentVariables());
    }
}