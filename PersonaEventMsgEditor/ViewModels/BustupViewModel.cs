using ReactiveUI;
using static PersonaEventMsgEditor.Models.Event.Bustup;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.Reactive.Linq;
using PersonaEventMsgEditor.Models.Event;
using System.Linq;
using System;

namespace PersonaEventMsgEditor.ViewModels;
public class BustupViewModel : ViewModelBase
{
    private BustupCharacter _character;
    private int _outfit;
    private int _emotion;
    private BustupPosition _position;
    private readonly ObservableAsPropertyHelper<List<int>> _emotions;
    private readonly ObservableAsPropertyHelper<List<int>> _outfits;
    private readonly ObservableAsPropertyHelper<bool> _exists;
    private readonly ObservableAsPropertyHelper<Bitmap?> _image;

    public BustupCharacter Character { get => _character; set => this.RaiseAndSetIfChanged(ref _character, value); }

    public int Outfit { get => _outfit; set => this.RaiseAndSetIfChanged(ref _outfit, value); }

    public int Emotion { get => _emotion; set => this.RaiseAndSetIfChanged(ref _emotion, value); }

    public BustupPosition Position { get => _position; set => this.RaiseAndSetIfChanged(ref _position, value); }

    public Bitmap? Image => _image.Value;

    public bool Exists => _exists.Value;

    public List<int> Emotions => _emotions.Value;

    public List<int> Outfits => _outfits.Value;

    public BustupCharacter[] Characters => Bustup.Characters;


    public BustupViewModel() : this(BustupCharacter.None, 0, 0, BustupPosition.Right)
    {

    }

    // TODO try using observable collections for the Outfits lists and things instead of setting the entire list
    // hopefully that'll help with thee null reference exceptions
    public BustupViewModel(BustupCharacter character, int outfit, int emotion, BustupPosition postion)
    {
        _character = character;
        _outfit = outfit;
        _emotion = emotion;
        _position = postion;

        this.WhenAnyValue(x => x.Character)
            .Select(GetValidOutfits)
            .ToProperty(this, x => x.Outfits, out _outfits);

        this.WhenAnyValue(x => x.Outfits)
            .Subscribe(outfits =>
            {
                if (outfits.Count > 0 && !outfits.Contains(Outfit))
                    Outfit = outfits.First();
            });

        this.WhenAnyValue(x => x.Character, x => x.Outfit, GetValidEmotions)
            .ToProperty(this, x => x.Emotions, out _emotions);

        this.WhenAnyValue(x => x.Emotions)
            .Subscribe(emotions =>
            {
                if (emotions.Count > 0 && !emotions.Contains(Emotion))
                    Emotion = emotions.First();
            });

        this.WhenAnyValue(x => x.Character, x => x.Outfit, x => x.Emotion, LoadImage)
            .ToProperty(this, x => x.Image, out _image);

        this.WhenAnyValue(x => x.Character)
            .Select(x => x != BustupCharacter.None)
            .ToProperty(this, x => x.Exists, out _exists);

    }

}
