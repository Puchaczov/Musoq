using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor : IScopeAwareExpressionVisitor
{
    private readonly List<BinaryFromNode> _joinedTables = [];
    private int _queryIndex;
    private Scope? _scopeValue;
    private RootNode? _rootScript;
    private RootNode? _phaseRoot;

    public RewriteQueryVisitor(CompilationOptions? compilationOptions = null)
    {
        _ = compilationOptions;
    }

    internal RewriteQueryVisitor(
        RewriteQueryPhaseInput phaseInput,
        CompilationOptions? compilationOptions = null)
        : this(compilationOptions)
    {
        ArgumentNullException.ThrowIfNull(phaseInput);
        phaseInput.Validate();
        _phaseRoot = phaseInput.Root;
    }

    internal void ValidatePhaseRoot(RootNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (_phaseRoot is not null && !ReferenceEquals(_phaseRoot, root))
            throw new InvalidOperationException(
                "Rewrite query phase input does not match the root being traversed.");
    }

    private Scope Scope => _scopeValue ?? throw new InvalidOperationException("Rewrite query visitor scope must be set before visiting query nodes.");

    private Stack<Node> Nodes { get; } = new();

    public RootNode RootScript
    {
        get => _rootScript ?? throw new InvalidOperationException("Root script is available only after visiting the root node.");
        private set => _rootScript = value ?? throw new ArgumentNullException(nameof(value));
    }
}
