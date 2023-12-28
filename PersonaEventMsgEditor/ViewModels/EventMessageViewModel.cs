using AtlusScriptLibrary.MessageScriptLanguage;
using PersonaEventMsgEditor.Models.Event;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace PersonaEventMsgEditor.ViewModels;
public class EventMessageViewModel : ViewModelBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string? _speaker;
    public string? Speaker
    {
        get => _speaker;
        set => this.RaiseAndSetIfChanged(ref _speaker, value);
    }

    public ObservableCollection<PageViewModel> Pages { get; } = new();

    private PageViewModel? _selectedPage;
    public PageViewModel? SelectedPage
    {
        get => _selectedPage;
        set => this.RaiseAndSetIfChanged(ref _selectedPage, value);
    }

    private MessageDialog _dialog;

    public EventMessageViewModel(MessageDialog dialog)
    {
        _dialog = dialog;
        _name = dialog.Name;
        _speaker = dialog.Speaker.ToString();

        BustupViewModel? bustup = null;
        foreach(var page in dialog.Pages)
        {
            var pageVm = new PageViewModel(page);
            Pages.Add(pageVm);
            // Propogate bustup between pages (if not explicitly set)
            if(pageVm.Bustup.Exists)
            {
                bustup = pageVm.Bustup.Clone();
            }
            else if(bustup != null)
            {
                pageVm.Bustup = bustup;
            }
        }

        if(Pages.Count > 0)
            _selectedPage = Pages[0];
    }

    /// <summary>
    /// Saves the message's changes to its underlying <see cref="MessageDialog"/>
    /// </summary>
    public void Save()
    {
        foreach(var page in Pages)
        {
            page.Save();
        }
    }

}
