using AmicitiaLibrary.Graphics.TMX;
using AtlusFileSystemLibrary.FileSystems.PAK;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Models;

/// <summary>
/// Represents an individual message in an event
/// </summary>
public class EventMessage
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Speaker { get; set; }
    public string Voice { get; set; }

    private MessageScriptDecompiler _decompiler;
    private MessageDialog _dialog;

    public EventMessage(MessageDialog dialog)
    {
        _dialog = dialog;
        Name = dialog.Name;
        Speaker = "To Implement";

        using var msgWriter = new StringWriter();
        _decompiler = new MessageScriptDecompiler(msgWriter);
        // TODO change this so the text is just the actual text and other stuff like speaker and bustup are separate variables
        // (Need to decide how they'll be placed on the UI and whether Wxnder even wants that)
        _decompiler.Decompile(dialog);
        Text = msgWriter.ToString();
    }

    public async Task<Stream?> LoadBustupAsync()
    {
        // TODO probably don't make async like this...
        return await Task.Run(() =>
        {
            // TODO deal with multiple pages
            var page = _dialog.Pages[0];

            var funcs = page.Tokens.Where(x => x.Kind == TokenKind.Function)
                .Select(x => (FunctionToken)x);
            // Check if the bustup function is ever called
            if (!funcs.Any(x => x.FunctionTableIndex == 0 && x.FunctionIndex == 31))
                return null;

            var bustupFunc = funcs.First(x => x.FunctionTableIndex == 0 && x.FunctionIndex == 31);
            int character = bustupFunc.Arguments[0];
            int outfit = bustupFunc.Arguments[1];
            int emotion = bustupFunc.Arguments[2];
            int position = bustupFunc.Arguments[3];

            var bustupBinName = $@"I_B_{character:D2}{outfit:X}{emotion:X}.BIN";
            var bustupTmxName = $"i_bust_{character:D2}_{outfit:x}{emotion:x}.tmx";

            var cvmService = App.Current?.Services?.GetService<ICvmService>();
            if (cvmService == null)
                throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

            if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
            {
                // Try alternative name only used by some bustups :(
                bustupBinName = $@"I_B_{character:D2}{outfit:X}{emotion:X}A.BIN";
                if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
                {
                    return null;
                }
                bustupTmxName = $"i_bust_{character:D2}_{outfit:x}{emotion:x}a.tmx";
            }

            var bustupBin = cvmService.GetFile(@$"BUSTUP\{bustupBinName}");
            if (!PAKFileSystem.TryOpen(bustupBin, true, out var bustupPak))
            {
                return null;
            }

            var bustupTmxStream = bustupPak.OpenFile(bustupTmxName, FileAccess.Read);
            var bustupTmx = TmxFile.Load(bustupTmxStream);
            var tmxBitmap = bustupTmx.GetBitmap();

            var memory = new MemoryStream();
            tmxBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            return memory;
        });
    }
}
