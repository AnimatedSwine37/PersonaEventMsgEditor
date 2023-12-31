using PersonaEventMsgEditor.Models;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public interface IConfigService
{
    /// <summary>
    /// The configuration
    /// </summary>
    public Config Config { get; }

    /// <summary>
    /// Load the configuration
    /// </summary>
    public Task Load();

    /// <summary>
    /// Save the configuration
    /// </summary>
    public Task Save();
}
