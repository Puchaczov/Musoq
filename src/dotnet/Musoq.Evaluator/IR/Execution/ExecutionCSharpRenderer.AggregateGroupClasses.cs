using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ClassDeclarationSyntax RenderAggregateGroupClass(
        AggregateGroupShape shape,
        ExecutionRenderContext context)
    {
        shape = CreateRenderableAggregateGroupShape(shape, context);

        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(shape.OwnerFields.Select(CreateAggregateGroupOwnerField));
        members.AddRange(shape.CapturedFields.Select(CreateAggregateCapturedField));
        members.AddRange(shape.Keys.Select(CreateAggregateGroupKeyField));
        members.AddRange(shape.Accumulators.Select(CreateAggregateAccumulatorField));
        members.Add(CreateAggregateGroupConstructor(shape));
        if (shape.OwnerFields.Count == 0 && shape.Accumulators.All(static accumulator => accumulator.CanMerge))
            members.Add(CreateAggregateGroupMergeMethod(shape));

        return SyntaxFactory.ClassDeclaration(shape.TypeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword))
            .WithMembers(SyntaxFactory.List(members));
    }

    private static FieldDeclarationSyntax CreateAggregateGroupOwnerField(AggregateGroupOwnerField owner)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(owner.Shape.TypeName))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(owner.FieldName))))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
    }

    private static FieldDeclarationSyntax CreateAggregateCapturedField(AggregateCapturedField capturedField)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(CreateTypeSyntax(capturedField.Type))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(capturedField.FieldName))))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
    }

    private static FieldDeclarationSyntax CreateAggregateGroupKeyField(AggregateGroupKeyField key)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(CreateTypeSyntax(key.Type))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(key.FieldName))))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
    }

    private static FieldDeclarationSyntax CreateAggregateAccumulatorField(AggregateAccumulatorField accumulator)
    {
        var accumulatorType = CreateTypeSyntax(accumulator.AccumulatorType);
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(accumulatorType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(accumulator.FieldName))))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
    }

    private static ConstructorDeclarationSyntax CreateAggregateGroupConstructor(AggregateGroupShape shape)
    {
        var parameters = shape.OwnerFields
            .Select(static owner => CreateParameter(owner.FieldName, SyntaxFactory.IdentifierName(owner.Shape.TypeName)))
            .Concat(shape.Keys
                .Select(static key => CreateParameter(key.FieldName, CreateTypeSyntax(key.Type))))
            .ToList();

        var body = new List<StatementSyntax>();
        foreach (var owner in shape.OwnerFields)
        {
            body.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(owner.FieldName)),
                SyntaxFactory.IdentifierName(owner.FieldName))));
        }

        foreach (var key in shape.Keys)
        {
            body.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(key.FieldName)),
                SyntaxFactory.IdentifierName(key.FieldName))));
        }

        return SyntaxFactory.ConstructorDeclaration(shape.TypeName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(StatementEmitter.CreateBlock(body));
    }

    private static MethodDeclarationSyntax CreateAggregateGroupMergeMethod(AggregateGroupShape shape)
    {
        var sourceParameter = CreateParameter("source", SyntaxFactory.IdentifierName(shape.TypeName));
        var body = new List<StatementSyntax>();

        body.AddRange(shape.Accumulators.Select(CreateAggregateAccumulatorMergeStatement));
        body.AddRange(shape.CapturedFields.Select(CreateAggregateCapturedFieldMergeStatement));

        return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "MergeFrom")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(sourceParameter)))
            .WithBody(StatementEmitter.CreateBlock(body));
    }

    private static StatementSyntax CreateAggregateAccumulatorMergeStatement(AggregateAccumulatorField accumulator)
    {
        var kernelInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateTypeSyntax(accumulator.Kernel.KernelType),
                    SyntaxFactory.IdentifierName("Merge")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(CreateAggregateAccumulatorAccess(SyntaxFactory.ThisExpression(), accumulator, false))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
                SyntaxFactory.Argument(CreateAggregateAccumulatorAccess(SyntaxFactory.IdentifierName("source"), accumulator, false))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.InKeyword))
            ])));

        return SyntaxFactory.ExpressionStatement(kernelInvocation);
    }

    private static StatementSyntax CreateAggregateCapturedFieldMergeStatement(AggregateCapturedField capturedField)
    {
        var sourceRead = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("source"),
            SyntaxFactory.IdentifierName(capturedField.FieldName));

        return CreateAggregateCapturedFieldAssignment(capturedField, sourceRead);
    }

    private static ExpressionStatementSyntax CreateAggregateCapturedFieldAssignment(
        AggregateCapturedField capturedField,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(capturedField.FieldName)),
                value));
    }
    private static bool CanRenderAggregateGroupShape(AggregateGroupShape shape)
    {
        return CanRenderIdentifier(shape.TypeName) &&
               CanRenderFieldNames(shape.OwnerFields.Select(static owner => owner.FieldName)) &&
               CanRenderFieldNames(shape.Keys.Select(static key => key.FieldName)) &&
               CanRenderFieldNames(shape.CapturedFields.Select(static capturedField => capturedField.FieldName)) &&
               CanRenderFieldNames(shape.Accumulators.Select(static accumulator => accumulator.FieldName)) &&
               shape.OwnerFields.All(static owner => CanRenderIdentifier(owner.Shape.TypeName)) &&
               shape.Keys.All(static key => CanReferenceType(key.Type)) &&
               shape.Accumulators.All(static accumulator =>
                   CanReferenceType(accumulator.InputType) &&
                   CanReferenceType(accumulator.ResultType) &&
                   CanReferenceType(accumulator.AccumulatorType) &&
                   CanReferenceType(accumulator.Kernel.KernelType) &&
                   CanReferenceType(accumulator.Kernel.StateType));
    }

    private static bool CanRenderAggregateGroupPlan(AggregateGroupPlan plan)
    {
        return plan.Levels.All(static level => CanRenderAggregateGroupShape(level.Shape));
    }
}
