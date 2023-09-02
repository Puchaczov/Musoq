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
        IReadOnlyCollection<dynamic> values,
        IReadOnlyDictionary<string, Type> schema = null)
    {
        schema ??= ((IDictionary<string, object>) values.First()).ToDictionary(f => f.Key, f => f.Value?.GetType());
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(),
            new AnySchemaNameProvider(new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                { "dynamic", (schema, values) }
            }),
            CreateMockedEnvironmentVariables());
    }
    
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        (string Name, IReadOnlyCollection<dynamic> Values, IReadOnlyDictionary<string, Type> Schema)[] sources)
    {
        var schemas = new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>();
        foreach (var source in sources)
        {
            schemas.Add(source.Name, (source.Schema, source.Values));
        }
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(),
            new DynamicSchemaProvider(schemas),
            CreateMockedEnvironmentVariables());
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