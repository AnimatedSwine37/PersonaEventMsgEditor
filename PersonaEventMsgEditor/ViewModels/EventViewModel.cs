using Avalonia.Platform.Storage;
using PersonaEventMsgEditor.Models.Files;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.Common.Text.Encodings;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using System.Linq;
using System;
using System.Reactive;

namespace PersonaEventMsgEditor.ViewModels;
public class EventViewModel : ViewModelBase
{
    public string Path => _file.Path.AbsolutePath;
    public string Name => _file.Name;
    public int MajorId { get; }
    public int MinorId { get; }
    public ObservableCollection<IDialogViewModel> Dialogs { get; } = new();

    private IDialogViewModel? _selectedDialog;
    public IDialogViewModel? SelectedDialog
    {
        get => _selectedDialog;
        set => this.RaiseAndSetIfChanged(ref _selectedDialog, value);
    }

    private readonly ObservableAsPropertyHelper<MessageViewModel?> _selectedMessage;
    public MessageViewModel? SelectedMessage => _selectedMessage.Value;

    private readonly ObservableAsPropertyHelper<SelectionViewModel?> _selectedSelection;
    public SelectionViewModel? SelectedSelection => _selectedSelection.Value;

    private AudioPlayerViewModel _audioPlayer;
    public AudioPlayerViewModel AudioPlayer
    {
        get => _audioPlayer;
        set => this.RaiseAndSetIfChanged(ref _audioPlayer, value);
    }

    public ReactiveCommand<IDialogViewModel,Unit> DialogFocusedCommand { get; }

    private MessageScript _messageScript;
    private PMD _pmd;
    private IStorageFile _file;

    public EventViewModel(IStorageFile file, int majorId, int minorId, MessageScript messageScript, PMD pmd)
    {
        _file = file;
        MajorId = majorId;
        MinorId = minorId;
        _messageScript = messageScript;
        _pmd = pmd;
        AudioPlayer = new AudioPlayerViewModel(majorId, minorId);

        DialogFocusedCommand = ReactiveCommand.Create<IDialogViewModel, Unit>(x =>
        {
            SelectedDialog = x;
            return Unit.Default;
        });

        foreach (var dialog in messageScript.Dialogs)
        {
            if (dialog.Kind == DialogKind.Message)
            {
                Dialogs.Add(new MessageViewModel((MessageDialog)dialog, DialogFocusedCommand));
            }
            else 
            {
                Dialogs.Add(new SelectionViewModel((SelectionDialog)dialog, DialogFocusedCommand));
            }
        }

        this.WhenAnyValue(x => x.SelectedDialog)
            .Select(x => x is SelectionViewModel ? (SelectionViewModel)x : null)
            .ToProperty(this, x => x.SelectedSelection, out _selectedSelection);


        this.WhenAnyValue(x => x.SelectedDialog)
                   .Select(x => x is MessageViewModel ? (MessageViewModel)x : GetLastMessage(x))
                   .ToProperty(this, x => x.SelectedMessage, out _selectedMessage);

        this.WhenAnyValue(x => x.SelectedMessage.VoiceIndex)
            .Subscribe(x => { AudioPlayer.AdxIndex = x; });
    }

    // Gets the first message before the specified dialog 
    // If a selection is displayed then the message before it also is
    private MessageViewModel? GetLastMessage(IDialogViewModel? dialog)
    {
        if (dialog == null) return null;

        var dialogs = Dialogs.ToList();
        for(int i = dialogs.IndexOf(dialog); i >= 0; i--)
        {
            if (dialogs[i] is MessageViewModel)
                return (MessageViewModel)dialogs[i];
        }
        return null;
    }

    public static async Task<EventViewModel> FromFileAsync(IStorageFile file)
    {
        int major = 0;
        int minor = 0;

        var match = Regex.Match(file.Name, @"E(\d+)_(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            major = int.Parse(match.Groups[1].Value);
            minor = int.Parse(match.Groups[2].Value);
        }

        // TODO see if I can get this to be opened as read-write using the IStorageFile instead of having to do this extra stuff
        using var eventFile = await file.OpenReadAsync();
        var eventStream = new MemoryStream();
        await eventFile.CopyToAsync(eventStream);
        var pmd = PMD.FromStream(eventStream);
        var messageScript = MessageScript.FromStream(pmd.Message, FormatVersion.Version1, AtlusEncoding.Persona3, true);

        return new EventViewModel(file, major, minor, messageScript, pmd);
    }

    public static FilePickerFileType EventFile { get; } = new("Atlus Event File")
    {
        Patterns = new[] { "*.pm1" }
    };

    public async void Save()
    {
        foreach (var message in Dialogs.ToList())
        {
            message.Save();
        }

        using var newMessageScript = _messageScript.ToStream();

        // TODO open the file as read-write from the start so we're not copying between so many streams
        await _pmd.InjectMessage(newMessageScript);
        await _pmd.ToFile(_file);
    }
}
