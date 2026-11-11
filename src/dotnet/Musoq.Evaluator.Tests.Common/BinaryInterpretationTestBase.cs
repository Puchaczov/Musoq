using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

public abstract class BinaryInterpretationTestBase
{
    private readonly List<IDisposable> _fixtures = [];

    [TestCleanup]
    public void DisposeInterpreterFixtures()
    {
        foreach (var fixture in _fixtures)
            fixture.Dispose();
        _fixtures.Clear();
    }

    protected T OwnFixture<T>(T fixture) where T : IDisposable
    {
        _fixtures.Add(fixture);
        return fixture;
    }

    protected object CreateAndCompileInterpreter(string schemaName, params FieldDefinitionNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, [.. fields]);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    protected object CompileInterpreter(SchemaRegistry registry, string schemaName)
    {
        return CompileFixture(registry, schemaName, []);
    }

    protected object CompileInterpreterForGenericInstantiation(SchemaRegistry registry, string genericSchemaName,
        string[] typeArguments) => CompileFixture(registry, genericSchemaName, typeArguments);

    private object CompileFixture(SchemaRegistry registry, string schemaName, string[] typeArguments)
    {
        var fixture = new CompiledBinaryFixture(new InterpreterCodeGenerator(registry).GenerateAll(), schemaName, typeArguments);
        _fixtures.Add(fixture);
        return fixture;
    }

    protected static object InvokeInterpret(object interpreter, byte[] data) =>
        ((ICompiledBinaryFixture)interpreter).Interpret(data);

    protected static T GetPropertyValue<T>(object obj, string propertyName) =>
        (T)((IReadOnlyDictionary<string, object?>)obj)[propertyName]!;

    protected static FieldDefinitionNode CreatePrimitiveField(
        string name,
        PrimitiveTypeName typeName,
        Endianness endianness)
    {
        var typeNode = new PrimitiveTypeNode(typeName, endianness);
        return new FieldDefinitionNode(name, typeNode);
    }

    protected object CreateAndCompileInterpreterWithSchema(string schemaName, params SchemaFieldNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, [.. fields]);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    protected static (bool success, object? result) InvokeTryInterpret(object interpreter, byte[] data) =>
        ((ICompiledBinaryFixture)interpreter).TryInterpret(data);

    protected static object InvokePartialInterpret(object interpreter, byte[] data) =>
        ((ICompiledBinaryFixture)interpreter).PartialInterpret(data);
}
