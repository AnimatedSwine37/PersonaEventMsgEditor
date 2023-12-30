using AtlusScriptLibrary.MessageScriptLanguage;
using ReactiveUI;
using System.Collections.ObjectModel;
using static PersonaEventMsgEditor.Models.Event.Bustup;

namespace PersonaEventMsgEditor.ViewModels;
public class MessageViewModel : ViewModelBase, IDialogViewModel
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

    public int? VoiceIndex => _selectedPage?.VoiceId;

    private MessageDialog _dialog;

    // Just for the design time use!
    internal MessageViewModel(string name, string speaker)
    {
        Name = name;
        Speaker = speaker;
    }

    public MessageViewModel(MessageDialog dialog)
    {
        _dialog = dialog;
        _name = dialog.Name;
        _speaker = dialog.Speaker?.ToString();

        BustupViewModel? bustup = null;
        foreach (var page in dialog.Pages)
        {
            var pageVm = new PageViewModel(page);
            Pages.Add(pageVm);
            // Propogate bustup between pages (if not explicitly set)
            if (pageVm.Bustup.Exists)
            {
                bustup = pageVm.Bustup.Clone();
            }
            else if (bustup != null)
            {
                pageVm.Bustup = bustup;
            }
        }

        if (Pages.Count > 0)
            _selectedPage = Pages[0];
    }

    /// <summary>
    /// Saves the message's changes to its underlying <see cref="MessageDialog"/>
    /// </summary>
    public void Save()
    {
        if(!string.IsNullOrEmpty(Speaker))
            _dialog.Speaker = new NamedSpeaker(Speaker);
        
        foreach (var page in Pages)
        {
            page.Save();
        }
    }

}


public class DesignMessageViewModel : MessageViewModel
{
    public DesignMessageViewModel() : base("Test MSG", "Yukari")
    {
        var bustup = new BustupViewModel(BustupCharacter.Yukari, 1, 1, BustupPosition.Right, new(), new());
        Pages.Add(new PageViewModel("This is a test message\nIt goes over two lines", bustup, null));
        SelectedPage = Pages[0];
    }
}