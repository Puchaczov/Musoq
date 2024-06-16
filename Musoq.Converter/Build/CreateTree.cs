using Musoq.Parser.Lexing;

namespace Musoq.Converter.Build
{
    public class CreateTree(BuildChain successor) : BuildChain(successor)
    {
        public override void Build(BuildItems items)
        {
            var lexer = new Lexer(items.RawQuery, true);
            var parser = new Parser.Parser(lexer);

            items.RawQueryTree = parser.ComposeAll();

            Successor?.Build(items);
        }
    }
}