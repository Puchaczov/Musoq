using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Musoq.Parser.Lexing
{
    /// <summary>
    ///     Idea how to implement this piece of code where founded here:
    ///     https://blogs.msdn.microsoft.com/drew/2009/12/31/a-simple-lexer-in-c-that-uses-regular-expressions/
    /// </summary>
    public abstract class LexerBase<TToken> : ILexer<TToken>
    {
        #region Constructors

        /// <summary>
        ///     Initialize instance.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="defaultToken">The Default token.</param>
        /// <param name="definitions">The Definitions.</param>
        protected LexerBase(string input, TToken defaultToken, params TokenDefinition[] definitions)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException(nameof(input));

            if (definitions == null || definitions.Length == 0)
                throw new ArgumentException(nameof(definitions));

            Input = input.Trim();

            Position = 0;
            _currentToken = defaultToken;
            _definitions = definitions;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Determine if lexer position is out of range.
        /// </summary>
        protected bool IsOutOfRange => Position >= Input.Length;

        #endregion

        #region TokenUtils

        [DebuggerDisplay("{Regex.ToString()}")]
        protected sealed class TokenDefinition
        {
            public TokenDefinition(string pattern)
            {
                Regex = new Regex(pattern);
            }

            public TokenDefinition(string pattern, RegexOptions options)
            {
                Regex = new Regex(pattern, options);
            }

            public Regex Regex { get; }
        }

        #endregion

        #region Private Variables

        private readonly TokenDefinition[] _definitions;
        private TToken _currentToken;
        private TToken _lastToken;

        #endregion

        #region Protected properties

        /// <summary>
        ///     Gets or sets the position.
        /// </summary>
        public int Position { get; protected set; }

        /// <summary>
        ///     Gets the input.
        /// </summary>
        protected string Input { get; }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Assigns token of specific type.
        /// </summary>
        /// <param name="instantiate">Instantiate token function.</param>
        /// <returns>The TToken.</returns>
        protected TToken AssignTokenOfType(Func<TToken> instantiate)
        {
            if (instantiate == null)
                throw new ArgumentNullException(nameof(instantiate));

            _lastToken = _currentToken;
            _currentToken = instantiate();
            return _currentToken;
        }

        /// <summary>
        ///     Gets the token.
        /// </summary>
        /// <param name="matchedDefinition">The matched definition.</param>
        /// <param name="match">The match.</param>
        /// <returns>The TToken.</returns>
        protected abstract TToken GetToken(TokenDefinition matchedDefinition, Match match);

        /// <summary>
        ///     Gets end of file token.
        /// </summary>
        /// <returns>The TToken.</returns>
        protected abstract TToken GetEndOfFileToken();

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets lastly processed token.
        /// </summary>
        /// <returns>The TToken.</returns>
        public virtual TToken Last()
        {
            return _lastToken;
        }

        /// <summary>
        ///     Gets currently processing token.
        /// </summary>
        /// <returns>The TToken.</returns>
        public virtual TToken Current()
        {
            return _currentToken;
        }

        /// <summary>
        ///     Compute next token.
        /// </summary>
        /// <returns>The TToken.</returns>
        public virtual TToken Next()
        {
            while (!IsOutOfRange)
            {
                TokenDefinition matchedDefinition = null;
                var matchLength = 0;

                Match match = null;

                foreach (var rule in _definitions)
                {
                    match = rule.Regex.Match(Input, Position);

                    if (!match.Success || match.Index - Position != 0) continue;
                    
                    matchedDefinition = rule;
                    matchLength = match.Length;
                    break;
                }

                if (matchedDefinition == null)
                    throw new UnknownTokenException(Position, Input[Position],
                        $"Unrecognized token exception at {Position} for {Input[Position..]}");
                var token = GetToken(matchedDefinition, match);
                Position += matchLength;

                return AssignTokenOfType(() => token);
            }

            return AssignTokenOfType(GetEndOfFileToken);
        }
        
        public virtual TToken NextOf(Regex regex, Func<string, TToken> getToken)
        {
            if (IsOutOfRange)
                return AssignTokenOfType(GetEndOfFileToken);
            
            var match = regex.Match(Input, Position);
            
            if (!match.Success || match.Index - Position != 0)
                throw new UnknownTokenException(Position, Input[Position],
                    $"Unrecognized token exception at {Position} for {Input[Position..]}");
            
            var token = getToken(match.Value);
            Position += match.Length;
            
            return AssignTokenOfType(() => token);
        }

        #endregion
    }
}