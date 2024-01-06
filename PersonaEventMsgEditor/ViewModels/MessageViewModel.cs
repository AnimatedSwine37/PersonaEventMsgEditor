using AtlusScriptLibrary.MessageScriptLanguage;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
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
    internal MessageViewModel(string name)
    {
        Name = name;
    }

    public ReactiveCommand<IDialogViewModel,Unit> GotFocusCommand { get; }

    public MessageViewModel(MessageDialog dialog, ReactiveCommand<IDialogViewModel,Unit> gotFocusCommand)
    {
        GotFocusCommand = gotFocusCommand;
        _dialog = dialog;
        _name = dialog.Name;
        var lastSpeaker = dialog.Speaker?.ToString();

        BustupViewModel? bustup = null;
        foreach (var page in dialog.Pages)
        {
            var pageVm = new PageViewModel(page, lastSpeaker);

            // Propogate speakers overriden by the page's message
            if(lastSpeaker != pageVm.Speaker)
                lastSpeaker = pageVm.Speaker;

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
        var lastSpeaker = Pages[0].Speaker;
        if (!string.IsNullOrEmpty(lastSpeaker))
            _dialog.Speaker = new NamedSpeaker(lastSpeaker);
        
        foreach (var page in Pages)
        {
            page.Save(lastSpeaker);
        }
    }

}


public class DesignMessageViewModel : MessageViewModel
{
    public DesignMessageViewModel() : base("Test MSG")
    {
        var bustup = new BustupViewModel(BustupCharacter.Yukari, 1, 1, BustupPosition.Right, new(), new());
        Pages.Add(new PageViewModel("This is a test message\nIt goes over two lines", "Yukari", bustup, null));
        SelectedPage = Pages[0];
    }
}