using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> OpenFromDiskCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFromGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveEventCommand { get; }
    public ReactiveCommand<Unit, Unit> SetIsoPathCommand { get; }

    private EventViewModel? _event;
    public EventViewModel? Event
    {
        get => _event;
        set => this.RaiseAndSetIfChanged(ref _event, value);
    }

    public MainViewModel()
    {
        OpenFromDiskCommand = ReactiveCommand.CreateFromTask(OpenFromDisk);
        OpenFromGameCommand = ReactiveCommand.Create(OpenFromGame);
        SetIsoPathCommand = ReactiveCommand.CreateFromTask(SelectIso);

        var canSaveObservable = this.WhenAnyValue(x => x.Event, x => x.Event, (x, y) => x != null);
        SaveEventCommand = ReactiveCommand.Create(() => _event!.Save(), canSaveObservable);
    }

    private async Task OpenFromDisk()
    {
        var configService = App.Current?.Services?.GetService<IConfigService>();
        if(configService == null) throw new NullReferenceException("Missing Config Service instance");
        if (configService?.Config.P3FIso == null)
        {
            // TODO add a message thing telling them they need to choose an iso
            await SelectIso();
            if (configService?.Config.P3FIso == null) return;
        }

        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        var file = await filesService.OpenFileAsync("Select an event", new[] { EventViewModel.EventFile });
        if (file is null) return;

        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if(cvmService == null) throw new NullReferenceException($"Missing CVM Service instance.");

        if (!cvmService.IsLoaded())
        {
            var iso = await filesService.OpenFileBookmark(configService.Config.P3FIso);
            if (iso is null) throw new NullReferenceException("Unable to load bookmark for iso file");
            cvmService.LoadFromIso(iso, "DATA.CVM");
        }

        Event = await EventViewModel.FromFileAsync(file);
    }

    private void OpenFromGame()
    {
        // TODO actually open the file and stuff
    }

    private async Task SelectIso()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        var file = await filesService.OpenFileAsync("Select P3F's ISO", new[] { IsoFile });
        if (file is null) return;

        var configService = App.Current?.Services?.GetService<IConfigService>();
        if (configService is null) throw new NullReferenceException("Missing Config Service insatnce.");
        var bookmark = await file.SaveBookmarkAsync();
        if(bookmark is null) throw new NullReferenceException("Unable to make bookmark for file");
        configService.Config.P3FIso = bookmark;
        await configService.Save();

        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService is null) throw new NullReferenceException("Missing CVM Service instance.");
        cvmService.LoadFromIso(file, "DATA.CVM");
    }

    private static FilePickerFileType IsoFile { get; } = new("ISO File")
    {
        Patterns = new[] { "*.iso" }
    };
}
