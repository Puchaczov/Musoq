using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueQueryTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(string script, IEnumerable<PathValueEntity> entities)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new PathValueSchemaProvider(entities),
            LoggerResolver,
            TestCompilationOptions);
    }

    protected Table RunNativeNumericQuery<T>(string script, IEnumerable<PathValueEntity> entities, bool allowNumericText = false)
        where T : struct
    {
        var rows = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(entities,
            entity => new NativeNumericPathValue<T>(entity.Path, DecodeNumericFixtureValue<T>(entity.Value, allowNumericText),
                DecodeNumericFixtureValue<T>(entity.Value2, allowNumericText))));
        using var compiled = InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString("N"),
            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<NativeNumericPathValue<T>, PathValueLibrary>(rows),
            LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    protected Table RunNativeStringQuery(string script, IEnumerable<PathValueEntity> entities)
    {
        var rows = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(entities,
            entity => new NativeStringPathValue(entity.Path,
                Convert.ToString(entity.Value, System.Globalization.CultureInfo.InvariantCulture))));
        using var compiled = InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString("N"),
            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<NativeStringPathValue, PathValueLibrary>(rows),
            LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    public sealed record NativeStringPathValue(string Path, string? Value);

    protected Table RunNativeMixedQuery(string script, IEnumerable<PathValueEntity> entities)
    {
        var rows = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(entities, entity =>
            new NativeMixedPathValue(entity.Path,
                entity.Value is byte or sbyte or short or ushort or int or uint or long
                    ? Convert.ToInt64(entity.Value, System.Globalization.CultureInfo.InvariantCulture) : null,
                entity.Value is byte or sbyte or short or ushort or int or uint or long or decimal or float or double
                    ? Convert.ToDecimal(entity.Value, System.Globalization.CultureInfo.InvariantCulture) : null,
                entity.Value as string, entity.Value is bool flag ? flag : null)));
        using var compiled = InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString("N"),
            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<NativeMixedPathValue, PathValueLibrary>(rows),
            LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    public sealed record NativeMixedPathValue(string Path, long? Integral, decimal? Amount, string? Text, bool? Flag);

    private static T? DecodeNumericFixtureValue<T>(object? value, bool allowNumericText) where T : struct
    {
        if (value is null || value is string && !allowNumericText)
            return null;
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception error) when (error is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    public sealed record NativeNumericPathValue<T>(string Path, T? Value, T? Value2) where T : struct;

    protected Table RunQuery(string script, IEnumerable<PathValueEntity> entities)
    {
        var vm = CreateAndRunVirtualMachine(script, entities);
        return TableMaterializationTestHelper.Materialize(vm.Run());
    }

    protected class PathValueSchemaProvider(IEnumerable<PathValueEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new PathValueSchema(entities);
        }
    }
}
