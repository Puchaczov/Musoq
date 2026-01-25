using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicQueryTestsBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static DynamicQueryTestsBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyCollection<dynamic> values,
        IReadOnlyDictionary<string, Type> schema = null)
    {
        schema ??= ((IDictionary<string, object>)values.First()).ToDictionary(f => f.Key, f => f.Value?.GetType());
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new AnySchemaNameProvider(
                new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
                {
                    { "dynamic", (schema, values) }
                }),
            LoggerResolver,
            TestCompilationOptions);
    }

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IEnumerable<(string Name, IReadOnlyCollection<dynamic> Values, IReadOnlyDictionary<string, Type> Schema)>
            sources
    )
    {
        var schemas = sources
            .ToDictionary<(string Name, IReadOnlyCollection<dynamic> Values, IReadOnlyDictionary<string, Type> Schema),
                string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>(source => source.Name,
                source => (source.Schema, source.Values));

        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new DynamicSchemaProvider(schemas),
            LoggerResolver,
            TestCompilationOptions);
    }

    protected static ExpandoObject CreateExpandoObject(ExpandoObject complex)
    {
        dynamic obj = new ExpandoObject();
        obj.Complex = complex;
        return obj;
    }

    protected static ExpandoObject CreateExpandoObject(int id)
    {
        dynamic obj = new ExpandoObject();
        obj.Id = id;
        return obj;
    }

    protected static ExpandoObject CreateExpandoObject(int id, string name)
    {
        dynamic obj = new ExpandoObject();
        obj.Id = id;
        obj.Name = name;
        return obj;
    }

    protected static ExpandoObject CreateExpandoObject(int[] array)
    {
        dynamic obj = new ExpandoObject();
        obj.Array = array;
        return obj;
    }

    protected static ExpandoObject CreateExpandoObject(ExpandoObject[] array)
    {
        dynamic obj = new ExpandoObject();
        obj.Array = array;
        return obj;
    }

    protected IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }
}
