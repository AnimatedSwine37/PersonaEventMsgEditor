using ReactiveUI;
using static PersonaEventMsgEditor.Models.Event.Bustup;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using AtlusScriptLibrary.MessageScriptLanguage;
using System.Linq;
using PersonaEventMsgEditor.Models.Event;

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

    // Just for design time use!
    internal PageViewModel(string text, BustupViewModel bustup, int? voiceId)
    {
        Text = text;
        Bustup = bustup;
        VoiceId = voiceId;
    }

    public PageViewModel(TokenText dialog)
    {
        _dialog = dialog;
        _bustup = GetBustup();
        _text = Message.GetText(dialog.Tokens);
        VoiceId = GetVoiceId();
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

        // Add the user's actual text
        tokens.AddRange(Message.GetTokens(Text));

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
}
