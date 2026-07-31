using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Components;
using Musoq.Plugins;
using Musoq.Schema;
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

    private readonly object _batchedQueryGate = new();
    private readonly List<CompiledQuery> _batchedQueries = [];

    [TestCleanup]
    public void DisposeBatchedCompiledQueries()
    {
        CompiledQuery[] queries;
        lock (_batchedQueryGate)
        {
            queries = _batchedQueries.ToArray();
            _batchedQueries.Clear();
        }

        foreach (var query in queries)
            query.Dispose();
    }

    protected static T RequireParameter<T>(object?[] parameters, int index)
    {
        if (parameters[index] is T value)
            return value;

        throw new AssertFailedException(
            $"Expected parameter {index} to be {typeof(T).Name}, but got {parameters[index]?.GetType().Name ?? "null"}.");
    }

    protected BuildItems CreateBuildItems<TFirstEntity, TSecondEntity, TThirdEntity>(
        string script,
        TFirstEntity[] first,
        TSecondEntity[] second,
        TThirdEntity[] third,
        Func<TFirstEntity, bool>? filterFirst = null,
        Func<TSecondEntity, bool>? filterSecond = null,
        Func<TThirdEntity, bool>? filterThird = null,
        Func<object?[], RowSourceFilterInput, object?>? filterFirstRowsSource = null,
        Func<object?[], RowSourceFilterInput, object?>? filterSecondRowsSource = null,
        Func<object?[], RowSourceFilterInput, object?>? filterThirdRowsSource = null)
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) },
                { "second", CreateEntitySource(second, filterSecond) },
                { "third", CreateEntitySource(third, filterThird) }
            },
            new Dictionary<string, Func<object?[], RowSourceFilterInput, object?>?>
            {
                { "first", filterFirstRowsSource },
                { "second", filterSecondRowsSource },
                { "third", filterThirdRowsSource }
            });

        return CreateBuildItems(script, schema);
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity>(
        string script,
        TFirstEntity[] first,
        Func<TFirstEntity, bool>? filterFirst = null,
        Func<object?[], RowSourceFilterInput, object?>? filter = null
    )
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) }
            }, new Dictionary<string, Func<object?[], RowSourceFilterInput, object?>?>
            {
                { "first", filter }
            });

        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TLibrary>(
        string script,
        TFirstEntity[] first,
        Func<TFirstEntity, bool>? filterFirst = null,
        Func<object?[], RowSourceFilterInput, object?>? filter = null
    ) where TLibrary : LibraryBase, new()
    {
        var schema = new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            { "first", CreateEntitySource(first, filterFirst) }
        }, new Dictionary<string, Func<object?[], RowSourceFilterInput, object?>?>
        {
            { "first", filter }
        });

        return CreateAndRunVirtualMachine(script, schema, CreateMockedEnvironmentVariables());
    }

    protected CompiledQuery CreateAndRunVirtualMachine<TFirstEntity, TSecondEntity>(
        string script,
        TFirstEntity[] first,
        TSecondEntity[] second,
        Func<TFirstEntity, bool>? filterFirst = null,
        Func<TSecondEntity, bool>? filterSecond = null,
        Func<object?[], RowSourceFilterInput, object?>? filterFirstRowsSource = null,
        Func<object?[], RowSourceFilterInput, object?>? filterSecondRowsSource = null
    )
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) },
                { "second", CreateEntitySource(second, filterSecond) }
            }, new Dictionary<string, Func<object?[], RowSourceFilterInput, object?>?>
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
        Func<TFirstEntity, bool>? filterFirst = null,
        Func<TSecondEntity, bool>? filterSecond = null,
        Func<TThirdEntity, bool>? filterThird = null,
        Func<object?[], RowSourceFilterInput, object?>? filterFirstRowsSource = null,
        Func<object?[], RowSourceFilterInput, object?>? filterSecondRowsSource = null,
        Func<object?[], RowSourceFilterInput, object?>? filterThirdRowsSource = null
    )
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "first", CreateEntitySource(first, filterFirst) },
                { "second", CreateEntitySource(second, filterSecond) },
                { "third", CreateEntitySource(third, filterThird) }
            }, new Dictionary<string, Func<object?[], RowSourceFilterInput, object?>?>
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
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? SourceRuntimeSettingsBySourceContextId = null)
    {
        _ = SourceRuntimeSettingsBySourceContextId;

        var result = StableTypedExecutionCompilationCoordinator.Submit(
            script,
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                { "#schema", schema }
            }),
            LoggerResolver,
            TestCompilationOptions);

        if (!result.Result.Succeeded)
        {
            throw result.Result.CaughtException != null
                ? new MusoqQueryException(result.Result.ToEnvelopes(), result.Result.CaughtException)
                : new MusoqQueryException(result.Result.ToEnvelopes());
        }

        if (result.WasBatched)
        {
            lock (_batchedQueryGate)
                _batchedQueries.Add(result.Result.CompiledQuery);
        }

        return result.Result.CompiledQuery;
    }

    private BuildItems CreateBuildItems(string script, ISchema schema)
    {
        return InstanceCreator.CreateForAnalyze(
            script,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                { "#schema", schema }
            }),
            LoggerResolver);
    }

    private static (ISchemaTable SchemaTable, object RowSource) CreateEntitySource<T>(
        T[] entities, Func<T, bool>? filter = null)
    {
        return (new GenericEntityTable<T>(),
            new GenericChunkSource<T>(entities, GenericEntityTable<T>.NameToIndexMap,
                GenericEntityTable<T>.IndexToObjectAccessMap, filter));
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<string>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }
}
