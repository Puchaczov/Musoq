using System;

namespace FQL.Parser.Tokens
{
    public abstract class GenericToken<TTokenType>
        where TTokenType : struct, IComparable, IFormattable
    {
        /// <summary>
        ///     Initialize instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <param name="span">The span.</param>
        protected GenericToken(string value, TTokenType type, TextSpan span)
        {
            Value = value;
            TokenType = type;
            Span = span;
        }

        /// <summary>
        ///     Gets the span.
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        ///     Gets the token type.
        /// </summary>
        public TTokenType TokenType { get; }

        /// <summary>
        ///     Gets the token string representation.
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///     Clones the token.
        /// </summary>
        /// <returns></returns>
        public abstract GenericToken<TTokenType> Clone();
    }
}