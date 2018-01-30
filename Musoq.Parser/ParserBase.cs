using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser
{
    public abstract class ParserBase<TToken, TTokenType>
        where TToken : GenericToken<TTokenType>
        where TTokenType : struct, IComparable, IFormattable
    {
    }
}