using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void CheckReorderedQueryWithJoin_ShouldConstructQuery()
        {
            var query = "from #some.a() s1 inner join #some.b() s2 on s1.col = s2.col where s1.col2 = '1' group by s2.col3 select s1.col4, s2.col4 skip 1 take 1";

            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);

            parser.ComposeAll();
        }
    }
}
