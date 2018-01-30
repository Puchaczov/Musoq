using FQL.Parser.Tokens;

namespace FQL.Parser.Nodes
{
    public enum SetOperator
    {
        Except = TokenType.Except,
        Union = TokenType.Union,
        Intersect = TokenType.Intersect
    }
}