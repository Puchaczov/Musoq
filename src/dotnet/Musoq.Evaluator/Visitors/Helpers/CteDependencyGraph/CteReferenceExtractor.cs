using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Extracts all CTE references (InMemoryTableFromNode) from a query subtree.
///     Used to determine which CTEs a given CTE or outer query depends on.
/// </summary>
public class CteReferenceExtractor : NoOpExpressionVisitor
{
    private readonly HashSet<string> _foundReferences = new();
    private readonly HashSet<string> _knownCteNames;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CteReferenceExtractor" /> class.
    /// </summary>
    /// <param name="knownCteNames">The set of known CTE names to look for.</param>
    public CteReferenceExtractor(IEnumerable<string> knownCteNames)
    {
        _knownCteNames = [.. knownCteNames];
    }

    /// <summary>
    ///     Gets the set of CTE names that were found as references.
    /// </summary>
    public IReadOnlySet<string> FoundReferences => _foundReferences;

    /// <summary>
    ///     Visits an InMemoryTableFromNode and checks if it references a known CTE.
    /// </summary>
    public override void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_knownCteNames.Contains(node.VariableName)) _foundReferences.Add(node.VariableName);
    }

    /// <summary>
    ///     Visits a JoinInMemoryWithSourceTableFromNode and checks if it references a known CTE.
    /// </summary>
    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_knownCteNames.Contains(node.InMemoryTableAlias)) _foundReferences.Add(node.InMemoryTableAlias);
    }

    /// <summary>
    ///     Visits an ApplyInMemoryWithSourceTableFromNode and checks if it references a known CTE.
    /// </summary>
    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_knownCteNames.Contains(node.InMemoryTableAlias)) _foundReferences.Add(node.InMemoryTableAlias);
    }

    /// <summary>
    ///     Records only complete bare CTE arguments. Identifiers nested in records,
    ///     arrays, or qualified column expressions are ordinary scalar expressions and
    ///     are intentionally left to the normal child traversal.
    /// </summary>
    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddCompleteRelationArguments(node.Parameters);
    }

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddCompleteRelationArguments(node.Args);
    }

    private void AddCompleteRelationArguments(ArgsListNode arguments)
    {
        for (var index = 0; index < arguments.Args.Length; index++)
        {
            if (arguments.Args[index] is IdentifierNode identifier &&
                _knownCteNames.Contains(identifier.Name))
                _foundReferences.Add(identifier.Name);
        }
    }

    /// <summary>
    ///     Clears the found references to allow reuse of this extractor.
    /// </summary>
    public void Clear()
    {
        _foundReferences.Clear();
    }
}
