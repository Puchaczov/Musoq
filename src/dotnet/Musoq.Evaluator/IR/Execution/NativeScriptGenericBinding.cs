using Musoq.Evaluator.IR.Expressions;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Specializes calls after native script storage has been assigned concrete carriers.</summary>
internal sealed class NativeScriptGenericBinding(NativeScriptValueTypes values) : ExecutionIrRewriter
{
    public override ExecutionPlan RewritePlan(ExecutionPlan plan)
        => values.HasGeneratedStorage || values.InlineRecords.Carriers.Any()
            ? base.RewritePlan(plan) : plan;

    protected override ExecutionExpression RewriteScriptParameterRead(ExecutionScriptParameterRead read)
        => read with { GeneratedTypeName = values.ResolveGeneratedTypeName(
            new ScriptParameterRef(read.Name, read.ReturnType.ResolveClrType())) };

    protected override ExecutionExpression RewriteScriptVariableRead(ExecutionScriptVariableRead read)
        => read with { GeneratedTypeName = values.ResolveGeneratedTypeName(
            new ScriptVariableRef(read.Name, read.ReturnType.ResolveClrType())) };

    protected override ExecutionExpression RewriteMethodCall(ExecutionMethodCall expression)
    {
        var call = (ExecutionMethodCall)base.RewriteMethodCall(expression);
        var method = call.Method.ResolveClrMethod();
        var bindings = GeneratedScalarGenericBinding.Resolve(method, call.Arguments);
        if (method.IsConstructedGenericMethod)
            foreach (var parameter in method.GetGenericMethodDefinition().GetGenericArguments())
                if (bindings.TryGetValue(parameter.GenericParameterPosition, out var name))
                    ValidateRecordConstraints(method.Name, parameter, name);
        return call with
        {
            GeneratedGenericArguments = bindings,
            GeneratedResultTypeName = GeneratedScalarGenericBinding.ResolveResult(method, bindings)
        };
    }
    private void ValidateRecordConstraints(string methodName, Type parameter, string name)
    {
        if (!values.IsRecordCarrier(name))
            return;
        if ((parameter.GenericParameterAttributes & System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            throw new Musoq.Evaluator.Exceptions.CannotResolveMethodException(
                $"Scalar '{methodName}' requires a reference type for '{parameter.Name}', but its generated record carrier is a value type.");
        if (name.EndsWith('?') &&
            (parameter.GenericParameterAttributes & System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            throw new Musoq.Evaluator.Exceptions.CannotResolveMethodException(
                $"Scalar '{methodName}' requires a non-nullable value type for '{parameter.Name}'.");
        if (parameter.GetCustomAttributesData().Any(static attribute =>
                attribute.AttributeType == typeof(System.Runtime.CompilerServices.IsUnmanagedAttribute)) &&
            !values.IsUnmanagedRecordCarrier(name))
            throw new Musoq.Evaluator.Exceptions.CannotResolveMethodException(
                $"Scalar '{methodName}' requires unmanaged fields for '{parameter.Name}', but its generated record contains managed fields.");
    }
}
