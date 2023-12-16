using Avalonia.Platform.Storage;
using DiscUtils.Streams;
using System.IO;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Models.Files;

/// <summary>
/// A class that contains utilities for extracting messages from PMD (particularly PM1)
/// files and injecting new messages into them.
/// </summary>
public class PMD
{
    private SubStream _msgStream;
    private Stream _file;
    public Stream Message { get => _msgStream; set => InjectMessage(value); }

    private int _typeTableCount;
    private int _msgAddress;
    private int _msgIndex;

    private PMD(Stream file, int typeTableCount, SubStream msgStream, int msgAddress, int msgIndex)
    {
        _file = file;
        _typeTableCount = typeTableCount;
        _msgStream = msgStream;
        _msgAddress = msgAddress;
        _msgIndex = msgIndex;
    }

    /// <summary>
    /// Creates a new PMD from a stream
    /// </summary>
    /// <param name="stream">A stream containing a PMD file</param>
    /// <returns>A PMD representing the given file</returns>
    public static PMD? FromStream(Stream stream)
    {
        var reader = new BinaryReader(stream);
        stream.Position = 0x10;
        int typeTableCount = reader.ReadInt32();
        stream.Position = 0x14;

        for (int i = 0; i < typeTableCount; i++)
        {
            stream.Position += 0xC;
            if (reader.ReadInt32() != 6) continue;
            // We found the message type table
            var itemSize = reader.ReadInt32();
            var itemCount = reader.ReadInt32(); // Really assuming this is 1, stuff will probably break if more...
            if (itemCount > 1)
            {
                throw new InvalidDataException("Attempted to load PMD with multiple messages.\nThese are currently unsupported, please report this.");
            }
            var itemAddress = reader.ReadInt32();
            return new PMD(stream, typeTableCount, new SubStream(stream, itemAddress, itemSize * itemCount), itemAddress, i);
        }

        // Didn't find the message, the pmd is useless in that case so return null
        return null;
    }

    /// <summary>
    /// Injects a new bmd file into the PMD stream
    /// </summary>
    /// <remarks>
    /// This writes to the stream this PMD was created from so if it was a file stream, the file on disk will be altered
    /// </remarks>
    /// <param name="message">A stream containing the new bmd file to inject</param>
    public async Task InjectMessage(Stream message)
    {
        // Get change in file length
        var reader = new BinaryReader(_file);
        _file.Position = 4;
        var fileSize = reader.ReadInt32();
        int msgLengthDiff = (int)(message.Length - _msgStream.Length);

        // Change file length
        _file.SetLength(fileSize + msgLengthDiff);
        _file.Position = 4;
        var writer = new BinaryWriter(_file);
        writer.Write(fileSize + msgLengthDiff);

        // Change the message length (note this assumes only one message item)
        _file.Position = 0x24 + _msgIndex * 0x10;
        writer.Write((int)message.Length);

        // Change the address of all entries to their new position
        _file.Position = 0x20;
        for (int i = 0; i < _typeTableCount; i++)
        {
            _file.Position += 0xC;
            var address = reader.ReadInt32();
            if (address <= _msgAddress) continue; // We only need to touch stuff that comes after the message

            _file.Position -= 4;
            writer.Write(address + msgLengthDiff);
        }

        // Copy all of the data after the message
        var msgEnd = _msgAddress + _msgStream.Length;
        MemoryStream? movedData = null;
        if (fileSize - msgEnd > 0)
        {
            movedData = new MemoryStream(new byte[fileSize - msgEnd]);
            _file.Position = msgEnd;
            await _file.CopyToAsync(movedData, (int)movedData.Length);
        }

        // Write out the new message data
        _file.Position = _msgAddress;
        message.Position = 0;
        await message.CopyToAsync(_file, (int)_msgStream.Length);

        // Write the old data after it
        if(movedData != null)
        {
            await movedData.CopyToAsync(_file);
        }
    }

    /// <summary>
    /// Writes the full PMD to a stream
    /// </summary>
    public async Task ToFile(IStorageFile file)
    {
        using var stream = await file.OpenWriteAsync();
        _file.Position = 0;
        await _file.CopyToAsync(stream);
    }
}

public class PMDHeader
{

}
