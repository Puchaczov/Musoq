using System;
using FQL.Parser.Tokens;

namespace FQL.Parser
{
    public abstract class ParserBase<TToken, TTokenType>
        where TToken : GenericToken<TTokenType>
        where TTokenType : struct, IComparable, IFormattable
    {
    }
}