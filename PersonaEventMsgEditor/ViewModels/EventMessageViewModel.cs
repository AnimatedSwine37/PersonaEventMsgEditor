using AmicitiaLibrary.Graphics.TMX;
using AtlusFileSystemLibrary.FileSystems.PAK;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;
public class EventMessageViewModel : ViewModelBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _text;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private string _speaker;
    public string Speaker
    {
        get => _speaker;
        set => this.RaiseAndSetIfChanged(ref _speaker, value);
    }

    private Bitmap? _bustup;
    public Bitmap? Bustup
    {
        get => _bustup;
        private set => this.RaiseAndSetIfChanged(ref _bustup, value);
    }

    // TODO actually have the sound file somehow instead of whatever this string is meant to be
    private string _voice;
    public string Voice
    {
        get => _voice;
        set => this.RaiseAndSetIfChanged(ref _voice, value);
    }

    private MessageDialog _dialog;

    public EventMessageViewModel(MessageDialog dialog)
    {
        _dialog = dialog;
        Name = dialog.Name;
        Speaker = "To Implement";

        using var msgWriter = new StringWriter();
        var decompiler = new MessageScriptDecompiler(msgWriter);
        // TODO change this so the text is just the actual text and other stuff like speaker and bustup are separate variables
        // (Need to decide how they'll be placed on the UI and whether Wxnder even wants that)
        decompiler.Decompile(dialog);
        Text = msgWriter.ToString();
    }

    public async Task LoadBustupAsync()
    {
        // TODO probably don't make async like this...
        var imageStream = await Task.Run(() =>
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

        if (imageStream != null)
        {
            Bustup = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }
    }
}
