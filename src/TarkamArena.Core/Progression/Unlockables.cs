using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Progression;

public enum UnlockKind : byte
{
    Costume,
    VictoryPose,
    Intro,
    Emote,
    Banner,
    Character,
}

/// <summary>A cosmetic or character the player can earn. Ids are stable save-data keys.</summary>
public sealed record Unlockable(string Id, UnlockKind Kind, LocalizedText Name, int Price, int RequiredLevel = 1, string? CharacterId = null, uint Tint = 0);

public static class Unlockables
{
    private static LocalizedText T(string en, string id) => new(en, id);

    private static readonly (string Key, LocalizedText Name, uint Tint, int Price, int Level)[] s_costumeThemes =
    [
        ("lebaran", T("Eid Best", "Baju Lebaran"), 0x2E8B57, 300, 2),
        ("merdeka", T("Independence Day", "Tujuh Belasan"), 0xE0322B, 400, 4),
        ("batik", T("Batik Night", "Malam Batik"), 0x8C5A2B, 600, 7),
        ("emas", T("Golden Legend", "Legenda Emas"), 0xD4AF37, 1200, 12),
    ];

    private static readonly LocalizedText[] s_poses =
    [
        T("Arms Crossed", "Tangan Bersedekap"), T("Flex", "Pamer Otot"), T("Selfie", "Swafoto"), T("Bow", "Membungkuk"),
        T("Joget", "Joget"), T("Victory Lap", "Keliling Arena"), T("Salute", "Hormat Bendera"), T("Sit Down Cool", "Duduk Santai"),
    ];

    private static readonly LocalizedText[] s_intros =
    [
        T("Walk In", "Jalan Santai"), T("Motorbike Arrival", "Datang Naik Motor"), T("Angkot Drop-off", "Turun dari Angkot"),
        T("Smoke and Thunder", "Asap dan Petir"), T("Out of the Crowd", "Muncul dari Penonton"), T("Falling from the Sky", "Jatuh dari Langit"),
    ];

    private static readonly LocalizedText[] s_emotes =
    [
        T("Wave", "Dadah"), T("Thumbs Up", "Jempol"), T("Laugh", "Ketawa"), T("Facepalm", "Tepok Jidat"), T("Taunt", "Ejek"),
        T("Mantap!", "Mantap!"), T("Sabar…", "Sabar…"), T("Gas Terus!", "Gas Terus!"),
    ];

    private static readonly (LocalizedText Name, uint Tint)[] s_banners =
    [
        (T("Terpal Blue", "Biru Terpal"), 0x1F4FB0), (T("Spanduk Yellow", "Kuning Spanduk"), 0xF6C21B), (T("Chilli Red", "Merah Cabai"), 0xE0322B),
        (T("Banana Leaf", "Daun Pisang"), 0x2E8B57), (T("Sogan Batik", "Batik Sogan"), 0x8C5A2B), (T("Night Market", "Pasar Malam"), 0x2B2B55),
        (T("Champion Gold", "Emas Juara"), 0xD4AF37), (T("Southern Sea", "Laut Selatan"), 0x0E7A52),
    ];

    private static readonly Unlockable[] s_all = Build();

    public static IReadOnlyList<Unlockable> All => s_all;

    public static Unlockable? Find(string id) => Array.Find(s_all, u => u.Id == id);

    public static IEnumerable<Unlockable> OfKind(UnlockKind kind) => s_all.Where(u => u.Kind == kind);

    public static IEnumerable<Unlockable> CostumesFor(string characterId) =>
        s_all.Where(u => u.Kind == UnlockKind.Costume && u.CharacterId == characterId);

    /// <summary>Everything a new profile starts with.</summary>
    public static IEnumerable<string> Defaults()
    {
        yield return "pose-0";
        yield return "intro-0";
        yield return "emote-0";
        yield return "emote-1";
        yield return "banner-0";
        foreach (var c in Roster.All.Where(c => !c.IsSecret && c.Dlc == DlcPack.None))
            yield return CharacterUnlockId(c.Id);
    }

    public static string CharacterUnlockId(string characterId) => $"char-{characterId}";

    public static string CostumeUnlockId(string characterId, string theme) => $"costume-{characterId}-{theme}";

    private static Unlockable[] Build()
    {
        var list = new List<Unlockable>();
        foreach (var c in Roster.All)
        {
            list.Add(new Unlockable(CharacterUnlockId(c.Id), UnlockKind.Character, c.Name, c.IsSecret ? 0 : 0, CharacterId: c.Id));
            foreach (var (key, name, tint, price, level) in s_costumeThemes)
                list.Add(new Unlockable(CostumeUnlockId(c.Id, key), UnlockKind.Costume, name, price, level, c.Id, tint));
        }
        for (int i = 0; i < s_poses.Length; i++) list.Add(new($"pose-{i}", UnlockKind.VictoryPose, s_poses[i], 150 * i, 1 + i));
        for (int i = 0; i < s_intros.Length; i++) list.Add(new($"intro-{i}", UnlockKind.Intro, s_intros[i], 200 * i, 1 + i * 2));
        for (int i = 0; i < s_emotes.Length; i++) list.Add(new($"emote-{i}", UnlockKind.Emote, s_emotes[i], 100 * i, 1 + i));
        for (int i = 0; i < s_banners.Length; i++) list.Add(new($"banner-{i}", UnlockKind.Banner, s_banners[i].Name, 120 * i, 1 + i, Tint: s_banners[i].Tint));
        return [.. list];
    }
}
