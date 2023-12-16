using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersonaEventMsgEditor.Models;

/// <summary>
/// Represents an individual message in an event
/// </summary>
public class EventMessage
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Speaker { get; set; }
    public string Bustup { get; set; }
    public string Voice { get; set; }

    private MessageScriptDecompiler _decompiler;
    private MessageDialog _dialog;

    public EventMessage(MessageDialog dialog)
    {
        Name = dialog.Name;
        Speaker = "To Implement";

        using var msgWriter = new StringWriter();
        _decompiler = new MessageScriptDecompiler(msgWriter);
        // TODO change this so the text is just the actual text and other stuff like speaker and bustup are separate variables
        // (Need to decide how they'll be placed on the UI and whether Wxnder even wants that)
        _decompiler.Decompile(dialog);
        Text = msgWriter.ToString();
    }
}
