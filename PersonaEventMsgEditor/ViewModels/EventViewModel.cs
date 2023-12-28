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

namespace PersonaEventMsgEditor.ViewModels;
public class EventViewModel : ViewModelBase
{
    public string Path => _file.Path.AbsolutePath;
    public string Name => _file.Name;
    public int MajorId { get; }
    public int MinorId { get; }
    public ObservableCollection<EventMessageViewModel> Messages { get; } = new();

    private EventMessageViewModel? _selectedMessage;
    public EventMessageViewModel? SelectedMessage
    {
        get => _selectedMessage;
        set => this.RaiseAndSetIfChanged(ref _selectedMessage, value);
    }

    private AudioPlayerViewModel _audioPlayer;
    public AudioPlayerViewModel AudioPlayer
    {
        get => _audioPlayer;
        set => this.RaiseAndSetIfChanged(ref _audioPlayer, value);
    }

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

        foreach (var dialog in messageScript.Dialogs)
        {
            if (dialog.Kind == DialogKind.Message)
            {
                Messages.Add(new EventMessageViewModel((MessageDialog)dialog));
            }
            else
            {
                // TODO make it work with selections as well
            }
        }

        this.WhenAnyValue(x => x.SelectedMessage.SelectedPage.VoiceId)
            .Subscribe(x => { AudioPlayer.AdxIndex = x; });

        RxApp.MainThreadScheduler.Schedule(LoadBustups);
    }

    private async void LoadBustups()
    {
        foreach (var message in Messages.ToList())
        {
            // TODO add this back when I've actually got them loading asynchronously in some way
            //await message.LoadBustupAsync();
        }
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
        foreach (var message in Messages.ToList())
        {
            message.Save();
        }

        using var newMessageScript = _messageScript.ToStream();

        // TODO open the file as read-write from the start so we're not copying between so many streams
        await _pmd.InjectMessage(newMessageScript);
        await _pmd.ToFile(_file);
    }
}
