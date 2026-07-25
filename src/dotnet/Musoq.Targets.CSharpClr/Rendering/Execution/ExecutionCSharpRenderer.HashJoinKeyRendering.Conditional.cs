using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private void AddConditionalKeyBuild(
        List<StatementSyntax> statements,
        ExecutionExpression key,
        Type keyType,
        string keyVariableName,
        Func<string, StatementSyntax> createAddStatement)
    {
        if (key is ExecutionValueTupleKey valueTupleKey &&
            HasNullableValueTuplePart(valueTupleKey))
        {
            statements.AddRange(CreateValueTupleKeyPartDeclarations(valueTupleKey));
            statements.Add(SyntaxFactory.IfStatement(
                CreateAllValueTuplePartsNotNullCondition(valueTupleKey),
                StatementEmitter.CreateBlock(CreateValueTupleKeyLocalDeclaration(valueTupleKey, keyVariableName), createAddStatement(keyVariableName))));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            CreateHashKeyLocalType(keyType),
            keyVariableName,
            RenderExpression(key)));

        if (!CanBeNull(keyType))
        {
            statements.Add(createAddStatement(keyVariableName));
            return;
        }

        statements.Add(SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(keyVariableName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            StatementEmitter.CreateBlock(createAddStatement(keyVariableName))));
    }
}
