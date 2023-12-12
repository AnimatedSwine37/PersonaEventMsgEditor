using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using Avalonia.Platform.Storage;
using PersonaEventMsgEditor.Models.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Models;
public class Event
{
    public string Path { get; set; }
    public int MajorId { get; set; }
    public int MinorId { get; set; }
    public MessageScript MessageScript { get; set; }

    private PMD _pmd;

    public Event(string path, int majorId, int minorId, MessageScript messageScript, PMD pmd)
    {
        Path = path;
        MajorId = majorId;
        MinorId = minorId;
        MessageScript = messageScript;
        _pmd = pmd;
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

        var eventFile = await file.OpenReadAsync();
        var pmd = PMD.FromStream(eventFile);
        var messageScript = MessageScript.FromStream(pmd.Message, FormatVersion.Version1, AtlusEncoding.Persona3, true);

        return new Event(file.Path.ToString(), major, minor, messageScript, pmd);
    }

    public static FilePickerFileType EventFile { get; } = new("Atlus Event File")
    {
        Patterns = new[] { "*.pm1" }
    };

}
