using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;

namespace PersonaEventMsgEditor.ViewModels;
public class EditorViewModel : ViewModelBase
{
    private Event _event;
    public ObservableCollection<DialogEditorViewModel> Dialogs { get; } = new();

    private int _selectedDialog;
    public int SelectedDialog
    {
        get => _selectedDialog;
        set => this.RaiseAndSetIfChanged(ref _selectedDialog, value);
    }

    public ObservableCollection<PreviewViewModel> Previews { get; } = new();

    public string Name => $"Event {_event.MajorId:D3} {_event.MinorId:D3}";

    public EditorViewModel(Event @event)
    {
        _event = @event;
        
        foreach(var message in _event.Messages)
        {
            Dialogs.Add(new DialogEditorViewModel(message));
            Previews.Add(new PreviewViewModel(message));
        }

        RxApp.MainThreadScheduler.Schedule(LoadBustups);
    }

    public void SaveEvent()
    {
        _event.Save();
    }

    private async void LoadBustups()
    {
        foreach(var preview in Previews.ToList())
        {
            await preview.LoadBustup();
        }
    }
}
