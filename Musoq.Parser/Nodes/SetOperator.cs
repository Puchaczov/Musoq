using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public enum SetOperator
    {
        Except = TokenType.Except,
        Union = TokenType.Union,
        Intersect = TokenType.Intersect
    }
}