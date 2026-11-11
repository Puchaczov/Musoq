namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionGeneratedTypeName
{
    internal static string? For(ExecutionExpression expression)
    {
        if (expression.InterpretationShape is { } interpretation)
            return interpretation.TypeName;
        if (ByteBufferViewShape.IsView(expression) || expression is ExecutionVariableRead { Variable.GeneratedRowTypeName: ByteBufferViewShape.TypeName })
            return ByteBufferViewShape.TypeName;
        if (!GeneratedScalarGenericBinding.RequiresGeneratedSpecialization(expression.ReturnType.ResolveClrType()))
            return null;
        return expression switch
        {
            ExecutionFieldRead field => field.GeneratedTypeName,
            ExecutionVariableRead variable => variable.Variable.GeneratedRowTypeName,
            ExecutionMethodCall method => method.GeneratedResultTypeName,
            ExecutionStructuralRecord record => record.GeneratedTypeName,
            ExecutionScriptParameterRead parameter => parameter.GeneratedTypeName,
            ExecutionScriptVariableRead variable => variable.GeneratedTypeName,
            _ => null
        };
    }
}
