using Avalonia.Media.Imaging;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Progression;
using TarkamArena.Core.Settings;
using TarkamArena.Engine.Assets;
using TarkamArena.Engine.Audio;
using TarkamArena.Engine.Input;
using TarkamArena.Persistence;

namespace TarkamArena.App.Services;

/// <summary>
/// Process-wide game services: save data, settings, audio and input. Created once at startup and
/// shared by every screen.
/// </summary>
public static class Game
{
    public static GameDatabase Database { get; private set; } = null!;
    public static PlayerProfile Profile { get; private set; } = null!;
    public static GameSettings Settings { get; private set; } = null!;
    public static AudioDirector Audio { get; private set; } = null!;
    public static InputMapper Input { get; private set; } = null!;

    public static void Initialize()
    {
        Database = new GameDatabase(GameDatabase.DefaultPath());
        Profile = Database.LoadProfile();
        Settings = Database.LoadSettings();
        if (LaunchOptions.Language is "en") Settings.Language = Language.English;
        if (LaunchOptions.Language is "id") Settings.Language = Language.Indonesian;
        Loc.SetLanguage(Settings.Language);
        Audio = new AudioDirector();
        Input = new InputMapper(Settings.KeyBindings);
        ApplyAudioSettings();
        ProgressionService.Refresh(Profile);
    }

    public static void ApplyAudioSettings() => Audio.SetVolumes(Settings.MasterVolume, Settings.MusicVolume, Settings.SfxVolume);

    public static void Save()
    {
        Profile.Language = Settings.Language;
        Database.SaveProfile(Profile);
        Database.SaveSettings(Settings);
    }

    public static void Shutdown()
    {
        Save();
        Audio.Dispose();
        Input.Dispose();
        Database.Dispose();
    }

    /// <summary>All fighters the player can pick, including their own creator fighters.</summary>
    public static IEnumerable<CharacterDefinition> PlayableRoster()
    {
        foreach (var c in Roster.All)
            if (c.Dlc == DlcPack.None || Profile.Owns(Unlockables.CharacterUnlockId(c.Id))) yield return c;
        foreach (var spec in Profile.CustomFighters)
        {
            CharacterDefinition? def = null;
            try { def = Core.Creator.CharacterCreator.ToDefinition(spec); } catch (ArgumentException) { }
            if (def is not null) yield return def;
        }
    }

    public static bool IsUnlocked(CharacterDefinition c) =>
        c.Category == CharacterCategory.Custom || Profile.HasCharacter(c.Id);
}

/// <summary>Decodes images once, at the size they are shown, and keeps them for reuse.</summary>
public static class Images
{
    private static readonly Dictionary<(string, int), Bitmap?> s_cache = [];

    public static Bitmap? Load(string relativePath, int decodeWidth = 0)
    {
        var key = (relativePath, decodeWidth);
        if (s_cache.TryGetValue(key, out var bmp)) return bmp;
        var path = AssetLocator.PathOf(relativePath);
        Bitmap? result = null;
        if (File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                result = decodeWidth > 0 ? Bitmap.DecodeToWidth(stream, decodeWidth) : new Bitmap(stream);
            }
            catch (Exception e) when (e is IOException or ArgumentException or InvalidOperationException)
            {
                result = null;
            }
        }
        s_cache[key] = result;
        return result;
    }

    /// <summary>Blender render of the authored model, else the engine-rendered portrait.</summary>
    public static Bitmap? Portrait(CharacterDefinition c, int width = 512) =>
        Load($"generated/previews/{c.Id}.png", width) ?? Load(c.PortraitPath, width);

    public static Bitmap? Backdrop(StageDefinition s, int width = 640) => Load($"backdrops/{s.Id}.jpg", width) ?? Load(s.ThumbnailPath, width);
}
