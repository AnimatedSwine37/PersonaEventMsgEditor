using PersonaEventMsgEditor.Models;
using ReactiveUI;

namespace PersonaEventMsgEditor.ViewModels;
public class EditorViewModel : ViewModelBase
{
    private Event? _event;
    public Event? Event
    {
        get => _event;
        set {
            this.RaiseAndSetIfChanged(ref _event, value);
            Preview.Event = value;
        }
    }

    public PreviewViewModel Preview { get; } = new();

    public EditorViewModel()
    {

    }
}
