using Avalonia.Platform.Storage;
using PersonaEventMsgEditor.Models.Files;
using PersonaEventMsgEditor.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using AtlusScriptLibrary.Common.Text.Encodings;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Linq;

namespace PersonaEventMsgEditor.ViewModels;
public class EventViewModel : ViewModelBase
{
    public string Path => _file.Path.AbsolutePath;
    public string Name => _file.Name;
    public int MajorId { get; }
    public int MinorId { get; }
    public ObservableCollection<EventMessageViewModel> Messages { get; } = new();
    private MessageScript _messageScript { get; set; }
    private PMD _pmd;
    private IStorageFile _file;

    public EventViewModel(IStorageFile file, int majorId, int minorId, MessageScript messageScript, PMD pmd)
    {
        _file = file;
        MajorId = majorId;
        MinorId = minorId;
        _messageScript = messageScript;
        _pmd = pmd;

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

        RxApp.MainThreadScheduler.Schedule(LoadBustups);
    }

    private async void LoadBustups()
    {
        foreach (var message in Messages.ToList())
        {
            await message.LoadBustupAsync();
        }
    }

    public static async Task<EventViewModel> FromFileAsync(IStorageFile file)
    {
        int major = 0;
        int minor = 0;

        var match = Regex.Match(file.Name, @"E(\d+)_(\d+)\.PM1", RegexOptions.IgnoreCase);
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
        StringBuilder sb = new();
        foreach (var dialog in Messages)
        {
            sb.AppendLine(dialog.Text);
        }

        var compiler = new MessageScriptCompiler(FormatVersion.Version1, AtlusEncoding.Persona3);
        var compiled = compiler.Compile(sb.ToString());

        using var newMessageScript = compiled.ToStream();

        // TODO open the file as read-write from the start so we're not copying between so many streams
        await _pmd.InjectMessage(newMessageScript);
        await _pmd.ToFile(_file);
    }
}
