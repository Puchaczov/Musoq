namespace FQL.Parser.Lexing
{
    public interface ILexer<out TToken>
    {
        /// <summary>
        ///     Gets the current position.
        /// </summary>
        int Position { get; }

        /// <summary>
        ///     Gets lastly taken token from stream.
        /// </summary>
        /// <returns>The TToken.</returns>
        TToken Last();

        /// <summary>
        ///     Gets the currently computed token.
        /// </summary>
        /// <returns>The TToken.</returns>
        TToken Current();

        /// <summary>
        ///     Compute the next token from stream.
        /// </summary>
        /// <returns>The TToken.</returns>
        TToken Next();
    }
}