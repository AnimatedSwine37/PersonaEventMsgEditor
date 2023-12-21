using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using PersonaEventMsgEditor.Models.Event;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static PersonaEventMsgEditor.Models.Event.Bustup;
using IToken = AtlusScriptLibrary.MessageScriptLanguage.IToken;

namespace PersonaEventMsgEditor.ViewModels;
public class EventMessageViewModel : ViewModelBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _text;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private string _speaker;
    public string Speaker
    {
        get => _speaker;
        set => this.RaiseAndSetIfChanged(ref _speaker, value);
    }

    private BustupViewModel _bustup;
    public BustupViewModel Bustup
    {
        get => _bustup;
        private set => this.RaiseAndSetIfChanged(ref _bustup, value);
    }

    private Voice? _voice;
    public Voice? Voice
    {
        get => _voice;
        set => this.RaiseAndSetIfChanged(ref _voice, value);
    }

    private int _eventMajor;
    private int _eventMinor;

    private MessageDialog _dialog;

    public EventMessageViewModel(MessageDialog dialog, int eventMajor, int eventMinor)
    {
        _eventMajor = eventMajor;
        _eventMinor = eventMinor;
        _dialog = dialog;
        Name = dialog.Name;
        Speaker = "To Implement";
        Bustup = GetBustup();
        Text = GetText();
        Voice = GetVoice();
    }

    private string GetText()
    {
        // TODO deal with multiple pages
        var page = _dialog.Pages[0];
        StringBuilder sb = new();

        foreach (var token in page.Tokens)
        {
            var str = PrintToken(token);
            if (str != null) sb.Append(str);
        }
        return sb.ToString();
    }

    private Voice? GetVoice()
    {
        // TODO deal with multiple pages
        var page = _dialog.Pages[0];

        var funcs = page.Tokens.Where(x => x.Kind == TokenKind.Function)
            .Select(x => (FunctionToken)x);
        // Check if the bustup function is ever called
        if (!funcs.Any(x => x.FunctionTableIndex == 2 && x.FunctionIndex == 8))
            return null;

        var voiceFunc = funcs.First(x => x.FunctionTableIndex == 2 && x.FunctionIndex == 8);

        int id = voiceFunc.Arguments[0];
        return new Voice(id, _eventMajor, _eventMinor);
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
        // TODO deal with multiple pages
        var page = _dialog.Pages[0];

        var funcs = page.Tokens.Where(x => x.Kind == TokenKind.Function)
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
    /// Saves the message's changes to its underlying <see cref="MessageDialog"/>
    /// </summary>
    public void Save()
    {
        // TODO deal with multiple pages
        var tokens = _dialog.Pages[0].Tokens;
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
        if (_voice != null)
        {
            tokens.Add(_voice.GetToken());
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
        if (_voice != null)
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
