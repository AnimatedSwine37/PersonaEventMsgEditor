using System.IO;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;

/// <summary>
/// A service for loading data from a CVM.
/// This needs to be initialised using <see cref="LoadFromIso(string, string)"/> before use
/// </summary>
public interface ICvmService
{
    /// <summary>
    /// Loads a CVM into the service from an iso
    /// </summary>
    /// <param name="isoPath">A path to the iso file</param>
    /// <param name="cvmPath">The path of the cvm inside of the iso</param>
    /// <remarks>
    /// When getting files from the <see cref="ICvmService"/> they will be loaded from the CVM loaded by this
    /// </remarks>
    public Task LoadFromIso(string isoPath, string cvmPath);

    /// <summary>
    /// Recursively gets all files from the cvm that match a pattern
    /// </summary>
    /// <param name="path">The path to search</param>
    /// <param name="pattern">A search pattern</param>
    /// <returns>A list of all files found</returns>
    /// <exception cref="NotInitializedException">If the CVM has not yet been initialised by running <see cref="LoadFromIso(string, string)"/></exception>
    public string[] GetFiles(string path, string pattern);

    /// <summary>
    /// Gets a file from the cvm
    /// </summary>
    /// <param name="path">Path to the file in the cvm</param>
    /// <returns>A stream with the file</returns>
    /// <exception cref="NotInitializedException">If the CVM has not yet been initialised by running <see cref="LoadFromIso(string, string)"/></exception>
    public Stream GetFile(string path);
}

[System.Serializable]
public class NotInitializedException : System.Exception
{
    public NotInitializedException() { }
    public NotInitializedException(string message) : base(message) { }
    public NotInitializedException(string message, System.Exception inner) : base(message, inner) { }
    protected NotInitializedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}