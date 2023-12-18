using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Models;
using PersonaEventMsgEditor.Services;
using ReactiveUI;
using System.IO;
using System;
using System.Reactive;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using PersonaEventMsgEditor.Models.Files;
using System.Reactive.Concurrency;

namespace PersonaEventMsgEditor.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> OpenFromDiskCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFromGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveEventCommand { get; }

    private EventViewModel? _event;
    public EventViewModel? Event
    {
        get => _event;
        set => this.RaiseAndSetIfChanged(ref _event, value);
    }

    public MainViewModel()
    {
        OpenFromDiskCommand = ReactiveCommand.Create(OpenFromDisk);
        OpenFromGameCommand = ReactiveCommand.Create(OpenFromGame);

        var canSaveObservable = this.WhenAnyValue(x => x.Event, x => x.Event, (x, y) => x != null);
        SaveEventCommand = ReactiveCommand.Create(() => _event!.Save(), canSaveObservable);
    }

    private async void OpenFromDisk()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        var file = await filesService.OpenFileAsync("Select an event", new[] { EventViewModel.EventFile });
        if (file is null) return;

        Event = await EventViewModel.FromFileAsync(file);
    }

    private void OpenFromGame()
    {
        // TODO actuall open the file and stuff
    }

}
