using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public class FilesService : IFilesService
{
    private readonly Window _target;

    public FilesService(Window target)
    {
        _target = target;
    }

    public async Task<IStorageFile?> OpenFileAsync(string title, FilePickerFileType[] filter)
    {
        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            FileTypeFilter = filter,
            AllowMultiple = false,
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string title)
    {
        return await _target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title
        });
    }

    public async Task<IStorageBookmarkFile?> OpenFileBookmark(string bookmark)
    {
        return await _target.StorageProvider.OpenFileBookmarkAsync(bookmark);
    }
}
