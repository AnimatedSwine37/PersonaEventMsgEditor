using DiscUtils.Iso9660;
using DiscUtils.Streams;
using System.IO;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public class CvmService : ICvmService
{
    private Stream? _cvmStream;
    private SubStream? _cvmIsoStream;
    private CDReader? _cvm;

    public CvmService()
    {
        // TODO get the path from some kind of config and do stuff if it's not there
        // (maybe a pop up or something telling them to set it)
        // Also just don't do this in the constructor...
        LoadFromIso(@"E:\Modding\P3F\CEP\Files\iso\P3F.iso", "DATA.CVM");
    }

    public void LoadFromIso(string isoPath, string cvmPath)
    {
        FileStream isoStream = File.Open(isoPath, FileMode.Open);
        CDReader cd = new CDReader(isoStream, true);
        _cvmStream = cd.OpenFile(cvmPath, FileMode.Open);
        // Move past the CVM header to the ISO file (TODO instead of just going by 0x1800 actually read the header and work out length)
        _cvmIsoStream = new SubStream(_cvmStream, 0x1800, _cvmStream.Length - 0x1800);

        _cvm = new CDReader(_cvmIsoStream, true);
    }

    public string[] GetFiles(string path, string pattern)
    {
        if (_cvm == null)
            throw new NotInitializedException("The CVM has not been loaded yet!");
        return _cvm.GetFiles(path, pattern, SearchOption.AllDirectories);
    }

    public Stream GetFile(string path)
    {
        if (_cvm == null)
            throw new NotInitializedException("The CVM has not been loaded yet!");
        return _cvm.OpenFile(path, FileMode.Open);
    }
}
