using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ForStatementSyntax CreateLibraryWindowFrameLoop(ExecutionWindowAggregateKernel kernel,
        string indices, string start, string count)
    {
        var frame = kernel.Frame!;
        var position = $"{kernel.Results.Name}PartitionIndex";
        var outputIndex = $"{kernel.Results.Name}OutputIndex";
        var frameStart = $"{kernel.Results.Name}FrameStart";
        var frameEnd = $"{kernel.Results.Name}FrameEnd";
        var state = SyntaxFactory.IdentifierName(CreateLibraryWindowStateName(kernel));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(indices, start, position, outputIndex),
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), frameStart,
                WindowRangeFrameSyntax.CreateAggregateFrameStartExpressionForKernel(kernel, frame.Start, indices, start, position, count)),
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), frameEnd,
                WindowRangeFrameSyntax.CreateAggregateFrameEndExpressionForKernel(kernel, frame.End, indices, start, position, count)),
            SyntaxFactory.ExpressionStatement(CreateKernelStateResetInvocation(kernel.Descriptor.LibraryKernel!.ResetMethod.RequireClrMethod(), state)),
            CreateLibraryWindowFrameAccumulation(kernel, indices, start, count, frameStart, frameEnd),
            CreateWindowResultAssignment(kernel.Results.Name, SyntaxFactory.IdentifierName(outputIndex), CreateLibraryWindowResult(kernel))
        };
        return CreatePartitionIndexedForLoop(position, count, StatementEmitter.CreateBlock(body));
    }

    private ForStatementSyntax CreateLibraryWindowFrameAccumulation(ExecutionWindowAggregateKernel kernel,
        string indices, string start, string count, string frameStart, string frameEnd)
    {
        var index = $"{kernel.Results.Name}FrameIndex";
        var current = new ExecutionVariable($"{kernel.Results.Name}CurrentIndex", typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(indices, start, index, current.Name)
        };
        if (kernel.Descriptor.CaptureArguments)
        {
            var values = new List<ExpressionSyntax>();
            for (var argument = 0; argument < kernel.Descriptor.LibraryKernel!.ArgumentTypes.Count; argument++)
                values.Add(SyntaxFactory.ParseExpression($"{CapturedWindowArgumentName(kernel, argument)}[{current.Name}]"));
            var update = LibraryKernelInvocationSyntax.Set(kernel.Descriptor.LibraryKernel!,
                CreateLibraryWindowStateName(kernel), values);
            if (kernel.FilterPredicate is null)
                body.Add(update);
            else
                body.Add(StatementEmitter.CreateIf(
                    SyntaxFactory.ParseExpression($"{kernel.Results.Name}CapturedFilter[{current.Name}]"),
                    StatementEmitter.CreateBlock([update])));
        }
        else
        {
            body.AddRange(CreateIndexedItemDeclarations(kernel.Item, kernel.Buffer, current, kernel.RowAccessMode));
            body.AddRange(CreateWindowAggregateAccumulateStatements(kernel));
        }
        var limit = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, SyntaxFactory.IdentifierName(index),
                    SyntaxFactory.IdentifierName(frameEnd)),
                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, SyntaxFactory.IdentifierName(index),
                    SyntaxFactory.IdentifierName(count)));
        return LibraryWindowLoopSyntax.Accumulate(index,
            CreateMathInvocation(nameof(Math.Max), CreateIntLiteral(0), SyntaxFactory.IdentifierName(frameStart)), limit, body);
    }
}
