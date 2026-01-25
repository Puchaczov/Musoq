#nullable enable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emits GROUP BY related syntax constructs.
/// </summary>
internal static class GroupByEmitter
{
    /// <summary>
    ///     Creates a foreach block for GROUP BY iteration with the given instructions.
    /// </summary>
    /// <param name="foreachInstructions">The block of instructions to execute in the foreach.</param>
    /// <param name="variableName">The variable name for the foreach iterator.</param>
    /// <param name="tableVariable">The table variable to iterate over.</param>
    /// <returns>A BlockSyntax containing the foreach loop.</returns>
    public static BlockSyntax CreateGroupByForeach(
        BlockSyntax foreachInstructions,
        string variableName,
        string tableVariable)
    {
        return StatementEmitter.CreateBlock(
            StatementEmitter.CreateForeach(variableName,
                SyntaxFactory.IdentifierName(tableVariable),
                foreachInstructions).NormalizeWhitespace());
    }

    /// <summary>
    ///     Creates a HAVING clause condition statement using the syntax generator.
    /// </summary>
    /// <param name="conditionExpression">The HAVING condition expression.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>A SyntaxNode representing the HAVING if statement.</returns>
    public static SyntaxNode CreateHavingCondition(SyntaxNode conditionExpression, SyntaxGenerator generator)
    {
        return generator.IfStatement(
                generator.LogicalNotExpression(conditionExpression),
                [StatementEmitter.CreateContinue()])
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }

    /// <summary>
    ///     Builds the GROUP BY execution block by assembling CSE declarations, cancellation check,
    ///     WHERE clause, group keys/values, parent/group declarations, for statement, refresh nodes,
    ///     and having clause.
    /// </summary>
    /// <param name="block">The initial block.</param>
    /// <param name="cseDeclarations">CSE variable declarations.</param>
    /// <param name="cancellationCheck">The cancellation check statement.</param>
    /// <param name="where">Optional WHERE clause statement.</param>
    /// <param name="groupKeysDeclaration">The group keys local declaration.</param>
    /// <param name="groupValuesDeclaration">The group values local declaration.</param>
    /// <param name="refreshBlock">Optional refresh block from stack.</param>
    /// <param name="groupHaving">Optional HAVING clause statement.</param>
    /// <param name="scoreTable">The score table alias for grouping.</param>
    /// <returns>The assembled GROUP BY execution block.</returns>
    public static BlockSyntax BuildGroupByExecutionBlock(
        BlockSyntax block,
        StatementSyntax[] cseDeclarations,
        StatementSyntax cancellationCheck,
        StatementSyntax? where,
        VariableDeclarationSyntax groupKeysDeclaration,
        VariableDeclarationSyntax groupValuesDeclaration,
        BlockSyntax? refreshBlock,
        SyntaxNode? groupHaving,
        string scoreTable)
    {
        if (cseDeclarations.Length > 0) block = StatementEmitter.CreateBlock(cseDeclarations.Concat(block.Statements));

        block = block.AddStatements(cancellationCheck);

        if (where != null)
            block = block.AddStatements(where);

        block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(groupKeysDeclaration));
        block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(groupValuesDeclaration));

        block = block.AddStatements(QueryEmitter.GenerateParentGroupDeclaration());
        block = block.AddStatements(QueryEmitter.GenerateGroupDeclaration());

        block = block.AddStatements(CreateGroupForStatement());

        if (refreshBlock != null)
            block = block.AddStatements(refreshBlock.Statements.ToArray());

        if (groupHaving != null)
            block = block.AddStatements((StatementSyntax)groupHaving);

        block = block.AddStatements(CreateAddGroupStatement(scoreTable));

        return block;
    }

    /// <summary>
    ///     Creates the AddGroupStatement which conditionally adds a group to the score table.
    /// </summary>
    private static StatementSyntax CreateAddGroupStatement(string scoreTable)
    {
        return StatementEmitter.CreateIf(
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("usedGroups"),
                        SyntaxFactory.IdentifierName("Contains")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")))))),
            StatementEmitter.CreateBlock(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(scoreTable),
                            SyntaxFactory.IdentifierName("Add")))
                        .WithArgumentList(SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")))))),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("usedGroups"),
                            SyntaxFactory.IdentifierName("Add")))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))))))));
    }

    /// <summary>
    ///     Creates the for loop that iterates through group keys and builds the group hierarchy.
    /// </summary>
    private static StatementSyntax CreateGroupForStatement()
    {
        var forBody = StatementEmitter.CreateBlock(
            CreateCancellationCheck(),
            CreateKeyDeclaration(),
            CreateGroupLookupIfStatement(),
            StatementEmitter.CreateAssignment("parent", SyntaxFactory.IdentifierName("group")));

        return SyntaxFactory.ForStatement(forBody)
            .WithDeclaration(CreateLoopVariableDeclaration())
            .WithCondition(CreateLoopCondition())
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression,
                    SyntaxFactory.IdentifierName("i"))));
    }

    private static StatementSyntax CreateCancellationCheck()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("token"),
                    SyntaxFactory.IdentifierName(nameof(CancellationToken.ThrowIfCancellationRequested)))));
    }

    private static StatementSyntax CreateKeyDeclaration()
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("key"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("keys"))
                                        .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))))))));
    }

    private static StatementSyntax CreateGroupLookupIfStatement()
    {
        var condition = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("groups"),
                    SyntaxFactory.IdentifierName("ContainsKey")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")))));

        var thenBlock = StatementEmitter.CreateBlock(
            StatementEmitter.CreateAssignment("group",
                SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("groups"))
                    .WithArgumentList(
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")))))));

        var elseBlock = StatementEmitter.CreateBlock(
            CreateGroupCreationAssignment(),
            CreateGroupsAddInvocation());

        return StatementEmitter.CreateIf(condition, thenBlock, elseBlock);
    }

    private static StatementSyntax CreateGroupCreationAssignment()
    {
        return StatementEmitter.CreateAssignment("group",
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("Group"))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("parent")),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("groupFieldsNames"))
                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i")))))),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("values"))
                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))))
                    }))));
    }

    private static StatementSyntax CreateGroupsAddInvocation()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("groups"),
                        SyntaxFactory.IdentifierName("Add")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))
                        }))));
    }

    private static VariableDeclarationSyntax CreateLoopVariableDeclaration()
    {
        return SyntaxFactory.VariableDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("i"))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0))))));
    }

    private static ExpressionSyntax CreateLoopCondition()
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.LessThanExpression,
            SyntaxFactory.IdentifierName("i"),
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("keys"),
                SyntaxFactory.IdentifierName("Length")));
    }
}
