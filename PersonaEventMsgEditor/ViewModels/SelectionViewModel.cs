using AtlusScriptLibrary.MessageScriptLanguage;
using PersonaEventMsgEditor.Models.Event;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace PersonaEventMsgEditor.ViewModels;
public class SelectionViewModel : ViewModelBase, IDialogViewModel
{
    private SelectionDialog _dialog;

    public ObservableCollection<SelectionOption> Options { get; } = new();

    public string Name => _dialog.Name;

    public ReactiveCommand<IDialogViewModel, Unit> GotFocusCommand { get; }

    public SelectionViewModel(SelectionDialog dialog, ReactiveCommand<IDialogViewModel, Unit> gotFocusCommand)
    {
        GotFocusCommand = gotFocusCommand;
        _dialog = dialog;
        
        foreach(var option in dialog.Options)
        {
            // TODO make code from PageViewModel shared so I can convert the tokens to text
            Options.Add(new SelectionOption(option.Tokens));
        }
    }

    public void Save()
    {
        _dialog.Options.Clear();

        foreach(var option in Options)
        {
            _dialog.Options.Add(option.GetTokenText());
        }
    }
}
