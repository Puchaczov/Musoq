using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class LibraryWindowInputCaptureSyntax
{
    public static string CapturedWindowArgumentName(ExecutionWindowAggregateKernel kernel, int argument)
        => argument == 0 ? LibraryWindowBatchValuesName(kernel) : $"{kernel.Results.Name}CapturedArgument{argument}";

    public static IReadOnlyList<StatementSyntax> Create(ExecutionWindowAggregateKernel kernel, ExecutionVariable index,
        IReadOnlyList<ExpressionSyntax> expressions, IEnumerable<StatementSyntax> items,
        ExpressionSyntax? filter, ExpressionSyntax count)
    {
        var statements = new List<StatementSyntax>();
        var body = new List<StatementSyntax>(items);
        var values = new List<StatementSyntax>();
        var types = kernel.Descriptor.LibraryKernel!.ArgumentTypes;
        for (var argument = 0; argument < types.Count; argument++)
        {
            var name = CapturedWindowArgumentName(kernel, argument);
            statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), name,
                CreateSizedArrayCreation(types[argument], count)));
            values.Add(CreateArrayAssignment(name, index.Name,
                expressions[argument], types[argument].RequireClrType()));
        }
        if (filter is not null)
        {
            var name = $"{kernel.Results.Name}CapturedFilter";
            statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), name,
                CreateSizedArrayCreation(typeof(bool), count)));
            body.Add(CreateArrayAssignment(name, index.Name, filter, typeof(bool)));
            body.Add(StatementEmitter.CreateIf(SyntaxFactory.ParseExpression($"{name}[{index.Name}]"),
                StatementEmitter.CreateBlock(values)));
        }
        else
            body.AddRange(values);
        statements.Add(CreateIndexedForLoop(index.Name, kernel.Buffer, StatementEmitter.CreateBlock(body)));
        return statements;
    }
}
