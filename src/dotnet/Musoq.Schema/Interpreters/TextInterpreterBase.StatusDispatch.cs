namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    ParseAttempt<TOut> ITextInterpreterStatus<TOut>.ReadAt(ref TextParseInput input, int offset)
    {
        try
        {
            return ParseAttempt<TOut>.Success(ParseAt(input.Text, offset));
        }
        catch (ParseException failure)
        {
            return ParseAttempt<TOut>.Failed(_partialParsedValue, ParseFailure.FromException(failure));
        }
    }
}
