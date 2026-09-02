using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private bool TryScanGenericFunction(int start, int identifierEnd, out Token token)
    {
        token = null!;
        if (identifierEnd >= Input.Length || Input[identifierEnd] != '<')
            return false;

        var typeStart = identifierEnd + 1;
        var input = Input.AsSpan();
        if (typeStart >= Input.Length || !FastCharacterClassifier.IsIdentifierStart(input, typeStart))
            return false;

        var typeEnd = typeStart + FastCharacterClassifier.GetIdentifierCodePointLength(input, typeStart);
        while (typeEnd < Input.Length && FastCharacterClassifier.IsIdentifierContinue(input, typeEnd))
            typeEnd += FastCharacterClassifier.GetIdentifierCodePointLength(input, typeEnd);

        if (typeEnd + 1 >= Input.Length || Input[typeEnd] != '>' || Input[typeEnd + 1] != '(')
            return false;

        Position = typeEnd + 1;
        token = new GenericFunctionToken(
            Input[start..identifierEnd],
            Input[typeStart..typeEnd],
            new TextSpan(start, Position - start));
        return true;
    }

    private bool TryScanMethodAccess(int start, int identifierEnd, out Token token)
    {
        token = null!;
        if (identifierEnd + 2 >= Input.Length || Input[identifierEnd] != '.')
            return false;

        var methodStart = identifierEnd + 1;
        var input = Input.AsSpan();
        if (!IsMethodNameStart(input, methodStart))
            return false;

        var methodEnd = methodStart + FastCharacterClassifier.GetIdentifierCodePointLength(input, methodStart);
        while (methodEnd < Input.Length && IsMethodNameContinue(input, methodEnd))
            methodEnd += FastCharacterClassifier.GetIdentifierCodePointLength(input, methodEnd);

        if (methodEnd >= Input.Length || Input[methodEnd] != '(')
            return false;

        Position = identifierEnd;
        token = new MethodAccessToken(Input[start..identifierEnd], new TextSpan(start, identifierEnd - start));
        return true;
    }

    private bool TryScanAccessToken(int start, int identifierEnd, out Token token)
    {
        token = null!;
        var bracketStart = identifierEnd;
        var innerStart = bracketStart + 1;
        if (innerStart >= Input.Length)
            return false;

        var name = Input[start..identifierEnd];
        var position = innerStart;
        if (Input[position] == '-')
            position++;

        var digitsStart = position;
        while (position < Input.Length && FastCharacterClassifier.IsDigit(Input[position]))
            position++;

        if (position > digitsStart && position < Input.Length && Input[position] == ']')
        {
            Position = position + 1;
            token = new NumericAccessToken(
                name,
                Input[innerStart..position],
                new TextSpan(start, Position - start),
                IsSchemaContext);
            return true;
        }

        if (Input[innerStart] == '\'')
        {
            position = innerStart + 1;
            while (position < Input.Length && IsKeyAccessConstChar(Input[position]))
                position++;

            if (position > innerStart + 1 &&
                position + 1 < Input.Length &&
                Input[position] == '\'' &&
                Input[position + 1] == ']')
            {
                Position = position + 2;
                token = new KeyAccessToken(
                    name,
                    Input[innerStart..(position + 1)],
                    new TextSpan(start, Position - start));
                return true;
            }
        }

        position = innerStart;
        while (position < Input.Length && IsKeyAccessVarChar(Input[position]))
            position++;

        if (position > innerStart && position < Input.Length && Input[position] == ']')
        {
            Position = position + 1;
            token = new KeyAccessToken(
                name,
                Input[innerStart..position],
                new TextSpan(start, Position - start));
            return true;
        }

        return false;
    }

    private static bool IsMethodNameStart(ReadOnlySpan<char> input, int index)
    {
        return index < input.Length &&
            (input[index] == '-' || FastCharacterClassifier.IsIdentifierStart(input, index));
    }

    private static bool IsMethodNameContinue(ReadOnlySpan<char> input, int index)
    {
        return index < input.Length &&
            (input[index] == '-' || FastCharacterClassifier.IsIdentifierContinue(input, index));
    }

    private static bool IsKeyAccessConstChar(char value)
    {
        return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    }

    private static bool IsKeyAccessVarChar(char value)
    {
        return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or
            ' ' or '\t' or '\r' or '\n' or '+' or '-' or '*' or '/' or '%' or '(' or ')';
    }
}
