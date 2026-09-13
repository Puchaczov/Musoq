using System;
using Musoq.Schema.StructuralInputs;
namespace Musoq.Evaluator.IR.Expressions;

internal static class StructuralReferenceExpressionFactory
{
    public static IrExpression CreateParameter(string name, Type returnType, Type? expectedType) =>
        Create(new ScriptParameterRef(name, returnType), expectedType);

    public static IrExpression CreateVariable(string name, Type returnType, Type? expectedType) =>
        Create(new ScriptVariableRef(name, returnType), expectedType);

    private static IrExpression Create(IrExpression reference, Type? expectedType)
    {
        if (expectedType == null || expectedType == typeof(object) || expectedType == typeof(StructuralValue))
            return reference;

        try
        {
            var descriptor = StructuralTypeDescriptor.FromClrType(expectedType);
            return descriptor.Kind is StructuralTypeKind.Record or StructuralTypeKind.Collection
                ? new StructuralConversion(reference, expectedType)
                : reference;
        }
        catch (InvalidOperationException)
        {
            return reference;
        }
    }
}