using LibVLCSharp.Shared;
using System;
using System.IO;

namespace PersonaEventMsgEditor.Services;
public class PlaybackService : IPlaybackService
{
    readonly LibVLC _libVLC;
    readonly MediaPlayer _mp;

    public PlaybackService()
    {
        Core.Initialize();

        _libVLC = new LibVLC();

        _mp = new MediaPlayer(_libVLC);
    }

    public void PlayStream(Stream stream)
    {
        // create a libvlc media
        // disable video output, we only need audio
        using (var media = new Media(_libVLC, new StreamMediaInput(stream), ":no-video"))
            _mp.Media = media;
        //using (var media = new Media(_libVLC, new Uri("https://archive.org/download/ImagineDragons_201410/imagine%20dragons.mp4"), ":no-video"))
        //    _mp.Media = media;

        _mp.Play();
    }

    public void Play()
    {
        _mp.Pause();
    }

    public void Pause()
    {
        _mp.Pause();
    }
}
