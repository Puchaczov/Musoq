using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static void AddCollectionParameterMembers(
        ExecutionPlan plan,
        ICollection<MemberDeclarationSyntax> members)
    {
        if (ExecutionIrAnalysis.CollectExpressions<ExecutionCollectionInCheck>(plan.Body).Any())
            members.Add(CreateCollectionParameterContainsFunction());
    }
}
