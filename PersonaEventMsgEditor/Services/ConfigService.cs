using PersonaEventMsgEditor.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public class ConfigService : IConfigService
{
    private static string ConfigPath = "Config.json";
    public Config Config { get; private set; }

    public ConfigService()
    {
        // TODO maybe don't do this, load it somewhere else?
        Task.WaitAll(Load());
    }

    public async Task Load()
    {
        string? json;
        if (!File.Exists(ConfigPath))
        {
            Config = new Config();
            json = JsonSerializer.Serialize(Config);
            File.WriteAllText(ConfigPath, json);
            return;
        }

        json = File.ReadAllText(ConfigPath);
        Config = JsonSerializer.Deserialize<Config>(json);
    }

    public async Task Save()
    {
        if (Config == null)
            await Load();

        var json = JsonSerializer.Serialize(Config);
        await File.WriteAllTextAsync(ConfigPath, json);
    }
}
