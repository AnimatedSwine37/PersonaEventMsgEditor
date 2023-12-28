using ReactiveUI;
using static PersonaEventMsgEditor.Models.Event.Bustup;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using AtlusScriptLibrary.MessageScriptLanguage;
using System.Linq;

namespace PersonaEventMsgEditor.ViewModels;
public class PageViewModel : ViewModelBase
{
    private string _text;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private BustupViewModel _bustup;
    public BustupViewModel Bustup
    {
        get => _bustup;
        set => this.RaiseAndSetIfChanged(ref _bustup, value);
    }

    private int? _voiceId;
    public int? VoiceId
    {
        get => _voiceId;
        set => this.RaiseAndSetIfChanged(ref _voiceId, value);
    }

    private TokenText _dialog;

    public PageViewModel(TokenText dialog)
    {
        _dialog = dialog;
        Bustup = GetBustup();
        Text = GetText();
        VoiceId = GetVoiceId();
    }

    private string GetText()
    {
        // TODO deal with multiple pages
        StringBuilder sb = new();

        foreach (var token in _dialog.Tokens)
        {
            var str = PrintToken(token);
            if (str != null) sb.Append(str);
        }
        return sb.ToString();
    }

    private int? GetVoiceId()
    {
        var funcs = _dialog.Tokens.Where(x => x.Kind == TokenKind.Function)
            .Select(x => (FunctionToken)x);
        // Check if the bustup function is ever called
        if (!funcs.Any(x => x.FunctionTableIndex == 2 && x.FunctionIndex == 8))
            return null;

        var voiceFunc = funcs.First(x => x.FunctionTableIndex == 2 && x.FunctionIndex == 8);

        return voiceFunc.Arguments[0];
    }

    private string? PrintToken(IToken token)
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

    HashSet<(int, int)> _ignoredFuncs = new HashSet<(int, int)>
    {
        (0, 31), // bustup (handled in GetBustup)
        (2, 8), // voice line
        (0, 8), // start
        (1, 31), // the other start
        (0, 4), // wait
        (2, 7) // auto wait (maybe used in conjunction with voice lines TODO confirm this)
    };

    private string? PrintFuncToken(FunctionToken token)
    {
        if (_ignoredFuncs.Contains((token.FunctionTableIndex, token.FunctionIndex)))
            return null;

        var args = string.Join(' ', token.Arguments);
        if (token.Arguments.Count > 0) args = " " + args;
        return $"[f {token.FunctionTableIndex} {token.FunctionIndex}{args}]";
    }

    private BustupViewModel GetBustup()
    {
        var funcs = _dialog.Tokens.Where(x => x.Kind == TokenKind.Function)
            .Select(x => (FunctionToken)x);
        // Check if the bustup function is ever called
        if (!funcs.Any(x => x.FunctionTableIndex == 0 && x.FunctionIndex == 31))
            return new BustupViewModel();

        var bustupFunc = funcs.First(x => x.FunctionTableIndex == 0 && x.FunctionIndex == 31);

        int character = bustupFunc.Arguments[0];
        int outfit = bustupFunc.Arguments[1];
        int emotion = bustupFunc.Arguments[2];
        int position = bustupFunc.Arguments[3];
        return new BustupViewModel((BustupCharacter)character, outfit, emotion, (BustupPosition)position);
    }

    /// <summary>
    /// Saves the message's changes to its underlying <see cref="TokenText"/>
    /// </summary>
    public void Save()
    {
        var tokens = _dialog.Tokens;
        tokens.Clear();

        // Add start tokens
        tokens.Add(new FunctionToken(0, 8, 65278));
        tokens.Add(new FunctionToken(1, 31));

        // Add bustup token
        if (Bustup.Exists)
        {
            tokens.Add(Bustup.GetToken());
        }

        // Add voice token
        if (VoiceId != null)
        {
            tokens.Add(new FunctionToken(2, 8, (ushort)VoiceId));
        }

        // Add text and any functions in it 
        int index = 0;
        foreach (Match match in Regex.Matches(Text, @"\[f((?: \d+)+)\]"))
        {
            if (match.Index > index)
            {
                AddStringToken(tokens, Text.Substring(index, match.Index - index));
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

        if (index < Text.Length)
        {
            AddStringToken(tokens, Text.Substring(index));
        }

        // Add a new line at the end (not sure if really necessary but the game has it like that)
        if (tokens.Last().Kind != TokenKind.NewLine)
            tokens.Add(new NewLineToken());

        // Add end tokens
        if (VoiceId != null)
        {
            tokens.Add(new FunctionToken(2, 7, 10));
        }
        tokens.Add(new FunctionToken(0, 4));
    }

    private void AddStringToken(List<IToken> tokens, string text)
    {
        int index = 0;
        foreach (Match match in Regex.Matches(text, "\n"))
        {
            if (match.Index > index)
            {
                tokens.Add(new StringToken(text.Substring(index, match.Index - index)));
                tokens.Add(new NewLineToken());
                index = match.Index + match.Length;
            }
        }

        if (index < Text.Length)
        {
            tokens.Add(new StringToken(Text.Substring(index)));
        }
    }
}
