using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using Avalonia.Platform.Storage;
using PersonaEventMsgEditor.Models.Files;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Models;
public class Event
{
    public string Path => _file.Path.AbsolutePath;
    public int MajorId { get; }
    public int MinorId { get; }
    public List<EventMessage> Messages { get; set; }
    private MessageScript _messageScript { get; set; }
    private PMD _pmd;
    private IStorageFile _file;

    public Event(IStorageFile file, int majorId, int minorId, MessageScript messageScript, PMD pmd)
    {
        _file = file;
        MajorId = majorId;
        MinorId = minorId;
        _messageScript = messageScript;
        _pmd = pmd;

        Messages = new();
        foreach(var dialog in messageScript.Dialogs)
        {
            if(dialog.Kind == DialogKind.Message)
            {
                Messages.Add(new EventMessage((MessageDialog)dialog));
            }
            else
            {
                // TODO make it work with selections as well
            }
        }
    }

    public static async Task<Event> FromFileAsync(IStorageFile file)
    {
        int major = 0;
        int minor = 0;

        var match = Regex.Match(file.Name, @"E(\d+)_(\d+)\.PM1", RegexOptions.IgnoreCase);
        if(match.Success)
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

        return new Event(file, major, minor, messageScript, pmd);
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
