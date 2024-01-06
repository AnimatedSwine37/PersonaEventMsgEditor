using Avalonia.Platform.Storage;
using DiscUtils.Iso9660;
using DiscUtils.Streams;
using System.IO;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public class CvmService : ICvmService
{
    private Stream? _isoStream;
    private Stream? _cvmStream;
    private SubStream? _cvmIsoStream;
    private CDReader? _cvm;

    public bool IsLoaded()
    {
        return _cvm != null;
    }

    public async void LoadFromIso(IStorageFile file, string cvmPath)
    {
        if (_isoStream != null)
        {
            _cvm = null;
            _isoStream.Close();
        }

        _isoStream = await file.OpenReadAsync();
        CDReader cd = new CDReader(_isoStream, true);
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
