namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Static cached operation name strings for visitor error context.
///     These are used by SafePop, SafeCast, etc. to avoid string allocations on every visit.
/// </summary>
internal static class VisitorOperationNames
{
    // Binary operators
    public const string VisitAddNode = "VisitAddNode";
    public const string VisitAndNode = "VisitAndNode";
    public const string VisitOrNode = "VisitOrNode";
    public const string VisitStarNode = "VisitStarNode";
    public const string VisitFSlashNode = "VisitFSlashNode";
    public const string VisitModuloNode = "VisitModuloNode";
    public const string VisitHyphenNode = "VisitHyphenNode";
    public const string VisitBitwiseAndNode = "VisitBitwiseAndNode";
    public const string VisitBitwiseOrNode = "VisitBitwiseOrNode";
    public const string VisitBitwiseXorNode = "VisitBitwiseXorNode";
    public const string VisitLeftShiftNode = "VisitLeftShiftNode";
    public const string VisitRightShiftNode = "VisitRightShiftNode";

    // Unary operators
    public const string VisitNotNode = "VisitNotNode";

    // Short-circuiting
    public const string VisitShortCircuitingNodeLeft = "VisitShortCircuitingNodeLeft";
    public const string VisitShortCircuitingNodeRight = "VisitShortCircuitingNodeRight";

    // Node types with sub-operations
    public const string VisitInNodeRight = "VisitInNode (right)";
    public const string VisitInNodeLeft = "VisitInNode (left)";
    public const string VisitContainsNodeRight = "VisitContainsNode (right)";
    public const string VisitContainsNodeLeft = "VisitContainsNode (left)";
    public const string VisitDotNodeExpression = "VisitDotNode (expression)";
    public const string VisitDotNodeRoot = "VisitDotNode (root)";
    public const string VisitDotNode = "VisitDotNode";
    public const string VisitInterpretAtCallNodeOffset = "VisitInterpretAtCallNode (offset)";
    public const string VisitInterpretAtCallNodeDataSource = "VisitInterpretAtCallNode (dataSource)";

    // Desc node
    public const string VisitDescNode = "VisitDescNode";

    // Field nodes
    public const string VisitFieldNode = "VisitFieldNode";
    public const string VisitFieldOrderedNode = "VisitFieldOrderedNode";

    // Access nodes
    public const string VisitAccessColumnNode = "VisitAccessColumnNode";
    public const string VisitAccessObjectKeyNode = "VisitAccessObjectKeyNode";
    public const string VisitAccessCallChainNode = "VisitAccessCallChainNode";
    public const string VisitPropertyValueNode = "VisitPropertyValueNode";

    // Other nodes
    public const string VisitIsNullNode = "VisitIsNullNode";
    public const string VisitInterpretCallNode = "VisitInterpretCallNode";
    public const string VisitParseCallNode = "VisitParseCallNode";
    public const string VisitTryInterpretCallNode = "VisitTryInterpretCallNode";
    public const string VisitTryParseCallNode = "VisitTryParseCallNode";
    public const string VisitPartialInterpretCallNode = "VisitPartialInterpretCallNode";
    public const string VisitArgsListNode = "VisitArgsListNode";

    // Root operations
    public const string GettingRoot = "getting root";
}
