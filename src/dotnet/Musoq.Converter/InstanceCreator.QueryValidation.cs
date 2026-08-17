using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static void RejectUnsupportedMultiStatementQuery(RootNode? rawQueryTree)
    {
        if (rawQueryTree?.Expression is not StatementsArrayNode statementsArray)
            return;

        var resultProducingStatements = statementsArray.Statements
            .Where(s => IsResultProducingStatement(s.Node))
            .ToArray();

        if (resultProducingStatements.Length > 1)
            throw new MultiStatementQueryException(resultProducingStatements[1].SpanOrEmpty());
    }

    private static bool IsResultProducingStatement(Node? node)
    {
        return node is not (
            null
            or ParameterBlockNode
            or ScriptVariableDeclarationNode
            or CreateTableNode
            or CoupleNode
            or Parser.Nodes.InterpretationSchema.BinarySchemaNode
            or Parser.Nodes.InterpretationSchema.TextSchemaNode);
    }

}
