using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using DynamicData;
using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;
public class EditorViewModel : ViewModelBase
{
    private Event _event;
    public ObservableCollection<DialogEditorViewModel> Dialogs { get; } = new();

    private DialogEditorViewModel? _selectedDialog;
    public DialogEditorViewModel? SelectedDialog
    {
        get => _selectedDialog;
        set => this.RaiseAndSetIfChanged(ref _selectedDialog, value);
    }

    public PreviewViewModel Preview { get; } = new();

    public string Name => $"Event {_event.MajorId:D3} {_event.MinorId:D3}";

    public EditorViewModel(Event @event)
    {
        _event = @event;
        
        foreach(var message in _event.Messages)
        {
            var dialogEditor = new DialogEditorViewModel(message);
            Dialogs.Add(dialogEditor);
        }
    }

    public void SaveEvent()
    {
        _event.Save();
    }
}
