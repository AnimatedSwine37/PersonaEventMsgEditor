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

namespace PersonaEventMsgEditor.ViewModels;

public class MainViewModel : ViewModelBase
{
    public Cvm DataCvm { get; private set; }
    public ReactiveCommand<Unit, Unit> OpenFromDiskCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFromGameCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveEventCommand { get; }

    private EditorViewModel? _editor;
    public EditorViewModel? Editor
    {
        get => _editor;
        set => this.RaiseAndSetIfChanged(ref _editor, value);
    }

    public MainViewModel()
    {
        // TODO store the path to the iso in a config or something
        DataCvm = Cvm.FromIso(@"E:\Modding\P3F\CEP\Files\iso\P3F.iso", "DATA.CVM");
        OpenFromDiskCommand = ReactiveCommand.Create(OpenFromDisk);
        OpenFromGameCommand = ReactiveCommand.Create(OpenFromGame);

        var canSaveObservable = this.WhenAnyValue(x => x.Editor, x => x.Editor, (x, y) => x != null);
        SaveEventCommand = ReactiveCommand.Create(() => _editor.SaveEvent(), canSaveObservable);
    }

    private async void OpenFromDisk()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        var file = await filesService.OpenFileAsync("Select an event", new[] { Event.EventFile });
        if (file is null) return;

        var @event = await Event.FromFileAsync(file);
        Editor = new EditorViewModel(@event);
    }

    private void OpenFromGame()
    {
        // TODO actuall open the file and stuff
    }

}
