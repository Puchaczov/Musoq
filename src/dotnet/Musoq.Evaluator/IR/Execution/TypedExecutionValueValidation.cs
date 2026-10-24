using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal static class TypedExecutionValueValidation
{
    public static void Validate(ExecutionPlan plan, DiagnosticContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportedFields = new HashSet<(string Name, string Unresolved)>();
        foreach (var shape in plan.Shapes.OfType<GeneratedRowShape>())
        {
            foreach (var field in shape.Fields)
            {
                if (field.GeneratedTypeName != null || field.InterpretationShape != null)
                    continue;
                var unresolved = ConcreteValueShape.FindUnresolvedValueType(field.Type.ResolveClrType());
                if (unresolved != null && reportedFields.Add((field.QualifiedName, unresolved)))
                    Report(diagnostics, $"Intermediate field '{field.QualifiedName}'", unresolved);
            }
        }

        var expressions = ExecutionIrAnalysis.FlattenExpressions(plan.Body).ToArray();
        foreach (var read in expressions.OfType<ExecutionFieldRead>())
        {
            if (read.AccessStrategy is GeneratedDictionaryNestedAccess)
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Dictionary member '{read.FieldName}' requires an authoritative concrete nested shape.");
            if (read.AccessStrategy is NestedClrPropertyAccess path && path.PropertyPath.Contains('['))
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Indexed member '{read.FieldName}' has no concrete member/indexed operation contract.");
        }
        foreach (var member in expressions.OfType<ExecutionMemberRead>())
        {
            if (member.InterpretationShape != null) continue;
            var unresolved = ConcreteValueShape.FindUnresolvedValueType(member.ReturnType.ResolveClrType());
            if (unresolved != null)
                Report(diagnostics, $"Nested member '{member.Receiver.ReturnType.ResolveClrType().Name}.{member.MemberName}'", unresolved);
        }
        foreach (var cast in expressions.OfType<ExecutionStrictCast>())
        {
            var sourceType = Nullable.GetUnderlyingType(cast.Expression.ReturnType.ResolveClrType()) ??
                             cast.Expression.ReturnType.ResolveClrType();
            var targetType = StrictCastLibraryConversionFacts.GetCastTargetType(cast.ReturnType.ResolveClrType());
            if (sourceType == typeof(DBNull) || sourceType == typeof(string) || sourceType == targetType ||
                targetType == typeof(string) || typeof(IConvertible).IsAssignableFrom(sourceType) ||
                (sourceType.IsValueType && (targetType == typeof(Guid) || targetType == typeof(TimeSpan) || targetType == typeof(DateTimeOffset))))
                continue;
            diagnostics.ReportError(DiagnosticCode.MQ3027_InvalidExpressionType,
                $"Cast from '{sourceType.Name}' to '{targetType.Name}' has no concrete conversion operation. " +
                "Provide a typed library conversion instead of an object-valued conversion.");
        }
        foreach (var call in expressions.OfType<ExecutionMethodCall>())
            ValidateScalarCall(call, diagnostics);
        foreach (var access in expressions.OfType<ExecutionArrayAccess>())
        {
            if (access.Array is ExecutionScriptParameterRead or ExecutionScriptVariableRead)
                continue;
            if (access.Array.InterpretationShape?.Element != null) continue;
            var arrayType = access.Array.ReturnType.ResolveClrType();
            if (arrayType.IsArray && arrayType.GetArrayRank() == 1 &&
                arrayType.GetElementType() == access.ElementType.ResolveClrType() &&
                ConcreteValueShape.FindUnresolvedMember(arrayType) is null)
                continue;
            diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                $"Indexed value of type '{access.Array.ReturnType.DisplayName}' has no concrete indexed contract. " +
                "Declare a typed array, list, or dictionary before compilation.");
        }
    }

    private static void ValidateScalarCall(ExecutionMethodCall call, DiagnosticContext diagnostics)
    {
        var method = call.Method.ResolveClrMethod();
        if (!SqlFunctionDeclarations.Read(method).Any(static declaration =>
                declaration.Kind == SqlFunctionKind.Scalar && declaration.Visibility == FunctionVisibility.Public))
            return;

        var parameters = method.GetParameters().Where(static parameter =>
            parameter.GetCustomAttribute<InjectTypeAttribute>() == null).ToArray();
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.ParameterType.IsArray && VariadicParameter.IsVariadic(parameter) &&
                !(call.Arguments.Count == index + 1 &&
                  parameter.ParameterType.IsAssignableFrom(call.Arguments[index].ReturnType.ResolveClrType())))
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Scalar '{method.Name}' expands an invocation argument array. " +
                    "Declare params ReadOnlySpan<T> or concrete fixed-arity overloads.");
            if (index < call.Arguments.Count && parameter.ParameterType.IsInterface &&
                call.Arguments[index].ReturnType.ResolveClrType().IsValueType)
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Scalar '{method.Name}' parameter '{parameter.Name}' requires boxing a value-type argument " +
                    $"into '{parameter.ParameterType.Name}'. Declare a concrete typed overload.");
            var definitionParameter = method.IsConstructedGenericMethod
                ? method.GetGenericMethodDefinition().GetParameters().Where(static item => item.GetCustomAttribute<InjectTypeAttribute>() == null).ElementAt(index)
                : parameter;
            var generatedParameter = definitionParameter.ParameterType.IsGenericParameter &&
                call.GeneratedGenericArguments.ContainsKey(definitionParameter.ParameterType.GenericParameterPosition);
            var closedInterpretationArgument = definitionParameter.ParameterType.ContainsGenericParameters &&
                index < call.Arguments.Count && call.Arguments[index].InterpretationShape != null;
            var unresolved = generatedParameter || closedInterpretationArgument ? null : ConcreteValueShape.FindUnresolvedValueType(parameter.ParameterType);
            if (unresolved != null)
                Report(diagnostics, $"Scalar '{method.Name}' parameter '{parameter.Name}'", unresolved);
        }

        var result = call.GeneratedResultTypeName is null
            ? ConcreteValueShape.FindUnresolvedValueType(method.ReturnType) : null;
        if (result != null)
            Report(diagnostics, $"Scalar '{method.Name}' result", result);
    }

    private static void Report(DiagnosticContext diagnostics, string boundary, string unresolved)
        => diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
            $"{boundary} has an unresolved object-valued shape at '{unresolved}'. " +
            "Declare concrete value types before compilation.");
}
