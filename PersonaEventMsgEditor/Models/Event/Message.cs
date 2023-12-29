using AtlusScriptLibrary.MessageScriptLanguage;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PersonaEventMsgEditor.Models.Event;
public class Message
{
    /// <summary>
    /// Converts a List of <see cref="IToken"/> to a string representation
    /// </summary>
    /// <param name="tokens">Tokens to convert to a string</param>
    /// <returns>A string representation of <paramref name="tokens"/></returns>
    public static string GetText(List<IToken> tokens)
    {
        StringBuilder sb = new();

        foreach (var token in tokens)
        {
            var str = PrintToken(token);
            if (str != null) sb.Append(str);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string representation of a message back to a List of <see cref="IToken"/>
    /// </summary>
    /// <param name="text">The message</param>
    /// <returns>A string representation of <paramref name="text"/></returns>
    public static List<IToken> GetTokens(string text)
    {
        List<IToken> tokens = new();
        int index = 0;
        foreach (Match match in Regex.Matches(text, @"\[f((?: \d+)+)\]"))
        {
            if (match.Index > index)
            {
                AddStringToken(tokens, text, text.Substring(index, match.Index - index));
                index = match.Index + match.Length;
            }

            List<ushort> args = new();
            foreach (Match argMatch in Regex.Matches(match.Groups[1].Value, @"\s(\d+)"))
            {
                args.Add(ushort.Parse(argMatch.Groups[1].Value));
            }
            if (args.Count < 2) continue;

            if (args.Count > 2)
                tokens.Add(new FunctionToken(args[0], args[1], args.Skip(2).ToArray()));
            else
                tokens.Add(new FunctionToken(args[0], args[1]));
        }

        if (index < text.Length)
        {
            AddStringToken(tokens, text, text.Substring(index));
        }

        return tokens;
    }

    private static void AddStringToken(List<IToken> tokens, string text, string subStr)
    {
        int index = 0;
        foreach (Match match in Regex.Matches(subStr, "\n"))
        {
            if (match.Index > index)
            {
                tokens.Add(new StringToken(subStr.Substring(index, match.Index - index)));
                tokens.Add(new NewLineToken());
                index = match.Index + match.Length;
            }
        }

        if (index < text.Length)
        {
            tokens.Add(new StringToken(text.Substring(index)));
        }
    }


    private static string? PrintToken(IToken token)
    {
        switch (token.Kind)
        {
            case TokenKind.String:
                return ((StringToken)token).Value;
            case TokenKind.NewLine:
                return "\n";
            case TokenKind.Function:
                return PrintFuncToken((FunctionToken)token);
            case TokenKind.CodePoint:
                return ((CodePointToken)token).ToString();
            default:
                return null;
        }
    }

    private static HashSet<(int, int)> _ignoredFuncs = new HashSet<(int, int)>
    {
        (0, 31), // bustup (handled in GetBustup)
        (2, 8), // voice line
        (0, 8), // start
        (1, 31), // the other start
        (0, 4), // wait
        (2, 7) // auto wait (maybe used in conjunction with voice lines TODO confirm this)
    };

    private static string? PrintFuncToken(FunctionToken token)
    {
        if (_ignoredFuncs.Contains((token.FunctionTableIndex, token.FunctionIndex)))
            return null;

        var args = string.Join(' ', token.Arguments);
        if (token.Arguments.Count > 0) args = " " + args;
        return $"[f {token.FunctionTableIndex} {token.FunctionIndex}{args}]";
    }
}
