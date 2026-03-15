using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericEntityTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static GenericEntityTestBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity>(
        string script,
        TFirstEntity[] first,
        Func<TFirstEntity, bool> filterFirst = null,
        Func<object[], RowSource, RowSource> filter = null
    )
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) }
            }, new Dictionary<string, Func<object[], RowSource, RowSource>>
            {
                { "first", filter }
            });

        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TLibrary>(
        string script,
        TFirstEntity[] first,
        Func<TFirstEntity, bool> filterFirst = null,
        Func<object[], RowSource, RowSource> filter = null
    ) where TLibrary : LibraryBase, new()
    {
        var schema = new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
        {
            { "first", CreateEntitySource(first, filterFirst) }
        }, new Dictionary<string, Func<object[], RowSource, RowSource>>
        {
            { "first", filter }
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
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) },
                { "second", CreateEntitySource(second, filterSecond) }
            }, new Dictionary<string, Func<object[], RowSource, RowSource>>
            {
                { "first", filterFirstRowsSource },
                { "second", filterSecondRowsSource }
            });

        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TSecondEntity, TThirdEntity>(
        string script,
        TFirstEntity[] first,
        TSecondEntity[] second,
        TThirdEntity[] third,
        Func<TFirstEntity, bool> filterFirst = null,
        Func<TSecondEntity, bool> filterSecond = null,
        Func<TThirdEntity, bool> filterThird = null,
        Func<object[], RowSource, RowSource> filterFirstRowsSource = null,
        Func<object[], RowSource, RowSource> filterSecondRowsSource = null,
        Func<object[], RowSource, RowSource> filterThirdRowsSource = null
    )
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) },
                { "second", CreateEntitySource(second, filterSecond) },
                { "third", CreateEntitySource(third, filterThird) }
            }, new Dictionary<string, Func<object[], RowSource, RowSource>>
            {
                { "first", filterFirstRowsSource },
                { "second", filterSecondRowsSource },
                { "third", filterThirdRowsSource }
            });

        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    private CompiledQuery CreateAndRunVirtualMachine(
        string script,
        ISchema schema,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                { "#schema", schema }
            }),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static (ISchemaTable SchemaTable, RowSource RowSource) CreateEntitySource<T>(
        T[] entities, Func<T, bool> filter = null)
    {
        return (new GenericEntityTable<T>(),
            new GenericRowsSource<T>(entities, GenericEntityTable<T>.NameToIndexMap,
                GenericEntityTable<T>.IndexToObjectAccessMap, filter));
    }

    private static IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }
}
