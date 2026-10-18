using TarkamArena.Core.AI;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Services;

/// <summary>Who controls a fighter slot.</summary>
public enum SlotController : byte
{
    Player1,
    Player2,
    Cpu,
    Remote,
}

/// <summary>
/// A match being set up by the menus: mode, rules, who fights and where. The fight screen turns it
/// into a <see cref="MatchSetup"/>; <see cref="OnFinished"/> lets story, tournament and survival
/// continue their flow afterwards.
/// </summary>
public sealed class MatchPlan
{
    public GameMode Mode { get; set; } = GameMode.Versus;
    public MatchRules Rules { get; set; } = MatchRules.For(GameMode.Versus);
    public CpuDifficulty Difficulty { get; set; } = CpuDifficulty.Normal;
    public SlotController[] Controllers { get; set; } = [SlotController.Player1, SlotController.Cpu];
    public CharacterDefinition?[] Fighters { get; set; } = new CharacterDefinition?[2];
    public int[] StartingHealth { get; set; } = [100, 100, 100, 100];
    public StageDefinition? Stage { get; set; }
    public uint Seed { get; set; } = (uint)Environment.TickCount;

    /// <summary>When set, character select only lets the player pick slot 0 and the rest is fixed.</summary>
    public bool OpponentsFixed { get; set; }

    /// <summary>Stage choice is fixed (story, tournament).</summary>
    public bool StageFixed { get; set; }

    /// <summary>Online: the networking session to play through.</summary>
    public Networking.RollbackSession? Online { get; set; }
    public Networking.LiteNetTransport? OnlineTransport { get; set; }
    public string? OnlineMatchId { get; set; }

    /// <summary>Called with the result when the match ends. Return true to suppress the default results screen.</summary>
    public Func<MatchSummary, RewardSummary, bool>? OnFinished { get; set; }

    public int SlotCount => Controllers.Length;

    public static MatchPlan For(GameMode mode, bool versusHuman = false, int ffaPlayers = 4)
    {
        int slots = GameModes.SlotCount(mode, ffaPlayers);
        var controllers = new SlotController[slots];
        for (int i = 0; i < slots; i++) controllers[i] = SlotController.Cpu;
        controllers[0] = SlotController.Player1;
        if (versusHuman)
        {
            // Player two takes the first slot of the other team.
            int p2 = mode is GameMode.TwoVsTwo or GameMode.TagBattle ? 2 : 1;
            controllers[p2] = SlotController.Player2;
        }
        return new MatchPlan
        {
            Mode = mode,
            Rules = MatchRules.For(mode),
            Controllers = controllers,
            Fighters = new CharacterDefinition?[slots],
        };
    }

    public MatchSetup ToSetup()
    {
        var entries = new FighterEntry[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            entries[i] = new FighterEntry(Fighters[i] ?? Roster.Get("pesilat"), StartingHealth[i]);
        return new MatchSetup(Rules, Stage ?? Stages.All[0], entries, Seed);
    }

    /// <summary>Fills empty CPU slots with random fighters the player has unlocked.</summary>
    public void FillRandom(Random random)
    {
        var pool = Game.PlayableRoster().Where(Game.IsUnlocked).ToArray();
        for (int i = 0; i < SlotCount; i++)
            Fighters[i] ??= pool[random.Next(pool.Length)];
        Stage ??= Stages.All[random.Next(Stages.All.Count)];
    }
}
