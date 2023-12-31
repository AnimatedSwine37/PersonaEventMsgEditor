using AmicitiaLibrary.Graphics.TMX;
using AtlusFileSystemLibrary.FileSystems.PAK;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using PersonaEventMsgEditor.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace PersonaEventMsgEditor.Models.Event;
public class Bustup
{
    private static Dictionary<BustupCharacter, List<int>> _cachedOutfits = new();
    private static Dictionary<(BustupCharacter character, int outfit), List<int>> _cachedEmotions = new();
    public static BustupCharacter[] Characters => Enum.GetValues<BustupCharacter>();
    private static ConcurrentDictionary<(BustupCharacter character, int outfit, int emotion), Bustup> _bustups = new();
    private static string _lock = "dummy :)";

    public BustupCharacter Character { get; }
    public int Outfit { get; }
    public int Emotion { get; }
    private Bitmap? _bitmap;

    private Bustup(BustupCharacter character, int outfit, int emotion)
    {
        Character = character;
        Outfit = outfit;
        Emotion = emotion;
    }

    public static Bustup GetBustup(BustupCharacter character, int outfit, int emotion)
    {
        return _bustups.GetOrAdd((character, outfit, emotion), new Bustup(character, outfit, emotion));
    }

    public static List<int> GetValidEmotions(BustupCharacter character, int outfit)
    {
        if (character == BustupCharacter.None)
            return new List<int>();

        if (_cachedEmotions.ContainsKey((character, outfit)))
        {
            var cachedEmotions = _cachedEmotions[(character, outfit)];
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

        var emotionsList = emotions.ToList();
        _cachedEmotions[(character, outfit)] = emotionsList;
        return emotionsList;
    }

    public static List<int> GetValidOutfits(BustupCharacter character)
    {
        if (_cachedOutfits.ContainsKey(character))
        {
            var cachedOutfits = _cachedOutfits[character];
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

        _cachedOutfits[character] = outfits.ToList();
        return _cachedOutfits[character];
    }

    public Bitmap? LoadImage()
    {
        if (Character == BustupCharacter.None) return null;

        lock (this)
        {
            if (_bitmap != null) return _bitmap;

            // Only allow one bustup to be loaded at a time as they're all loaded from the same base stream
            lock (_lock)
            {
                var bustupBinName = $@"I_B_{(int)Character:D2}{Outfit:X}{Emotion:X}.BIN";
                var bustupTmxName = $"i_bust_{(int)Character:D2}_{Outfit:x}{Emotion:x}.tmx";

                var cvmService = App.Current?.Services?.GetService<ICvmService>();
                if (cvmService == null)
                    throw new NotInitializedException("The CVM service hasn't been initialised, report this!");

                if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
                {
                    // Try alternative name only used by some bustups :(
                    bustupBinName = $@"I_B_{(int)Character:D2}{Outfit:X}{Emotion:X}A.BIN";
                    if (cvmService.GetFiles(@"\BUSTUP", $"*{bustupBinName}*").Length == 0)
                    {
                        return null;
                    }
                    bustupTmxName = $"i_bust_{(int)Character:D2}_{Outfit:x}{Emotion:x}a.tmx";
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

                _bitmap = Bitmap.DecodeToWidth(memory, 512);
                return _bitmap;
            }
        }
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
