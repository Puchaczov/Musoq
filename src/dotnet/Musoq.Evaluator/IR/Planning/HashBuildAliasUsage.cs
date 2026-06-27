using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static class HashBuildAliasUsage
{
    public static bool BuildKeysReferenceAlias(PhysicalHashJoinNode hashJoin, string alias)
    {
        return hashJoin.BuildKeys.Any(key => ColumnRefExtractor
            .Extract(key)
            .Any(column => string.Equals(column.Alias, alias, StringComparison.OrdinalIgnoreCase)));
    }
}
