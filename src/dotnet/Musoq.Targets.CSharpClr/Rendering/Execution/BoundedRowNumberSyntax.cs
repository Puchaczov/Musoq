using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class BoundedRowNumberSyntax
{
    // This local helper compiles against the generated order key's SQL-aware comparer.
    private static readonly LocalFunctionStatementSyntax Selection = (LocalFunctionStatementSyntax)
        SyntaxFactory.ParseStatement(
            """
            static long[] SelectBoundedRows<TKey>(Musoq.Evaluator.Helpers.WindowPartitionSet partitions, TKey[] keys, long bound, System.Threading.CancellationToken cancellation, out int[] positions)
                where TKey : struct, IComparable<TKey>
            {
                int limit = (int)Math.Clamp(bound, 0L, (long)int.MaxValue);
                int total = 0;
                for (int p = 0; p < partitions.PartitionCount; p++)
                    total += Math.Min(limit, partitions.GetLength(p));
                var ranks = new long[total];
                bool allSelected = total == keys.Length;
                var selected = allSelected ? Array.Empty<int>() : new int[total];
                positions = selected;
                if (total == 0)
                    return ranks;
                int next = 0;
                var comparer = Comparer<int>.Create((a,b) => keys[a].CompareTo(keys[b]));
                int[] indices = partitions.Indices;
                for (int p = 0; p < partitions.PartitionCount; p++)
                {
                    if ((p & 1023) == 0) cancellation.ThrowIfCancellationRequested();
                    int start = partitions.GetStart(p);
                    int length = partitions.GetLength(p);
                    if (length <= limit)
                    {
                        if (length == 2)
                        {
                            int first = indices[start];
                            int second = indices[start + 1];
                            bool reverse = keys[first].CompareTo(keys[second]) > 0;
                            Record(first, reverse ? 2 : 1);
                            Record(second, reverse ? 1 : 2);
                        }
                        else
                        {
                            if (length > 2)
                                Array.Sort(indices, start, length, comparer);
                            for (int i = 0; i < length; i++)
                                Record(indices[start + i], i + 1L);
                        }
                        continue;
                    }
                    var best = new Musoq.Evaluator.Helpers.EvaluationHelper.BoundedTopRecordList<int>(limit, comparer);
                    for (int i = 0; i < length; i++)
                    {
                        if ((i & 1023) == 0) cancellation.ThrowIfCancellationRequested();
                        best.Add(indices[start + i]);
                    }
                    long rank = 0;
                    foreach (int input in best)
                    {
                        Record(input, ++rank);
                    }
                }
                if (!allSelected)
                    Array.Sort(selected, ranks);
                return ranks;

                void Record(int input, long rank)
                {
                    if (allSelected)
                        ranks[input] = rank;
                    else
                    {
                        ranks[next] = rank;
                        selected[next++] = input;
                    }
                }
            }
            """
        );

    internal static IEnumerable<StatementSyntax> Select(
        ExecutionComputeRankingWindow ranking,
        ExecutionVariable partitions,
        ExecutionVariable keys,
        ExecutionVariable positions
    )
    {
        var name = SyntaxFactory.Identifier(positions.Name + "Select");
        var output = SyntaxFactory
            .Argument(
                SyntaxFactory.DeclarationExpression(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(positions.Name))
                )
            )
            .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));
        var call = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(name))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(partitions.Name)),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(keys.Name)),
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(ranking.QualifyUpperBound!.Value)
                            )
                        ),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                        output,
                    ])
                )
            );
        return
        [
            Selection.WithIdentifier(name),
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), ranking.Results.Name, call),
        ];
    }
}
