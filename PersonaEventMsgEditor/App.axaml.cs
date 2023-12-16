using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text.Encodings;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using PersonaEventMsgEditor.ViewModels;
using PersonaEventMsgEditor.Views;
using System;
using System.Text;

namespace PersonaEventMsgEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            var services = new ServiceCollection();

            services.AddSingleton<IFilesService>(x => new FilesService(desktop.MainWindow));
            services.AddSingleton<ICvmService>(x => new CvmService());

            Services = services.BuildServiceProvider();

        }

        // Setup script compiler stuff
        LibraryLookup.SetLibraryPath($"{AppDomain.CurrentDomain.BaseDirectory}\\Libraries");
        AtlusEncoding.SetCharsetDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\Charsets");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Needed for shift_jis encoding to be available

        base.OnFrameworkInitializationCompleted();
    }

    public new static App? Current => Application.Current as App;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider? Services { get; private set; }
}
