using System.IO;

namespace PersonaEventMsgEditor.Services;
public interface IPlaybackService
{
    /// <summary>
    /// Plays an audio file from the given stream
    /// </summary>
    /// <param name="stream">A stream of the audio file ot play</param>
    public void PlayStream(Stream stream);
}
