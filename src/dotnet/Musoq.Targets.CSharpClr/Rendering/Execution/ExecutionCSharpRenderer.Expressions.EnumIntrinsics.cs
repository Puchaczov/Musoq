using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

internal static class EnumIntrinsicExpressionRenderer
{
    internal static bool CanRender(ExecutionMethodCall methodCall)
    {
        if (methodCall.EnumIntrinsic == EnumIntrinsicKind.EnumValue ||
            methodCall.OperandEnumType == null ||
            methodCall.Arguments.Count == 0 ||
            !ExecutionCSharpRenderer.CanRenderExpression(methodCall.Arguments[0]))
            return false;

        return methodCall.EnumIntrinsic switch
        {
            EnumIntrinsicKind.EnumName => methodCall.ReturnType.RequireClrType() == typeof(string),
            EnumIntrinsicKind.IsDefined => methodCall.ReturnType.RequireClrType() == typeof(bool),
            EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags =>
                methodCall.ReturnType.RequireClrType() == typeof(bool) &&
                methodCall.OperandEnumType.IsFlags &&
                methodCall.EnumMask is { } mask &&
                mask.Kind == methodCall.OperandEnumType.UnderlyingKind,
            _ => false
        };
    }

    internal static ExpressionSyntax Render(
        ExecutionCSharpRenderer renderer,
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        var intrinsic = methodCall.EnumIntrinsic ??
                        throw new InvalidOperationException("Enum intrinsic metadata is missing.");
        var descriptor = methodCall.OperandEnumType ??
                         throw new InvalidOperationException($"Enum intrinsic '{intrinsic}' has no descriptor.");
        if (methodCall.Arguments.Count == 0)
            throw new InvalidOperationException($"Enum intrinsic '{intrinsic}' has no operand.");

        return intrinsic switch
        {
            EnumIntrinsicKind.EnumName => RenderEnumName(renderer, methodCall.Arguments[0], descriptor, context),
            EnumIntrinsicKind.IsDefined => RenderEnumIsDefined(renderer, methodCall.Arguments[0], descriptor, context),
            EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags =>
                RenderEnumFlags(renderer, methodCall.Arguments[0], descriptor, methodCall.EnumMask, intrinsic, context),
            EnumIntrinsicKind.EnumValue => throw new InvalidOperationException(
                "EnumValue must be erased to its primitive operand before execution lowering."),
            _ => throw new ArgumentOutOfRangeException(nameof(intrinsic), intrinsic, "Unknown enum intrinsic.")
        };
    }

    private static ExpressionSyntax RenderEnumName(
        ExecutionCSharpRenderer renderer,
        ExecutionExpression operand,
        EnumTypeDescriptor descriptor,
        ExecutionRenderContext context)
    {
        var seenValues = new HashSet<EnumScalarValue>();
        var arms = new List<SwitchExpressionArmSyntax>();
        foreach (var member in descriptor.Members)
        {
            if (!seenValues.Add(member.Value))
                continue;

            arms.Add(SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.ConstantPattern(RenderEnumScalarLiteral(member.Value)),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(member.Name))));
        }

        arms.Add(SyntaxFactory.SwitchExpressionArm(
            SyntaxFactory.DiscardPattern(),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.SwitchExpression(
                renderer.RenderExpression(operand, context),
                SyntaxFactory.SeparatedList(arms)));
    }

    private static ExpressionSyntax RenderEnumIsDefined(
        ExecutionCSharpRenderer renderer,
        ExecutionExpression operand,
        EnumTypeDescriptor descriptor,
        ExecutionRenderContext context)
    {
        PatternSyntax? definedPattern = null;
        var seenValues = new HashSet<EnumScalarValue>();
        foreach (var member in descriptor.Members)
        {
            if (!seenValues.Add(member.Value))
                continue;

            var memberPattern = (PatternSyntax)SyntaxFactory.ConstantPattern(RenderEnumScalarLiteral(member.Value));
            definedPattern = definedPattern == null
                ? memberPattern
                : SyntaxFactory.BinaryPattern(SyntaxKind.OrPattern, definedPattern, memberPattern);
        }

        if (definedPattern == null)
        {
            return SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.SwitchExpression(
                    renderer.RenderExpression(operand, context),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.DiscardPattern(),
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))));
        }

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.SwitchExpression(
                renderer.RenderExpression(operand, context),
                SyntaxFactory.SeparatedList(
                [
                    SyntaxFactory.SwitchExpressionArm(
                        definedPattern,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                    SyntaxFactory.SwitchExpressionArm(
                        SyntaxFactory.DiscardPattern(),
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                ])));
    }

    private static ExpressionSyntax RenderEnumFlags(
        ExecutionCSharpRenderer renderer,
        ExecutionExpression operand,
        EnumTypeDescriptor descriptor,
        EnumScalarValue? mask,
        EnumIntrinsicKind intrinsic,
        ExecutionRenderContext context)
    {
        var boundMask = mask ?? throw new InvalidOperationException(
            $"Enum flags intrinsic '{intrinsic}' has no compiled mask.");
        var captureName = $"__enumValue{context.Session.EnumIntrinsicPatternCount++}";
        var capture = SyntaxFactory.IdentifierName(captureName);
        var maskLiteral = RenderEnumScalarLiteral(boundMask);
        var maskedValue = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.BitwiseAndExpression,
                capture,
                maskLiteral));
        var comparison = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                intrinsic == EnumIntrinsicKind.HasAnyFlags
                    ? SyntaxKind.NotEqualsExpression
                    : SyntaxKind.EqualsExpression,
                maskedValue,
                intrinsic == EnumIntrinsicKind.HasAnyFlags
                    ? RenderEnumScalarLiteral(EnumScalarValue.FromRaw(descriptor.UnderlyingKind, 0))
                    : maskLiteral));
        var carrierType = EnumScalarTypeFacts.GetCarrierType(descriptor.UnderlyingKind);
        var operandType = operand.ReturnType.ResolveClrType();
        if (Nullable.GetUnderlyingType(operandType) == null)
        {
            return SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.SwitchExpression(
                    renderer.RenderExpression(operand, context),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.VarPattern(
                                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(captureName))),
                            comparison))));
        }

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.SwitchExpression(
                renderer.RenderExpression(operand, context),
                SyntaxFactory.SeparatedList(
                [
                    SyntaxFactory.SwitchExpressionArm(
                        SyntaxFactory.DeclarationPattern(
                            ExecutionSyntaxFactory.CreateTypeSyntax(carrierType),
                            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(captureName))),
                        comparison),
                    SyntaxFactory.SwitchExpressionArm(
                        SyntaxFactory.DiscardPattern(),
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                ])));
    }

    private static ExpressionSyntax RenderEnumScalarLiteral(EnumScalarValue value)
    {
        object carrier = value.Kind switch
        {
            EnumUnderlyingKind.Byte => value.AsByte(),
            EnumUnderlyingKind.SByte => value.AsSByte(),
            EnumUnderlyingKind.Int16 => value.AsInt16(),
            EnumUnderlyingKind.UInt16 => value.AsUInt16(),
            EnumUnderlyingKind.Int32 => value.AsInt32(),
            EnumUnderlyingKind.UInt32 => value.AsUInt32(),
            EnumUnderlyingKind.Int64 => value.AsInt64(),
            EnumUnderlyingKind.UInt64 => value.AsUInt64(),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unknown enum backing kind.")
        };

        return ExecutionCSharpRenderer.RenderLiteral(carrier);
    }
}
