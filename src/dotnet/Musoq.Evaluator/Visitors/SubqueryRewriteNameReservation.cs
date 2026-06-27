using System;
using System.Collections.Generic;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private readonly HashSet<string> _reservedCteNames = new(StringComparer.OrdinalIgnoreCase);
    private int _subqueryCounter;
    private int _derivedTableCounter;

    internal void RegisterReservedCteNames(IEnumerable<CteInnerExpressionNode> expressions)
    {
        foreach (var expression in expressions)
            ReserveGeneratedName(expression.Name);
    }

    private void RegisterReservedAliases(IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
            ReserveGeneratedName(alias);
    }

    private string CreateUniqueSubqueryName()
    {
        while (true)
        {
            var candidate = GeneratedSubqueryContract.CreateSubqueryName(++_subqueryCounter);
            if (ReserveGeneratedName(candidate))
                return candidate;
        }
    }

    private string CreateUniqueDerivedTableName()
    {
        while (true)
        {
            var candidate = GeneratedSubqueryContract.CreateDerivedTableName(++_derivedTableCounter);
            if (ReserveGeneratedName(candidate))
                return candidate;
        }
    }

    private string CreateUniqueScalarMaterializationCteName(string cteName)
    {
        var preferredName = GeneratedSubqueryContract.CreateScalarMaterializationName(cteName);
        if (ReserveGeneratedName(preferredName))
            return preferredName;

        var index = 1;
        while (true)
        {
            var candidate = GeneratedSubqueryContract.CreateScalarMaterializationName(
                GeneratedSubqueryContract.CreateSubqueryName(index++));
            if (ReserveGeneratedName(candidate))
                return candidate;
        }
    }

    private bool ReserveGeneratedName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && _reservedCteNames.Add(name);
    }
}
