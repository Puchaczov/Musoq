using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR;

namespace Musoq.Targets.CSharpClr;

internal static class StructuralInputSyntaxFactory
{
    internal static LocalDeclarationStatementSyntax RenderPrepare(
        ExecutionPrepareStructuralInput input,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var value = input.Input switch
        {
            ExecutionStructuralArray array => RenderArray(array, context, renderer),
            ExecutionStructuralRecord record => RenderConstruction(
                record,
                record.ConstructionPlan ?? input.Plan,
                context,
                renderer),
            _ => throw UnsupportedShape.Of(
                $"Structural input '{input.Input.GetType().Name}",
                "the C# backend")
        };

        return ExecutionSyntaxFactory.CreateLocalDeclaration(
            ExecutionSyntaxFactory.CreateVariableTypeSyntax(input.Target),
            input.Target.Name,
            value);
    }

    internal static ExpressionSyntax RenderArray(
        ExecutionStructuralArray array,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var rendered = ExecutionSyntaxFactory.CreateArrayCreation(
            array.ElementType,
            array.Elements.Select(element => renderer.RenderExpression(element, context)));
        return array.LimitBinding is { } binding
            ? StructuralInputLimitSyntaxFactory.RenderGuard(
                rendered,
                array.ReturnType.RequireClrType(),
                binding,
                context,
                renderer)
            : rendered;
    }

    internal static ExpressionSyntax RenderConversion(
        ExecutionStructuralConversion conversion,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        if (conversion.LimitBinding is { } limitBinding)
        {
            // A script parameter/let read may need a receiver-specific typed
            // adapter before it can be guarded (for example an array exposed
            // as the engine's read-only sequence type).  Preserve that normal
            // carrier lowering and apply the limit check to its typed result.
            var guardedInput = StructuralCarrierSyntaxFactory.TryCreateConversion(
                renderer.StructuralScriptVariableDefinitions,
                renderer.StructuralScriptParameterDefinitions,
                conversion,
                renderer,
                context,
                out var guardedCarrierExpression)
                ? guardedCarrierExpression
                : renderer.RenderExpression(conversion.Input, context);
            return StructuralInputLimitSyntaxFactory.RenderGuard(
                guardedInput,
                conversion.TargetType.RequireClrType(),
                limitBinding,
                context,
                renderer);
        }

        if (StructuralCarrierSyntaxFactory.TryCreateConversion(
                renderer.StructuralScriptVariableDefinitions,
                renderer.StructuralScriptParameterDefinitions,
                conversion,
                renderer,
                context,
                out var carrierExpression))
        {
            return carrierExpression;
        }

        throw UnsupportedShape.Of(
            $"Structural conversion to '{conversion.TargetType.DisplayName}' has no typed execution lowering",
            "the C# backend");
    }

    internal static ExpressionSyntax RenderCteCollection(
        ExecutionCteCollectionInput input,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var helperName = StructuralCtePreparationMembers.GetHelperName(input);

        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(helperName))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                renderer.RenderExpression(input.Rows, context),
                SyntaxFactory.IdentifierName("token")));
    }

    internal static ExpressionSyntax CreateCteElementConstruction(
        ExecutionCteCollectionInput input,
        GeneratedRowShape sourceShape,
        string rowName,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        if (input.ConstructionPlan is { } plan)
        {
            var constructor = plan.Constructor.RequireClrConstructor();
            var parameters = constructor.GetParameters();
            var arguments = new ExpressionSyntax[plan.SourceFieldIndexes.Count];
            for (var index = 0; index < arguments.Length; index++)
            {
                var sourceFieldIndex = plan.SourceFieldIndexes[index];
                if (sourceFieldIndex >= 0)
                {
                    if (sourceFieldIndex >= input.Fields.Count || index >= parameters.Length)
                        throw new InvalidOperationException("CTE structural construction metadata is inconsistent.");

                    arguments[index] = RenderCteFieldRead(
                        sourceShape,
                        input.Fields[sourceFieldIndex],
                        rowName,
                        parameters[index].ParameterType);
                    continue;
                }

                var defaultExpression = plan.Defaults[index];
                arguments[index] = defaultExpression == null
                    ? SyntaxFactory.DefaultExpression(ExecutionSyntaxFactory.CreateTypeSyntax(parameters[index].ParameterType))
                    : renderer.RenderExpression(defaultExpression, context);
            }

            return SyntaxFactory.ObjectCreationExpression(ExecutionSyntaxFactory.CreateTypeSyntax(plan.TargetType))
                .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));
        }

        if (input.Fields.Count != 1)
            throw new InvalidOperationException("A primitive CTE collection requires exactly one output field.");

        return RenderCteFieldRead(
            sourceShape,
            input.Fields[0],
            rowName,
            input.ElementType.RequireClrType());
    }

    internal static ExpressionSyntax RenderCteFieldRead(
        GeneratedRowShape sourceShape,
        ExecutionCteCollectionField field,
        string rowName,
        Type targetType)
    {
        var generatedField = sourceShape.Fields.FirstOrDefault(candidate => candidate.OutputIndex == field.Index)
            ?? throw new InvalidOperationException(
                $"CTE structural field '{field.Name}' at output index {field.Index} is absent from the generated row shape.");
        var value = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ExecutionSyntaxFactory.CreateIdentifierName(rowName),
            ExecutionSyntaxFactory.CreateIdentifierName(GeneratedRowNamingPolicy.GetGeneratedFieldName(generatedField)));

        var sourceClrType = generatedField.Type.RequireClrType();
        if (targetType == typeof(object) || sourceClrType == targetType)
            return value;

        return SyntaxFactory.CastExpression(ExecutionSyntaxFactory.CreateTypeSyntax(targetType), value);
    }
    internal static ExpressionSyntax RenderConstruction(
        ExecutionStructuralRecord record,
        ExecutionStructuralConstructionPlan plan,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var arguments = new ExpressionSyntax[plan.SourceFieldIndexes.Count];

        for (var index = 0; index < arguments.Length; index++)
        {
            var sourceFieldIndex = plan.SourceFieldIndexes[index];
            if (sourceFieldIndex >= 0)
            {
                if (sourceFieldIndex >= record.Fields.Count)
                    throw new InvalidOperationException(
                        $"Structural construction field index {sourceFieldIndex} is outside the authored record.");

                arguments[index] = renderer.RenderExpression(record.Fields[sourceFieldIndex].Value, context);
                continue;
            }

            var defaultExpression = plan.Defaults[index];
            arguments[index] = defaultExpression == null
                ? SyntaxFactory.DefaultExpression(
                    ExecutionSyntaxFactory.CreateTypeSyntax(
                        plan.Constructor.RequireClrConstructor().GetParameters()[index].ParameterType))
                : renderer.RenderExpression(defaultExpression, context);
        }

        var rendered = SyntaxFactory.ObjectCreationExpression(ExecutionSyntaxFactory.CreateTypeSyntax(plan.TargetType))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));
        return record.LimitBinding is { } binding
            ? StructuralInputLimitSyntaxFactory.RenderGuard(
                rendered,
                record.ReturnType.RequireClrType(),
                binding,
                context,
                renderer)
            : rendered;
    }

    internal static bool CanRenderPreparation(
        ExecutionPrepareStructuralInput input,
        Func<ExecutionExpression, bool> canRenderExpression)
    {
        if (!canRenderExpression(input.Input) &&
            input.Input is not ExecutionStructuralRecord)
            return false;

        if (input.Input is ExecutionStructuralRecord record &&
            record.Fields.Any(field => !canRenderExpression(field.Value)))
            return false;

        return input.Plan.SourceFieldIndexes.Count == input.Plan.Defaults.Count &&
               input.Plan.SourceFieldIndexes.All(index => index >= -1) &&
               input.Plan.Defaults.Where(static value => value != null).All(value => canRenderExpression(value!)) &&
               input.Plan.TargetType.Descriptor.Portability !=
               Musoq.Targets.Abstractions.ExecutionPortableSymbolPortability.HostImport;
    }
}
