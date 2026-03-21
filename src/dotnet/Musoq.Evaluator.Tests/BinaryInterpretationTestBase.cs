#nullable enable annotations

using System;
using System.Collections.Generic;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

public abstract class BinaryInterpretationTestBase
{
    protected static object CreateAndCompileInterpreter(string schemaName, params FieldDefinitionNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, fields);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    protected static object CompileInterpreter(SchemaRegistry registry, string schemaName)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();
        if (!success)
            throw new InvalidOperationException(
                $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");

        var type = compilationUnit.GetInterpreterType(schemaName);
        if (type == null) throw new InvalidOperationException($"Type '{schemaName}' not found in compiled assembly.");

        return Activator.CreateInstance(type)!;
    }

    protected static object CompileInterpreterForGenericInstantiation(SchemaRegistry registry, string genericSchemaName,
        string[] typeArguments)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();
        if (!success)
            throw new InvalidOperationException(
                $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");


        var genericType = compilationUnit.GetInterpreterType(genericSchemaName);
        if (genericType == null)
            throw new InvalidOperationException($"Generic type '{genericSchemaName}' not found in compiled assembly.");


        var concreteTypes = new Type[typeArguments.Length];
        for (var i = 0; i < typeArguments.Length; i++)
        {
            var argType = compilationUnit.GetInterpreterType(typeArguments[i]);
            if (argType == null)
                throw new InvalidOperationException(
                    $"Type argument '{typeArguments[i]}' not found in compiled assembly.");
            concreteTypes[i] = argType;
        }

        var closedType = genericType.MakeGenericType(concreteTypes);
        return Activator.CreateInstance(closedType)!;
    }

    protected static object InvokeInterpret(object interpreter, byte[] data)
    {
        var interpreterType = interpreter.GetType();

        var interpretMethod = interpreterType.GetMethod("Interpret",
            [typeof(byte[])]);

        if (interpretMethod == null) throw new InvalidOperationException("Interpret(byte[]) method not found");

        return interpretMethod.Invoke(interpreter, [data])!;
    }

    protected static T GetPropertyValue<T>(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found");

        return (T)prop.GetValue(obj)!;
    }

    protected static FieldDefinitionNode CreatePrimitiveField(
        string name,
        PrimitiveTypeName typeName,
        Endianness endianness)
    {
        var typeNode = new PrimitiveTypeNode(typeName, endianness);
        return new FieldDefinitionNode(name, typeNode);
    }

    protected static object CreateAndCompileInterpreterWithSchema(string schemaName, params SchemaFieldNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, fields);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    protected static (bool success, object? result) InvokeTryInterpret(object interpreter, byte[] data)
    {
        try
        {
            var result = InvokeInterpret(interpreter, data);
            return (true, result);
        }
        catch
        {
            return (false, null);
        }
    }

    protected static object InvokePartialInterpret(object interpreter, byte[] data)
    {
        var method = interpreter.GetType().GetMethod("PartialInterpret", [typeof(byte[])]);
        if (method == null)
            throw new InvalidOperationException("PartialInterpret method not found on interpreter");

        return method.Invoke(interpreter, [data])!;
    }
}
