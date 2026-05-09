using System.Buffers;

namespace EditorLearningTask;

public sealed class Lexer
{
    private static readonly SearchValues<char> WhitespaceChars = SearchValues.Create(" \t\r\n\f\v");
    private static readonly SearchValues<char> DigitChars = SearchValues.Create("0123456789");
    private static readonly SearchValues<char> SymbolChars = SearchValues.Create(",;().=*<>!+-/");
    private static readonly SearchValues<char> IdentifierContinueChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");

    [ThreadStatic]
    private static List<Token>? _scratchTokens;
    private static List<Token> ScratchTokens => _scratchTokens ??= new List<Token>(128);

    // Fills `output[0..count-1]` with tokenized lines. The caller owns the output array.
    public static void Tokenize(string[] lines, int count, IReadOnlyList<Token>[] output)
    {
        int lineEndState = 0;
        var scratch = ScratchTokens;

        for (int lineNum = 0; lineNum < count; lineNum++)
        {
            scratch.Clear();

            var lineMemory = lines[lineNum].AsMemory();
            var span = lineMemory.Span;
            int length = span.Length;
            int position = 0;

            if (lineEndState == SqlTokenTypes.TOKEN_COMMENT)
            {
                var endIndex = span.IndexOf("*/".AsSpan());
                if (endIndex >= 0)
                {
                    position = endIndex + 2;
                    lineEndState = 0;
                    scratch.Add(new Token(
                        Start: 0,
                        Length: endIndex + 2,
                        Value: SqlTokenTypes.TOKEN_COMMENT,
                        Text: lineMemory[..(endIndex + 2)]));
                }
                else
                {
                    scratch.Add(new Token(Start: 0, length, SqlTokenTypes.TOKEN_COMMENT, lineMemory));
                    output[lineNum] = scratch.ToArray();
                    continue;
                }
            }
            else if (lineEndState == SqlTokenTypes.TOKEN_STRING)
            {
                int closeIndex = FindStringClose(span, 0);
                if (closeIndex >= 0)
                {
                    scratch.Add(new Token(0, closeIndex, SqlTokenTypes.TOKEN_STRING, lineMemory[..closeIndex]));
                    position = closeIndex;
                    lineEndState = 0;
                }
                else
                {
                    scratch.Add(new Token(0, length, SqlTokenTypes.TOKEN_STRING, lineMemory));
                    output[lineNum] = scratch.ToArray();
                    continue;
                }
            }

            TokenizeLine(span, lineMemory, scratch, ref position, ref lineEndState);
            output[lineNum] = scratch.ToArray();
        }
    }

    private static void TokenizeLine(
        ReadOnlySpan<char> span,
        ReadOnlyMemory<char> memory,
        List<Token> tokens,
        ref int position,
        ref int lineEndState)
    {
        int length = span.Length;

        while (position < length)
        {
            char c = span[position];

            if (WhitespaceChars.Contains(c))
            {
                int run = span[position..].IndexOfAnyExcept(WhitespaceChars);
                int end = run < 0 ? length : position + run;
                tokens.Add(new Token(position, end - position, SqlTokenTypes.TOKEN_WHITESPACE, memory[position..end]));
                position = end;
                continue;
            }

            if (c == '-' && position + 1 < length && span[position + 1] == '-')
            {
                tokens.Add(new Token(position, length - position, SqlTokenTypes.TOKEN_COMMENT, memory[position..]));
                return;
            }

            if (c == '/' && position + 1 < length && span[position + 1] == '*')
            {
                int endIdx = span[(position + 2)..].IndexOf("*/".AsSpan());
                if (endIdx >= 0)
                {
                    int tokenEnd = position + 2 + endIdx + 2;
                    tokens.Add(new Token(position, tokenEnd - position, SqlTokenTypes.TOKEN_COMMENT, memory[position..tokenEnd]));
                    position = tokenEnd;
                }
                else
                {
                    lineEndState = SqlTokenTypes.TOKEN_COMMENT;
                    tokens.Add(new Token(position, length - position, SqlTokenTypes.TOKEN_COMMENT, memory[position..]));
                    return;
                }
                continue;
            }

            if (c == '\'')
            {
                int closeIdx = FindStringClose(span, position + 1);
                if (closeIdx >= 0)
                {
                    tokens.Add(new Token(position, closeIdx - position, SqlTokenTypes.TOKEN_STRING, memory[position..closeIdx]));
                    position = closeIdx;
                }
                else
                {
                    lineEndState = SqlTokenTypes.TOKEN_STRING;
                    tokens.Add(new Token(position, length - position, SqlTokenTypes.TOKEN_STRING, memory[position..]));
                    return;
                }
                continue;
            }

            if (DigitChars.Contains(c))
            {
                int run = span[position..].IndexOfAnyExcept(DigitChars);
                int end = run < 0 ? length : position + run;
                tokens.Add(new Token(position, end - position, SqlTokenTypes.TOKEN_NUMBER, memory[position..end]));
                position = end;
                continue;
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                int run = span[position..].IndexOfAnyExcept(IdentifierContinueChars);
                int end = run < 0 ? length : position + run;
                int kwType = SqlTokenTypes.GetKeywordToken(span[position..end]);
                tokens.Add(new Token(position, end - position,
                    kwType >= 0 ? kwType : SqlTokenTypes.TOKEN_IDENTIFIER,
                    memory[position..end]));
                position = end;
                continue;
            }

            if (SymbolChars.Contains(c))
            {
                tokens.Add(new Token(position, 1, SqlTokenTypes.TOKEN_SYMBOL, memory[position..(position + 1)]));
                position++;
                continue;
            }

            tokens.Add(new Token(position, 1, SqlTokenTypes.TOKEN_UNKNOWN, memory[position..(position + 1)]));
            position++;
        }
    }

    /// <summary>
    /// Returns the position AFTER the closing quote, or -1 if the string is not closed on this line.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="position">Position must point to the character immediately after the opening quote</param>
    private static int FindStringClose(ReadOnlySpan<char> span, int position)
    {
        while (position < span.Length)
        {
            int rel = span[position..].IndexOf('\'');
            if (rel < 0)
            {
                return -1;
            }

            int abs = position + rel;
            // '' is an escaped quote — skip both and continue
            if (abs + 1 < span.Length && span[abs + 1] == '\'')
            {
                position = abs + 2;
                continue;
            }

            return abs + 1; // position after closing quote
        }
        
        return -1;
    }
}