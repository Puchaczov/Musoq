using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderOrdinalRangeIndex(
        ExecutionCreateRangeIndex index,
        ExecutionRenderContext context
    )
    {
        var rows = RenderExpression(index.Candidates, context);
        var ordinal = SyntaxFactory.IdentifierName(index.Index.Name + "Position");
        var count = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            rows,
            SyntaxFactory.IdentifierName("Count")
        );
        var sequence = CreateInvocationExpression(
            "Enumerable",
            "Range",
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)),
            count
        );
        var positions = SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    sequence,
                    SyntaxFactory.IdentifierName("Select")
                )
            )
            .WithArgumentList(
                CreateArgumentList(
                    SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(ordinal.Identifier),
                        CreateInvocationExpression("Tuple", "Create", ordinal)
                    )
                )
            );
        var position = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ordinal,
            SyntaxFactory.IdentifierName("Item1")
        );
        var selector = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(ordinal.Identifier),
            StatementEmitter.CreateBlock(
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    index.Candidate.Name,
                    CreateElementAccess(rows, position)
                ),
                SyntaxFactory.ReturnStatement(RenderExpression(index.CandidateKey, context))
            )
        );
        var method = SyntaxFactory
            .GenericName(nameof(EvaluationHelper.CreateRangeJoinIndex))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>([
                        CreateTypeSyntax(typeof(Tuple<int>)),
                        CreateTypeSyntax(index.KeyType),
                    ])
                )
            );
        var build = SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    method
                )
            )
            .WithArgumentList(
                CreateArgumentList(positions, selector, CreateBinaryOpKindExpression(index.ComparisonKind))
            );
        return
        [
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), index.Index.Name, build),
            CreateLocalDeclaration(
                CreateTypeSyntax(index.KeyType),
                index.Index.Name + "PreviousKey",
                SyntaxFactory.DefaultExpression(CreateTypeSyntax(index.KeyType))
            ),
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(bool)),
                index.Index.Name + "HasPreviousKey",
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)
            ),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                index.Index.Name + "Positions",
                SyntaxFactory
                    .ObjectCreationExpression(CreateTypeSyntax(typeof(List<int>)))
                    .WithArgumentList(SyntaxFactory.ArgumentList())
            ),
        ];
    }

    private StatementSyntax RenderOrderedRangeMatches(ExecutionRangeProbe probe, ExecutionRenderContext context)
    {
        var scratch = probe.Index.Name + "Positions";
        var item = probe.Index.Name + "Position";
        var ordinal = probe.InputOrdinal!.Name;
        var rows = SyntaxFactory.IdentifierName(probe.BufferedRows!.Name);
        var position = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(item),
            SyntaxFactory.IdentifierName("Item1")
        );
        var currentKey = SyntaxFactory.IdentifierName(probe.Index.Name + "ProbeKey");
        var previousKey = SyntaxFactory.IdentifierName(probe.Index.Name + "PreviousKey");
        var hasPreviousKey = SyntaxFactory.IdentifierName(probe.Index.Name + "HasPreviousKey");
        var append = StatementEmitter.CreateForeach(
            item,
            CreateInvocationExpression(probe.Index.Name, "Find", currentKey),
            StatementEmitter.CreateBlock(CreateInvocationStatement(scratch, "Add", position))
        );
        var body = RenderBlock(probe.Body, context);
        var matches = StatementEmitter.CreateForeach(
            ordinal,
            SyntaxFactory.IdentifierName(scratch),
            body.WithStatements(
                body.Statements.Insert(
                    0,
                    CreateLocalDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        probe.Match.Name,
                        CreateElementAccess(rows, SyntaxFactory.IdentifierName(ordinal))
                    )
                )
            )
        );
        return StatementEmitter.CreateBlock(
            CreateLocalDeclaration(
                CreateTypeSyntax(probe.KeyType),
                currentKey.Identifier.ValueText,
                RenderExpression(probe.ProbeKey, context)
            ),
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LogicalOrExpression,
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, hasPreviousKey),
                    SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, currentKey, previousKey)
                ),
                StatementEmitter.CreateBlock(
                    CreateInvocationStatement(scratch, "Clear"),
                    append,
                    CreateInvocationStatement(scratch, "Sort"),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            previousKey,
                            currentKey
                        )
                    ),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            hasPreviousKey,
                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                        )
                    )
                )
            ),
            matches
        );
    }
}
