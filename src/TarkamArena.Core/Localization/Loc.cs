using System.Collections.Frozen;

namespace TarkamArena.Core.Localization;

/// <summary>
/// UI string table in English and Bahasa Indonesia. Keys are stable identifiers used by the app;
/// <c>LocalizationTests</c> verifies that every key has both translations.
/// </summary>
public static class Loc
{
    public static Language Current { get; set; } = Language.Indonesian;

    public static event Action? LanguageChanged;

    public static void SetLanguage(Language language)
    {
        if (Current == language) return;
        Current = language;
        LanguageChanged?.Invoke();
    }

    public static string T(string key) =>
        s_table.TryGetValue(key, out var text) ? text.Get(Current) : key;

    public static string T(string key, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, T(key), args);

    public static IReadOnlyDictionary<string, LocalizedText> Table => s_table;

    private static readonly FrozenDictionary<string, LocalizedText> s_table = new Dictionary<string, LocalizedText>
    {
        // Brand
        ["app.title"] = new("Tarkam Arena", "Tarkam Arena"),
        ["app.tagline"] = new("Legendary Fighting Tournament from Indonesia", "Turnamen Pertarungan Legendaris dari Indonesia"),
        ["app.slogan"] = new(
            "From the wet market to the spirit world, every fighter has a story. Enter the arena, seize the glory, and become a legend of Indonesia.",
            "Dari pasar becek sampai dunia gaib, semua petarung punya cerita. Masuk arena, rebut kejayaan, dan jadilah legenda Indonesia."),
        ["app.credit"] = new("Made by Ariana Mischa Fadhila of Syubadubin Studios", "Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios"),
        ["app.closing"] = new("Legends are not born. Legends are forged in the arena.", "Legenda tidak dilahirkan. Legenda ditempa di arena."),
        ["app.pressStart"] = new("Press Enter to enter the arena", "Tekan Enter untuk masuk arena"),

        // Main menu
        ["menu.fight"] = new("Fight", "Bertarung"),
        ["menu.fight.hint"] = new("1 vs 1, 2 vs 2, tag battle and free for all", "1 lawan 1, 2 lawan 2, tag battle, dan bebas semua"),
        ["menu.story"] = new("Story", "Cerita"),
        ["menu.story.hint"] = new("The road to the Tarkam Arena finals", "Jalan menuju final Tarkam Arena"),
        ["menu.tournament"] = new("Tournament", "Turnamen"),
        ["menu.tournament.hint"] = new("Eight fighters, one trophy", "Delapan petarung, satu piala"),
        ["menu.survival"] = new("Survival", "Survival"),
        ["menu.survival.hint"] = new("How many waves can you last?", "Berapa gelombang kamu bertahan?"),
        ["menu.online"] = new("Online", "Online"),
        ["menu.online.hint"] = new("Ranked matches and clan wars", "Pertandingan ranked dan perang klan"),
        ["menu.creator"] = new("Creator", "Kreator"),
        ["menu.creator.hint"] = new("Build your own fighter", "Rakit petarungmu sendiri"),
        ["menu.collection"] = new("Collection", "Koleksi"),
        ["menu.collection.hint"] = new("Costumes, poses, banners and achievements", "Kostum, pose, banner, dan pencapaian"),
        ["menu.training"] = new("Training", "Latihan"),
        ["menu.training.hint"] = new("Practise combos against a dummy", "Latih kombo melawan boneka"),
        ["menu.settings"] = new("Settings", "Pengaturan"),
        ["menu.credits"] = new("Credits", "Kredit"),
        ["menu.quit"] = new("Quit", "Keluar"),

        // Common
        ["common.back"] = new("Back", "Kembali"),
        ["common.confirm"] = new("Confirm", "Pilih"),
        ["common.start"] = new("Start match", "Mulai tanding"),
        ["common.random"] = new("Random", "Acak"),
        ["common.locked"] = new("Locked", "Terkunci"),
        ["common.save"] = new("Save", "Simpan"),
        ["common.saved"] = new("Saved", "Tersimpan"),
        ["common.continue"] = new("Continue", "Lanjut"),
        ["common.rematch"] = new("Rematch", "Tanding ulang"),
        ["common.mainMenu"] = new("Main menu", "Menu utama"),
        ["common.resume"] = new("Resume", "Lanjutkan"),
        ["common.paused"] = new("Paused", "Jeda"),
        ["common.cpu"] = new("CPU", "CPU"),
        ["common.player"] = new("Player {0}", "Pemain {0}"),
        ["common.loading"] = new("Setting up the arena…", "Menyiapkan arena…"),

        // Mode select
        ["mode.title"] = new("Choose a match", "Pilih pertandingan"),
        ["mode.versus.desc"] = new("Best of three rounds, one on one.", "Tiga ronde, satu lawan satu."),
        ["mode.twovtwo.desc"] = new("Four fighters on screen. Hit the same target as your partner for team combos.", "Empat petarung sekaligus. Serang target yang sama dengan rekanmu untuk team combo."),
        ["mode.tag.desc"] = new("Two fighters per team. Press Tag to swap; tag in mid-combo for a launcher.", "Dua petarung per tim. Tekan Tag untuk bertukar; tag di tengah kombo untuk launcher."),
        ["mode.ffa.desc"] = new("Three or four fighters, everyone for themselves.", "Tiga atau empat petarung, semua lawan semua."),
        ["mode.custom.desc"] = new("Set the rounds, timer, damage and arena events yourself.", "Atur sendiri ronde, waktu, damage, dan event arena."),
        ["mode.opponent"] = new("Opponent", "Lawan"),
        ["mode.difficulty"] = new("CPU difficulty", "Kesulitan CPU"),

        // Character select
        ["select.title"] = new("Choose your fighter", "Pilih petarungmu"),
        ["select.category.profession"] = new("Professions", "Profesi"),
        ["select.category.mystic"] = new("Mystic creatures", "Makhluk mistis"),
        ["select.category.wildlife"] = new("Wildlife", "Satwa Nusantara"),
        ["select.category.secret"] = new("Secret legends", "Karakter rahasia"),
        ["select.category.custom"] = new("Your fighters", "Petarungmu"),
        ["select.stats.power"] = new("Power", "Tenaga"),
        ["select.stats.speed"] = new("Speed", "Kecepatan"),
        ["select.stats.defense"] = new("Defense", "Pertahanan"),
        ["select.stats.technique"] = new("Technique", "Teknik"),
        ["select.specials"] = new("Special moves", "Jurus khusus"),
        ["select.finishers"] = new("Finishers", "Finisher"),
        ["select.origin"] = new("From {0}", "Dari {0}"),
        ["select.ready"] = new("Ready", "Siap"),
        ["select.picking"] = new("Picking…", "Memilih…"),

        // Stage select
        ["stage.title"] = new("Choose the arena", "Pilih arena"),
        ["stage.npcs"] = new("Locals", "Warga sekitar"),
        ["stage.events"] = new("Arena events", "Event arena"),
        ["stage.music"] = new("Music", "Musik"),

        // HUD
        ["hud.round"] = new("Round {0}", "Ronde {0}"),
        ["hud.finalRound"] = new("Final round", "Ronde penentuan"),
        ["hud.fight"] = new("Fight!", "Tarung!"),
        ["hud.ko"] = new("K.O.", "K.O."),
        ["hud.timeUp"] = new("Time up", "Waktu habis"),
        ["hud.draw"] = new("Draw", "Seri"),
        ["hud.finishThem"] = new("Finish them!", "Habisi!"),
        ["hud.wins"] = new("{0} wins", "{0} menang"),
        ["hud.teamWins"] = new("Team {0} wins", "Tim {0} menang"),
        ["hud.combo"] = new("{0} hit combo", "Kombo {0} pukulan"),
        ["hud.counter"] = new("Counter", "Counter"),
        ["hud.parry"] = new("Parry", "Tangkis"),
        ["hud.teamCombo"] = new("Team combo", "Team combo"),
        ["hud.rage"] = new("Rage", "Amarah"),
        ["hud.rageReady"] = new("Rage ready", "Amarah siap"),
        ["hud.adrenaline"] = new("Adrenaline", "Adrenalin"),
        ["hud.jurus"] = new("Jurus", "Jurus"),
        ["hud.jurusReady"] = new("Jurus ×{0}", "Jurus ×{0}"),
        ["hud.signature"] = new("Signature move", "Jurus pamungkas"),
        ["moves.signatures"] = new("Signature moves", "Jurus pamungkas"),
        ["moves.meter"] = new("Jurus meter", "Meter jurus"),
        ["hud.crowd"] = new("Crowd", "Sorakan"),
        ["hud.tag"] = new("Tag", "Tag"),
        ["hud.mercy"] = new("Mercy! The fight goes on.", "Ampunan! Pertarungan berlanjut."),
        ["hud.legendaryLocked"] = new("Legendary finisher needs a roaring crowd", "Finisher legenda butuh sorakan penuh"),
        ["hud.arenaEvent"] = new("Arena event", "Event arena"),

        // Results
        ["result.title"] = new("Match result", "Hasil pertandingan"),
        ["result.xp"] = new("+{0} XP", "+{0} XP"),
        ["result.coins"] = new("+{0} coins", "+{0} koin"),
        ["result.unlocked"] = new("Unlocked: {0}", "Terbuka: {0}"),
        ["result.levelUp"] = new("Level {0}!", "Level {0}!"),
        ["result.reputation"] = new("Crowd reputation {0}", "Reputasi penonton {0}"),

        // Story
        ["story.title"] = new("Story campaign", "Mode cerita"),
        ["story.chapter"] = new("Chapter {0}", "Bab {0}"),
        ["story.fight"] = new("Fight this chapter", "Mainkan bab ini"),
        ["story.complete"] = new("Completed", "Selesai"),

        // Tournament
        ["tournament.title"] = new("Tarkam Cup", "Piala Tarkam"),
        ["tournament.quarter"] = new("Quarter-finals", "Perempat final"),
        ["tournament.semi"] = new("Semi-finals", "Semifinal"),
        ["tournament.final"] = new("Final", "Final"),
        ["tournament.champion"] = new("Champion", "Juara"),
        ["tournament.next"] = new("Play next match", "Main pertandingan berikutnya"),
        ["tournament.new"] = new("New tournament", "Turnamen baru"),
        ["tournament.eliminated"] = new("Eliminated", "Tersingkir"),

        // Survival
        ["survival.title"] = new("Survival", "Survival"),
        ["survival.wave"] = new("Wave {0}", "Gelombang {0}"),
        ["survival.best"] = new("Best: wave {0}", "Rekor: gelombang {0}"),
        ["survival.desc"] = new("Health carries over between waves. You recover a little after each win.", "Darah terbawa antar gelombang. Kamu pulih sedikit setiap menang."),

        // Online
        ["online.title"] = new("Online", "Online"),
        ["online.ranked"] = new("Find ranked match", "Cari pertandingan ranked"),
        ["online.clan"] = new("Clan wars", "Perang klan"),
        ["online.server"] = new("Server address", "Alamat server"),
        ["online.connect"] = new("Connect", "Sambungkan"),
        ["online.disconnected"] = new("Not connected. Start TarkamArena.Server, then connect.", "Belum tersambung. Jalankan TarkamArena.Server, lalu sambungkan."),
        ["online.searching"] = new("Searching for an opponent…", "Mencari lawan…"),
        ["online.rating"] = new("Rating {0}", "Rating {0}"),
        ["online.leaderboard"] = new("Leaderboard", "Papan peringkat"),
        ["online.hostLan"] = new("Host LAN match", "Buat pertandingan LAN"),
        ["online.joinLan"] = new("Join LAN match", "Gabung pertandingan LAN"),
        ["online.clanName"] = new("Clan name", "Nama klan"),
        ["online.createClan"] = new("Create clan", "Buat klan"),
        ["online.joinClan"] = new("Join clan", "Gabung klan"),

        // Creator
        ["creator.title"] = new("Fighter creator", "Kreator petarung"),
        ["creator.name"] = new("Name", "Nama"),
        ["creator.base"] = new("Origin", "Asal-usul"),
        ["creator.base.human"] = new("Human fighter", "Petarung manusia"),
        ["creator.base.mystic"] = new("Mystic creature", "Makhluk mistis"),
        ["creator.base.jawara"] = new("Regional champion", "Jawara daerah"),
        ["creator.style"] = new("Fighting style", "Gaya bertarung"),
        ["creator.points"] = new("{0} points left", "Sisa {0} poin"),
        ["creator.colors"] = new("Colours", "Warna"),
        ["creator.height"] = new("Height", "Tinggi badan"),
        ["creator.build"] = new("Build", "Postur"),
        ["creator.features"] = new("Details", "Detail"),
        ["creator.saveFighter"] = new("Save fighter", "Simpan petarung"),
        ["creator.error.name"] = new("Give your fighter a name of 2 to 20 letters.", "Beri nama petarung 2 sampai 20 huruf."),
        ["creator.error.points"] = new("Spend exactly {0} stat points.", "Gunakan tepat {0} poin atribut."),
        ["creator.error.features"] = new("Some details are not available for this origin.", "Beberapa detail tidak tersedia untuk asal-usul ini."),
        ["creator.error.style"] = new("Legend style is reserved for the secret legends.", "Gaya legenda hanya untuk karakter rahasia."),

        // Collection
        ["collection.title"] = new("Collection", "Koleksi"),
        ["collection.costumes"] = new("Costumes", "Kostum"),
        ["collection.poses"] = new("Victory poses", "Pose kemenangan"),
        ["collection.intros"] = new("Intros", "Intro"),
        ["collection.emotes"] = new("Emotes", "Emote"),
        ["collection.banners"] = new("Banners", "Banner"),
        ["collection.achievements"] = new("Achievements", "Pencapaian"),
        ["collection.level"] = new("Level {0}", "Level {0}"),
        ["collection.coins"] = new("{0} coins", "{0} koin"),
        ["collection.buy"] = new("Unlock for {0}", "Buka dengan {0}"),

        // Settings
        ["settings.title"] = new("Settings", "Pengaturan"),
        ["settings.language"] = new("Language", "Bahasa"),
        ["settings.master"] = new("Master volume", "Volume utama"),
        ["settings.music"] = new("Music", "Musik"),
        ["settings.sfx"] = new("Sound effects", "Efek suara"),
        ["settings.quality"] = new("Graphics quality", "Kualitas grafis"),
        ["settings.quality.low"] = new("Low", "Rendah"),
        ["settings.quality.medium"] = new("Medium", "Sedang"),
        ["settings.quality.high"] = new("High", "Tinggi"),
        ["settings.quality.ultra"] = new("Ultra", "Ultra"),
        ["settings.motionBlur"] = new("Motion blur", "Motion blur"),
        ["settings.weather"] = new("Dynamic weather", "Cuaca dinamis"),
        ["settings.fullscreen"] = new("Full screen", "Layar penuh"),
        ["settings.finishers"] = new("Show finishers", "Tampilkan finisher"),
        ["settings.controls"] = new("Controls", "Kontrol"),

        // Credits
        ["credits.title"] = new("Credits", "Kredit"),
        ["credits.director"] = new("Created and directed by", "Dibuat dan disutradarai oleh"),
        ["credits.studio"] = new("Studio", "Studio"),
        ["credits.tech"] = new("Built with", "Dibangun dengan"),
        ["credits.music"] = new("Music", "Musik"),
        ["credits.thanks"] = new("Special thanks", "Terima kasih khusus"),
        ["credits.thanks.body"] = new("Every tarkam tournament, every kampung crowd and every legend of the archipelago.",
                                      "Setiap turnamen tarkam, setiap penonton kampung, dan setiap legenda Nusantara."),

        // Controls
        ["controls.move"] = new("Move", "Gerak"),
        ["controls.jump"] = new("Jump", "Lompat"),
        ["controls.crouch"] = new("Crouch", "Jongkok"),
        ["controls.light"] = new("Light attack", "Serangan ringan"),
        ["controls.heavy"] = new("Heavy attack", "Serangan berat"),
        ["controls.grab"] = new("Grab & throw", "Tangkap & banting"),
        ["controls.block"] = new("Block (tap to parry)", "Tahan (ketuk untuk tangkis)"),
        ["controls.dodge"] = new("Dodge", "Menghindar"),
        ["controls.special"] = new("Special", "Jurus khusus"),
        ["controls.tag"] = new("Tag partner", "Tukar rekan"),
        ["controls.rage"] = new("Rage mode", "Mode amarah"),
        ["controls.pause"] = new("Pause", "Jeda"),

        // Loading tips
        ["loading.tip"] = new("Tip", "Tips"),
        ["tip.0"] = new("Tap Block just before a hit lands to parry: no damage, and the attacker is left open.", "Ketuk Tahan sesaat sebelum pukulan mendarat untuk menangkis: tanpa damage, dan penyerang terbuka."),
        ["tip.1"] = new("Down + Heavy launches. Press Up on hit to jump after them for an air combo.", "Bawah + Berat melontarkan lawan. Tekan Atas saat kena untuk mengejarnya dengan kombo udara."),
        ["tip.2"] = new("Rage fills faster when you are below a quarter of your health. Press Rage when it is full.", "Amarah terisi lebih cepat saat darahmu di bawah seperempat. Tekan Amarah saat penuh."),
        ["tip.3"] = new("Legendary Finishers need a roaring crowd: keep the VU meter in the red.", "Finisher Legenda butuh penonton yang riuh: jaga VU meter tetap merah."),
        ["tip.4"] = new("Two grabs at once cancel each other out. Grabs cannot catch someone in the air.", "Dua tangkapan bersamaan saling batal. Tangkapan tidak bisa menangkap lawan di udara."),
        ["tip.5"] = new("A hit during the opponent's startup is a counter: more damage, longer stun.", "Pukulan saat lawan memulai jurus adalah counter: damage lebih besar, terpaku lebih lama."),
        ["tip.6"] = new("Watch the red warning on the floor: arena events knock everyone down.", "Perhatikan peringatan merah di lantai: event arena menjatuhkan siapa saja."),
        ["tip.7"] = new("In Tag Battle, tag in while the opponent is in hitstun to enter with a launcher.", "Di Tag Battle, tukar rekan saat lawan terkena hitstun untuk masuk dengan launcher."),
        ["tip.8"] = new("Mercy lets the loser stand back up with a sliver of health. The crowd loves it.", "Ampunan membuat yang kalah bangkit dengan sedikit darah. Penonton menyukainya."),
        ["tip.9"] = new("Locals recognise some fighters: traders fear the Market Thug, students cheer the protester.", "Warga mengenali petarung tertentu: pedagang takut Preman Pasar, mahasiswa bersorak untuk Mahasiswi Demo."),
        ["tip.10"] = new("Quarter-circle forward (↓↘→) + attack is a forward special; →↓↘ + attack is the uppercut.", "Seperempat putaran maju (↓↘→) + serangan adalah jurus maju; →↓↘ + serangan adalah uppercut."),
        ["tip.11"] = new("Win a tournament, finish the story or survive ten waves to unlock the secret legends.", "Juarai turnamen, tamatkan cerita, atau bertahan sepuluh gelombang untuk membuka legenda rahasia."),
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
