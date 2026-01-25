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
                {
                    "first",
                    (new GenericEntityTable<TFirstEntity>(),
                        new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap,
                            GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filterFirst))
                }
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
            {
                "first",
                (new GenericEntityTable<TFirstEntity>(),
                    new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap,
                        GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filterFirst))
            }
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
                {
                    "first",
                    (new GenericEntityTable<TFirstEntity>(),
                        new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap,
                            GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filterFirst))
                },
                {
                    "second",
                    (new GenericEntityTable<TSecondEntity>(),
                        new GenericRowsSource<TSecondEntity>(second, GenericEntityTable<TSecondEntity>.NameToIndexMap,
                            GenericEntityTable<TSecondEntity>.IndexToObjectAccessMap, filterSecond))
                }
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
                {
                    "first",
                    (new GenericEntityTable<TFirstEntity>(),
                        new GenericRowsSource<TFirstEntity>(first, GenericEntityTable<TFirstEntity>.NameToIndexMap,
                            GenericEntityTable<TFirstEntity>.IndexToObjectAccessMap, filterFirst))
                },
                {
                    "second",
                    (new GenericEntityTable<TSecondEntity>(),
                        new GenericRowsSource<TSecondEntity>(second, GenericEntityTable<TSecondEntity>.NameToIndexMap,
                            GenericEntityTable<TSecondEntity>.IndexToObjectAccessMap, filterSecond))
                },
                {
                    "third",
                    (new GenericEntityTable<TThirdEntity>(),
                        new GenericRowsSource<TThirdEntity>(third, GenericEntityTable<TThirdEntity>.NameToIndexMap,
                            GenericEntityTable<TThirdEntity>.IndexToObjectAccessMap, filterThird))
                }
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

    private static IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }
}
