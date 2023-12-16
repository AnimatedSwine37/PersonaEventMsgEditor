using Avalonia.Media.Imaging;
using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;
public class PreviewViewModel : ViewModelBase
{
    private readonly EventMessage _message;
    private Bitmap? _bustup;

    public Bitmap? Bustup
    {
        get => _bustup;
        private set => this.RaiseAndSetIfChanged(ref _bustup, value);
    }

    public string Text => _message.Text;

    public PreviewViewModel(EventMessage message)
    {
        _message = message;
    }

    public async Task LoadBustup()
    {
        await using (var imageStream = await _message.LoadBustupAsync())
        {
            if (imageStream == null) return;
            Bustup = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }
    }
}
