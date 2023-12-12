using DiscUtils.Iso9660;
using DiscUtils.Streams;
using System.IO;

namespace PersonaEventMsgEditor.Models.Files;
public class Cvm
{
    private Stream _cvmStream;
    private SubStream _cvmIsoStream;
    private CDReader _cvm;

    private Cvm(Stream cvmStream, SubStream cvmIsoStream, CDReader cvm)
    {
        _cvmStream = cvmStream;
        _cvmIsoStream = cvmIsoStream;
        _cvm = cvm;
    }

    public static Cvm FromIso(string isoPath, string cvmPath)
    {
        FileStream isoStream = File.Open(isoPath, FileMode.Open);
        CDReader cd = new CDReader(isoStream, true);
        var cvmStream = cd.OpenFile(cvmPath, FileMode.Open);
        // Move past the CVM header to the ISO file (TODO instead of just going by 0x1800 actually read the header and work out length)
        var cvmIsoStream = new SubStream(cvmStream, 0x1800, cvmStream.Length - 0x1800);

        var cvm = new CDReader(cvmIsoStream, true);
        return new Cvm(cvmStream, cvmIsoStream, cvm);
    }

    /// <summary>
    /// Recursively gets all files from the cvm
    /// </summary>
    /// <param name="path">The path to search</param>
    /// <param name="pattern">A search pattern</param>
    /// <returns>A list of all files found</returns>
    public string[] GetFiles(string path, string pattern)
    {
        return _cvm.GetFiles(path, pattern, SearchOption.AllDirectories);
    }

    /// <summary>
    /// Gets a file from the cvm
    /// </summary>
    /// <param name="path">Path to the file in the cvm</param>
    /// <returns>A stream with the file</returns>
    public Stream GetFile(string path)
    {
        return _cvm.OpenFile(path, FileMode.Open);
    }
}
