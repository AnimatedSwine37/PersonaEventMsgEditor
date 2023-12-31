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
        if (configService?.Config.P3FIsoPath == null)
        {
            // TODO add a message thing telling them they need to choose an iso
            await SelectIso();
            if (configService?.Config.P3FIsoPath == null) return;
        }

        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        var file = await filesService.OpenFileAsync("Select an event", new[] { EventViewModel.EventFile });
        if (file is null) return;

        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if(cvmService == null) throw new NullReferenceException($"Missing CVM Service instance.");

        if (!cvmService.IsLoaded())
            cvmService.LoadFromIso(configService.Config.P3FIsoPath, "DATA.CVM");

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
        configService.Config.P3FIsoPath = file.Path.AbsolutePath;
        await configService.Save();

        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService is null) throw new NullReferenceException("Missing CVM Service instance.");
        cvmService.LoadFromIso(file.Path.AbsolutePath, "DATA.CVM");
    }

    private static FilePickerFileType IsoFile { get; } = new("ISO File")
    {
        Patterns = new[] { "*.iso" }
    };
}
