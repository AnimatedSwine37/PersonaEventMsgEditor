using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using PuyoTools.Archives.Formats.Afs;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace PersonaEventMsgEditor.ViewModels;
public class AudioPlayerViewModel : ViewModelBase
{
    private AfsReader? _afs;
    private int _major;
    private int _minor;

    private int? _adxIndex;
    public int? AdxIndex
    {
        get => _adxIndex;
        set => this.RaiseAndSetIfChanged(ref _adxIndex, value);
    }

    private readonly ObservableAsPropertyHelper<bool> _loaded;
    public bool Loaded => _loaded.Value;

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    private IPlaybackService _playbackService;

    public AudioPlayerViewModel(int major, int minor)
    {
        _major = major;
        _minor = minor;
        LoadAfs();

        var loadedObservable = this.WhenAnyValue(x => x.AdxIndex)
            .Select(x => x != null && _afs != null);
        loadedObservable.ToProperty(this, x => x.Loaded, out _loaded);

        PlayCommand = ReactiveCommand.Create(Play, loadedObservable);
        
        _playbackService = App.Current?.Services?.GetService<IPlaybackService>();
        if (_playbackService == null)
            throw new NotInitializedException("The playback service hasn't been initialised, report this!");

    }

    private void LoadAfs()
    {
        // TODO do async
        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService == null)
            throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

        var afsName = $"V{_major:D3}{_minor:D3}.AFS";
        if (cvmService.GetFiles(@"\SOUND", $"{afsName}*").Length == 0)
            return;

        var afsStream = cvmService.GetFile($@"\SOUND\{afsName}");
        _afs = new AfsReader(afsStream);
    }

    private void Play()
    {
        _playbackService.PlayStream(_afs!.Entries[(int)AdxIndex!].Open());
    }
}
