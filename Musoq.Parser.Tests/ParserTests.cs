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
        
        [TestMethod]
        public void CouplingSyntax_ComposeSchemaMethodWithKeywordAsMethod_ShouldParse()
        {
            var query = "couple #some.table with table Test as SourceOfTestValues;";

            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);

            parser.ComposeAll();
        }
        
        [TestMethod]
        public void CouplingSyntax_ComposeSchemaMethodWithWordAsMethod_ShouldParse()
        {
            var query = "couple #some.something with table Test as SourceOfTestValues;";

            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);

            parser.ComposeAll();
        }
        
        [TestMethod]
        public void CouplingSyntax_ComposeSchemaMethodWithWordFinishedWithNumberAsMethod_ShouldParse()
        {
            var query = "couple #some.something4 with table Test as SourceOfTestValues;";

            var lexer = new Lexer(query, true);
            var parser = new Parser(lexer);

            parser.ComposeAll();
        }
    }
}
