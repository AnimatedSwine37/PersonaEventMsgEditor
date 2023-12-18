using AmicitiaLibrary.Graphics.TMX;
using AtlusFileSystemLibrary.FileSystems.PAK;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.Models.Event;
// TODO Split this into a model and view model
public class Bustup : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<Bitmap?> _image;
    public Bitmap? Image => _image.Value;

    private BustupCharacter _character;
    // TODO fix null reference exceptions with these setters (make them nullable maybe?)
    public BustupCharacter Character
    {
        get => _character;
        set => this.RaiseAndSetIfChanged(ref _character, value);
    }
    private static BustupCharacter[] _characters = Enum.GetValues<BustupCharacter>();
    public BustupCharacter[] Characters => _characters;


    // TODO map this to an enum as well (if emotions are uniform)
    private int _emotion;
    public int Emotion
    {
        get => _emotion;
        set => this.RaiseAndSetIfChanged(ref _emotion, value);
    }
    private readonly ObservableAsPropertyHelper<List<int>> _emotions;
    public List<int> Emotions => _emotions.Value;
    private Dictionary<(BustupCharacter character, int outfit), List<int>> _cachedEmotions = new();


    // TODO map this to an enum as well (if it makes sense to)
    private int _outfit;
    public int Outfit
    {
        get => _outfit;
        set => this.RaiseAndSetIfChanged(ref _outfit, value);
    }
    private readonly ObservableAsPropertyHelper<List<int>> _outfits;
    public List<int> Outfits => _outfits.Value;
    private Dictionary<BustupCharacter, List<int>> _characterOutfits = new();


    private BustupPosition _position;
    public BustupPosition Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
    }

    public Bustup() : this(BustupCharacter.None, 0, 0, BustupPosition.Right)
    {
    }

    public Bustup(BustupCharacter character, int outfit, int emotion, BustupPosition position)
    {
        Character = character;
        Outfit = outfit;
        Emotion = emotion;
        Position = position;

        this.WhenAnyValue(x => x.Character)
            .Select(GetValidOutfits)
            .ToProperty(this, x => x.Outfits, out _outfits);

        this.WhenAnyValue(x => x.Character, x => x.Outfit, GetValidEmotions)
            .ToProperty(this, x => x.Emotions, out _emotions);

        this.WhenAnyValue(x => x.Character, x => x.Outfit, x => x.Emotion,
            (character, outfit, emotion) => LoadBustup())
            .ToProperty(this, x => x.Image, out _image);
    }

    private List<int> GetValidEmotions(BustupCharacter character, int outfit)
    {
        if(character == BustupCharacter.None)
            return new List<int>();

        if (_cachedEmotions.ContainsKey((character, outfit)))
        {
            var cachedEmotions = _cachedEmotions[(character, outfit)];
            
            if (cachedEmotions.Count > 0 && !cachedEmotions.Contains(Emotion))
                Emotion = cachedEmotions.First();
            return cachedEmotions;
        }

        var emotions = new HashSet<int>();
        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService == null)
            throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

        var characterFiles = cvmService.GetFiles(@"\BUSTUP", $@"*I_B_{(int)character:D2}{outfit:X}*.BIN*");
        foreach (var file in characterFiles)
        {
            var match = Regex.Match(file, @"I_B_([0-9a-f]{2})([0-9a-f])([0-9a-f])", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var emotion = Convert.ToInt32(match.Groups[3].Value, 16);
            emotions.Add(emotion);
        }

        if (emotions.Count > 0 && !emotions.Contains(Emotion))
            Emotion = emotions.First();

        var emotionsList = emotions.ToList();
        _cachedEmotions[(character, outfit)] = emotionsList;
        return emotionsList;
    }

    private List<int> GetValidOutfits(BustupCharacter character)
    {
        if (_characterOutfits.ContainsKey(character))
        {
            var cachedOutfits = _characterOutfits[character];
            if (cachedOutfits.Count > 0 && !cachedOutfits.Contains(Outfit))
                Outfit = cachedOutfits.First();
            return cachedOutfits;
        }

        var outfits = new HashSet<int>();
        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService == null)
            throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

        var characterFiles = cvmService.GetFiles(@"\BUSTUP", $@"*I_B_{(int)character:D2}*.BIN*");
        foreach (var file in characterFiles)
        {
            var match = Regex.Match(file, @"I_B_([0-9a-f]{2})([0-9a-f])([0-9a-f])", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var outfit = Convert.ToInt32(match.Groups[2].Value, 16);
            outfits.Add(outfit);
        }

        _characterOutfits[character] = outfits.ToList();
        if (outfits.Count > 0 && !outfits.Contains(Outfit))
            Outfit = outfits.First();

        return _characterOutfits[character];
    }

    private Bitmap? LoadBustup()
    {
        if (_character == BustupCharacter.None) return null;

        // TODO make this async somehow
        var bustupBinName = $@"I_B_{(int)_character:D2}{_outfit:X}{_emotion:X}.BIN";
        var bustupTmxName = $"i_bust_{(int)_character:D2}_{_outfit:x}{_emotion:x}.tmx";

        var cvmService = App.Current?.Services?.GetService<ICvmService>();
        if (cvmService == null)
            throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

        if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
        {
            // Try alternative name only used by some bustups :(
            bustupBinName = $@"I_B_{(int)_character:D2}{_outfit:X}{_emotion:X}A.BIN";
            if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
            {
                return null;
            }
            bustupTmxName = $"i_bust_{(int)_character:D2}_{_outfit:x}{_emotion:x}a.tmx";
        }

        var bustupBin = cvmService.GetFile(@$"BUSTUP\{bustupBinName}");
        if (!PAKFileSystem.TryOpen(bustupBin, true, out var bustupPak))
        {
            return null;
        }

        var bustupTmxStream = bustupPak.OpenFile(bustupTmxName, FileAccess.Read);
        var bustupTmx = TmxFile.Load(bustupTmxStream);
        var tmxBitmap = bustupTmx.GetBitmap();

        var memory = new MemoryStream();
        tmxBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        return Bitmap.DecodeToWidth(memory, 512);
    }

    public enum BustupCharacter
    {
        None = 0,
        Protagonist = 1,
        Yukari = 2,
        Aigis = 3,
        Mitsuru = 4,
        Junpei = 5,
        Fuuka = 6,
        Akihiko = 7,
        Ken = 8,
        Shinjiro = 9,
        Koromaru = 10,
        Ikutsuki = 11,
        Pharos = 12,
        Ryoji = 13,
        Takeharu_Kirijo = 14,
        Igor = 15,
        Elizabeth = 16,
        Takaya = 17,
        Jin = 18,
        Chidori = 19,
        Ms_Toriumi = 44,
        // TODO name the rest...
    }

    // TODO verify these (assuming it's the same as p4g)
    public enum BustupPosition
    {
        Right = 1,
        Middle = 2,
        Left = 3,
    }
}
