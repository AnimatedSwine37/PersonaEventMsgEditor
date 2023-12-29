using AtlusScriptLibrary.MessageScriptLanguage;
using ReactiveUI;
using System.Collections.Generic;

namespace PersonaEventMsgEditor.Models.Event;
public class SelectionOption : ReactiveObject
{
    private string _text;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    public SelectionOption(List<IToken> tokens)
    {
        _text = Message.GetText(tokens);
    }

    public TokenText GetTokenText()
    {
        List<IToken> tokens = new()
        {
            new FunctionToken(0, 8, 65278), // start functions
            new FunctionToken(1, 31)
        };
        tokens.AddRange(Message.GetTokens(Text));
        return new TokenText(tokens);
    }
}
