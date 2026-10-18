using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Creator;

/// <summary>The three kinds of fighter the creator can make.</summary>
public enum CreatorBase : byte
{
    Human,
    Mystic,
    Jawara,
}

/// <summary>A player-made fighter. Stored in the profile and turned into a <see cref="CharacterDefinition"/> for matches.</summary>
public sealed record CustomFighterSpec
{
    public const int StatBudget = 24;
    public const int MinStat = 1;
    public const int MaxStat = 9;

    public string Id { get; init; } = $"custom-{Guid.NewGuid():N}"[..15];
    public string Name { get; init; } = "";
    public CreatorBase Base { get; init; } = CreatorBase.Human;
    public Archetype Archetype { get; init; } = Archetype.Brawler;
    public byte Power { get; init; } = 6;
    public byte Speed { get; init; } = 6;
    public byte Defense { get; init; } = 6;
    public byte Technique { get; init; } = 6;
    public float Height { get; init; } = 1.72f;
    public float Width { get; init; } = 1.0f;
    public uint PrimaryColor { get; init; } = 0x1F4FB0;
    public uint SecondaryColor { get; init; } = 0xF6C21B;
    public uint SkinColor { get; init; } = 0xB98055;
    public BodyFeature Features { get; init; } = BodyFeature.None;

    public int SpentPoints => Power + Speed + Defense + Technique;
}

public static class CharacterCreator
{
    /// <summary>Features each base is allowed to use.</summary>
    public static BodyFeature AllowedFeatures(CreatorBase b) => b switch
    {
        CreatorBase.Human => BodyFeature.Hat | BodyFeature.Helmet | BodyFeature.LongHair | BodyFeature.Glasses | BodyFeature.Headscarf | BodyFeature.Uniform | BodyFeature.Apron | BodyFeature.Mask | BodyFeature.Cape,
        CreatorBase.Mystic => BodyFeature.LongHair | BodyFeature.Shroud | BodyFeature.Horns | BodyFeature.Floating | BodyFeature.Glow | BodyFeature.Fur | BodyFeature.LongArms | BodyFeature.Wings | BodyFeature.Tail | BodyFeature.Mask,
        _ => BodyFeature.Hat | BodyFeature.Headscarf | BodyFeature.Weapon | BodyFeature.Cape | BodyFeature.Mask | BodyFeature.LongHair,
    };

    public static IReadOnlyList<string> Validate(CustomFighterSpec spec)
    {
        var errors = new List<string>();
        string name = spec.Name.Trim();
        if (name.Length is < 2 or > 20) errors.Add("creator.error.name");
        if (spec.SpentPoints != CustomFighterSpec.StatBudget) errors.Add("creator.error.points");
        foreach (byte v in new[] { spec.Power, spec.Speed, spec.Defense, spec.Technique })
            if (v is < CustomFighterSpec.MinStat or > CustomFighterSpec.MaxStat) { errors.Add("creator.error.points"); break; }
        if ((spec.Features & ~AllowedFeatures(spec.Base)) != 0) errors.Add("creator.error.features");
        if (spec.Archetype == Archetype.Legend) errors.Add("creator.error.style");
        return errors;
    }

    public static CharacterDefinition ToDefinition(CustomFighterSpec spec)
    {
        if (Validate(spec) is { Count: > 0 } errors)
            throw new ArgumentException($"Invalid custom fighter: {string.Join(", ", errors)}", nameof(spec));

        var title = spec.Base switch
        {
            CreatorBase.Human => new LocalizedText("Street Fighter", "Petarung Jalanan"),
            CreatorBase.Mystic => new LocalizedText("Spirit of the Night", "Makhluk Malam"),
            _ => new LocalizedText("Regional Champion", "Jawara Daerah"),
        };

        var specials = spec.Archetype switch
        {
            Archetype.Grappler => new SpecialMoveNames(new("Iron Grip", "Cengkeraman Besi"), new("Shoulder Charge", "Seruduk Bahu"), new("Earth Slam", "Bantingan Bumi")),
            Archetype.Mystic => new SpecialMoveNames(new("Spirit Orb", "Bola Arwah"), new("Vanish", "Menghilang"), new("Curse Wave", "Gelombang Kutukan")),
            Archetype.Beast => new SpecialMoveNames(new("Claw Flurry", "Cakar Beruntun"), new("Pounce", "Terkam"), new("Rising Fang", "Taring Naik")),
            Archetype.Striker => new SpecialMoveNames(new("Pebble Shot", "Lemparan Kerikil"), new("Flying Knee", "Lutut Terbang"), new("Rising Kick", "Tendangan Naik")),
            _ => new SpecialMoveNames(new("Sandal Throw", "Lempar Sandal"), new("Running Fist", "Tinju Lari"), new("Uppercut", "Pukulan Bawah")),
        };

        string name = spec.Name.Trim();
        return new CharacterDefinition
        {
            Id = spec.Id,
            Name = new(name, name),
            Title = title,
            Category = CharacterCategory.Custom,
            Archetype = spec.Archetype,
            Origin = "Tarkam Arena",
            Bio = new($"{name} was made in the fighter creator.", $"{name} dirakit di kreator petarung."),
            Stats = new(spec.Power, spec.Speed, spec.Defense, spec.Technique),
            Body = new(Math.Clamp(spec.Height, 1.4f, 2.2f), Math.Clamp(spec.Width, 0.8f, 1.4f), 1f, spec.PrimaryColor, spec.SecondaryColor, spec.SkinColor, spec.Features),
            Specials = specials,
            FinisherNames =
            [
                new("Final Lesson", "Pelajaran Terakhir"), new("Lights Out", "Lampu Padam"), new("Kampung Justice", "Keadilan Kampung"),
                new("Endless Beatdown", "Hajar Terus"), new("Payback", "Balas Dendam"),
                new("High Five", "Tos Dulu"), new("Joget Together", "Joget Bareng"),
                new("Go Home", "Pulang Sana"),
                new("A Legend Is Forged", "Legenda Ditempa"),
            ],
        };
    }
}
