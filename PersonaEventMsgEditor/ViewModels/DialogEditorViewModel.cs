using AtlusScriptLibrary.MessageScriptLanguage;
using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace PersonaEventMsgEditor.ViewModels;
public class DialogEditorViewModel : ViewModelBase
{
    private EventMessage _message;
    public EventMessage Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public DialogEditorViewModel(EventMessage message)
    {
        Message = message;
    }
}
