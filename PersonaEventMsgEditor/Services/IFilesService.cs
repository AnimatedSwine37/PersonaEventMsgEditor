using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Services;
public interface IFilesService
{
    public Task<IStorageFile?> OpenFileAsync(string title, FilePickerFileType[] filter);
    public Task<IStorageFile?> SaveFileAsync(string title);
    public Task<IStorageBookmarkFile?> OpenFileBookmark(string bookmark);
}
