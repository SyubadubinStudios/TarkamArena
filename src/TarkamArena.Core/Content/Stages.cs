using TarkamArena.Core.Localization;
using static TarkamArena.Core.Content.NpcKind;

namespace TarkamArena.Core.Content;

/// <summary>All arenas. Order is stable for save data and network packets.</summary>
public static class Stages
{
    private static readonly StageDefinition[] s_all = Build();

    public static IReadOnlyList<StageDefinition> All => s_all;

    public static StageDefinition Get(string id) =>
        Array.Find(s_all, s => s.Id == id) ?? throw new KeyNotFoundException($"Unknown stage '{id}'.");

    public static int IndexOf(string id) => Array.FindIndex(s_all, s => s.Id == id);

    private static LocalizedText T(string en, string id) => new(en, id);

    private static ArenaEventDefinition E(ArenaEventKind kind, string en, string id, string descEn, string descId, bool affectsFight = false) =>
        new(kind, T(en, id), T(descEn, descId), affectsFight);

    private static StageDefinition[] Build() =>
    [
        new()
        {
            Id = "pasar-becek", Name = T("Wet Market", "Pasar Becek"), Region = "Jakarta Timur",
            Description = T("Puddles, tarpaulin roofs and a hundred haggling voices. Watch your step on the fish stall.",
                            "Genangan air, atap terpal, dan seratus suara tawar-menawar. Hati-hati di depan lapak ikan."),
            Palette = new(0x4E4A3F, 0x8FB3D9, 0xB9C3C7, 0x1F4FB0, 0xFFF1D6),
            Time = TimeOfDay.Day, Weather = Weather.Cloudy, Music = MusicStyle.Rock,
            Npcs = [Trader, Shopper, Porter, VegetableSeller, Chicken],
            Events =
            [
                E(ArenaEventKind.LooseChicken, "Loose chicken", "Ayam lepas", "A chicken bolts across the arena.", "Seekor ayam kabur melintasi arena."),
                E(ArenaEventKind.PassingCart, "Passing cart", "Gerobak lewat", "A cart rolls through and knocks fighters aside.", "Gerobak lewat dan menyenggol petarung.", true),
                E(ArenaEventKind.RagingGoat, "Raging goat", "Kambing mengamuk", "A goat charges whoever is nearest.", "Kambing menyeruduk siapa pun yang terdekat.", true),
            ],
        },
        new()
        {
            Id = "sawah-jawa-tengah", Name = T("Central Java Rice Fields", "Sawah Jawa Tengah"), Region = "Klaten",
            Description = T("Golden paddies at dusk, with Merapi watching from the horizon.",
                            "Sawah keemasan saat senja, dengan Merapi mengawasi dari kejauhan."),
            Palette = new(0x7A8A3A, 0xF2A65A, 0xE8C9A0, 0xF6C21B, 0xFFD39A),
            Time = TimeOfDay.Dusk, Weather = Weather.Clear, Music = MusicStyle.Gamelan,
            Npcs = [Farmer, Buffalo, Kid, Musician],
            Events =
            [
                E(ArenaEventKind.Procession, "Harvest procession", "Arak-arakan panen", "Villagers parade past with offerings.", "Warga berarak membawa hasil panen."),
                E(ArenaEventKind.Downpour, "Sudden downpour", "Hujan mendadak", "Rain makes the mud slippery.", "Hujan membuat lumpur licin.", true),
            ],
        },
        new()
        {
            Id = "gedung-dpr", Name = T("Parliament Steps", "Gedung DPR"), Region = "Senayan, Jakarta",
            Description = T("Marble steps in front of the green dome. Today's session is very lively.",
                            "Tangga marmer di depan kubah hijau. Sidang hari ini sangat meriah."),
            Palette = new(0xC9C3B5, 0x9CC3E6, 0xD5DCE3, 0x2E8B57, 0xFFFFFF),
            Time = TimeOfDay.Day, Weather = Weather.Clear, Music = MusicStyle.OrchestralHybrid,
            Npcs = [Politician, Journalist, Officer, Photographer],
            Events =
            [
                E(ArenaEventKind.Protest, "Demonstration", "Demonstrasi", "Protesters arrive with banners and megaphones.", "Massa datang membawa spanduk dan toa."),
                E(ArenaEventKind.Fireworks, "Press flashes", "Kilatan kamera", "Every camera flashes at once.", "Semua kamera memotret bersamaan."),
            ],
        },
        new()
        {
            Id = "halte-transjakarta", Name = T("Busway Station", "Stasiun Transjakarta"), Region = "Jakarta Pusat",
            Description = T("A glass bus shelter in the middle of the avenue at rush hour.",
                            "Halte kaca di tengah jalan raya saat jam pulang kantor."),
            Palette = new(0x5A6470, 0x2B3A55, 0x6A7A90, 0xE0322B, 0xCFE3FF),
            Time = TimeOfDay.Night, Weather = Weather.Rain, Music = MusicStyle.Edm,
            Npcs = [Commuter, Officer, Hawker],
            Events =
            [
                E(ArenaEventKind.BusArrival, "Bus arrival", "Bus datang", "A bus pulls in and the doors slide open.", "Bus merapat dan pintunya terbuka."),
                E(ArenaEventKind.PassengerRush, "Passenger rush", "Serbuan penumpang", "Commuters push through the fight.", "Penumpang menerobos arena.", true),
            ],
        },
        new()
        {
            Id = "terminal-kampung-rambutan", Name = T("Kampung Rambutan Terminal", "Terminal Kampung Rambutan"), Region = "Jakarta Timur",
            Description = T("Diesel, shouting conductors and a bus to every city on Java.",
                            "Asap solar, kernet yang berteriak, dan bus ke setiap kota di Jawa."),
            Palette = new(0x55504A, 0xE2B07A, 0xB09A80, 0xF28C1B, 0xFFE2B8),
            Time = TimeOfDay.Day, Weather = Weather.Clear, Music = MusicStyle.MetalHardcore,
            Npcs = [Driver, Conductor, Passenger, Hawker],
            Events =
            [
                E(ArenaEventKind.BusArrival, "Bus arrival", "Bus masuk terminal", "An intercity bus reverses into the bay.", "Bus antarkota mundur masuk jalur."),
                E(ArenaEventKind.PassengerRush, "Crowded platform", "Keramaian penumpang", "Passengers flood the platform.", "Penumpang memadati peron.", true),
            ],
        },
        new()
        {
            Id = "kampung-ambon", Name = T("Kampung Ambon", "Kampung Ambon"), Region = "Jakarta Timur",
            Description = T("Narrow alleys strung with lights and music from every window.",
                            "Gang sempit penuh lampu hias dan musik dari setiap jendela."),
            Palette = new(0x4A3A3A, 0x1E1B3A, 0x3A2E4A, 0xF6C21B, 0xFFB86B),
            Time = TimeOfDay.Night, Weather = Weather.Night, Music = MusicStyle.Sasando,
            Npcs = [Kid, Musician, Hawker, Shopper],
            Events =
            [
                E(ArenaEventKind.Fireworks, "Fireworks", "Kembang api", "Someone is celebrating a wedding nearby.", "Ada yang merayakan pernikahan di dekat sini."),
            ],
        },
        new()
        {
            Id = "hutan-kalimantan", Name = T("Borneo Rainforest", "Hutan Kalimantan"), Region = "Kalimantan Tengah",
            Description = T("Giant trees, hanging vines and eyes watching from the canopy.",
                            "Pohon raksasa, sulur bergelantungan, dan mata yang mengintai dari kanopi."),
            Palette = new(0x2F4A2A, 0x6E9A6A, 0x6F8F6A, 0x2E8B57, 0xE6FFD6),
            Time = TimeOfDay.Day, Weather = Weather.Fog, Music = MusicStyle.Talempong,
            Npcs = [Ranger, Researcher, OrangUtan],
            Events =
            [
                E(ArenaEventKind.FruitThrow, "Fruit barrage", "Orang utan melempar buah", "Orangutans throw fruit at the fighters.", "Orang utan melempari petarung dengan buah.", true),
                E(ArenaEventKind.FallingTree, "Falling tree", "Pohon tumbang", "A dead tree crashes across the arena.", "Pohon mati tumbang melintang arena.", true),
            ],
            HalfWidth = 8f,
        },
        new()
        {
            Id = "pantai-selatan", Name = T("Southern Beach", "Pantai Selatan"), Region = "Parangtritis",
            Description = T("Black sand, roaring surf and a queen who does not like the colour green.",
                            "Pasir hitam, ombak menggelegar, dan sang ratu yang tak suka warna hijau."),
            Palette = new(0x2A2A2E, 0x3A5A7A, 0x5A7A8A, 0x0E7A52, 0xBFE6FF),
            Time = TimeOfDay.Dusk, Weather = Weather.Storm, Music = MusicStyle.OrchestralHybrid,
            Npcs = [Fisherman, Tourist, Photographer],
            Events =
            [
                E(ArenaEventKind.BigWave, "Rogue wave", "Ombak besar", "A wave sweeps the beach and drags fighters back.", "Ombak menyapu pantai dan menyeret petarung.", true),
            ],
            HalfWidth = 8.5f,
        },
        new()
        {
            Id = "kota-tua", Name = T("Old Town Jakarta", "Kota Tua Jakarta"), Region = "Jakarta Barat",
            Description = T("Colonial facades, rental bicycles and street performers frozen like statues.",
                            "Fasad kolonial, sepeda sewaan, dan seniman jalanan yang diam seperti patung."),
            Palette = new(0x8A6A4A, 0xF0C080, 0xD9B48A, 0xE0322B, 0xFFE8C0),
            Time = TimeOfDay.Dusk, Weather = Weather.Clear, Music = MusicStyle.Angklung,
            Npcs = [Tourist, Photographer, Musician, Hawker],
            Events =
            [
                E(ArenaEventKind.TourBus, "Tour group", "Rombongan wisata", "A tour group stops to film the fight.", "Rombongan wisata berhenti merekam pertarungan."),
            ],
        },
        new()
        {
            Id = "gunung-bromo", Name = T("Mount Bromo", "Gunung Bromo"), Region = "Jawa Timur",
            Description = T("A sea of sand under a smoking crater at sunrise.",
                            "Lautan pasir di bawah kawah berasap saat matahari terbit."),
            Palette = new(0x6A5A4E, 0xF5B38A, 0xC8A898, 0xE0322B, 0xFFD6B0),
            Time = TimeOfDay.Dawn, Weather = Weather.Ash, Music = MusicStyle.DegungSunda,
            Npcs = [Tourist, Monk, Photographer],
            Events =
            [
                E(ArenaEventKind.Eruption, "Ash burst", "Semburan abu", "The crater coughs ash over the arena.", "Kawah menyemburkan abu ke arena.", true),
                E(ArenaEventKind.Procession, "Kasada offering", "Upacara Kasada", "Pilgrims carry offerings towards the crater.", "Peziarah membawa sesaji menuju kawah."),
            ],
            HalfWidth = 9f,
        },
        new()
        {
            Id = "candi-borobudur", Name = T("Borobudur Temple", "Candi Borobudur"), Region = "Magelang",
            Description = T("Stone terraces and bell-shaped stupas above the morning mist.",
                            "Teras batu dan stupa berbentuk lonceng di atas kabut pagi."),
            Palette = new(0x7A7468, 0xBFD3E0, 0xD0D6D8, 0xB8862B, 0xFFF4DE),
            Time = TimeOfDay.Dawn, Weather = Weather.Fog, Music = MusicStyle.Gamelan,
            Npcs = [Monk, Tourist, Photographer],
            Events =
            [
                E(ArenaEventKind.Procession, "Lantern procession", "Arak-arakan lampion", "Monks walk the terraces with lanterns.", "Para biksu berjalan di teras membawa lampion."),
            ],
        },
        new()
        {
            Id = "karnaval-sound-horeg", Name = T("Sound Horeg Carnival", "Karnaval Sound Horeg"), Region = "Malang, Jawa Timur",
            Description = T("A village road lined with sound-system trucks stacked three storeys high. The bass is a weather system of its own.",
                            "Jalan desa dipenuhi truk sound system bertumpuk tiga lantai. Bass-nya menjadi cuaca tersendiri."),
            Palette = new(0x3A3A44, 0x1A1030, 0x40305A, 0xFF3EA5, 0xB0FFEA),
            Time = TimeOfDay.Night, Weather = Weather.Night, Music = MusicStyle.Horeg,
            Npcs = [Dancer, SoundCrew, Kid, Hawker, Villager],
            Events =
            [
                E(ArenaEventKind.BassDrop, "Bass drop", "Bass drop", "The sound wall drops the bass and the shockwave throws fighters.", "Tembok sound menjatuhkan bass dan gelombangnya melempar petarung.", true),
                E(ArenaEventKind.Procession, "Sound truck parade", "Pawai truk sound", "Another sound truck crawls past, horns blaring.", "Truk sound lain merayap lewat sambil membunyikan klakson."),
                E(ArenaEventKind.Blackout, "Power cut", "Listrik padam", "The generator trips and the lights flicker out.", "Genset jeglek dan lampu berkedip padam."),
            ],
            HalfWidth = 8f, CrowdDensity = 90,
        },
        new()
        {
            Id = "hutan-gambut-riau", Name = T("Burning Peatland", "Hutan Gambut Riau"), Region = "Riau",
            Description = T("Orange smoke, smouldering peat and firefighters holding the line between the fire and the villages.",
                            "Asap jingga, gambut membara, dan para pemadam menahan api agar tak sampai ke desa."),
            Palette = new(0x3A2A1E, 0xC8743A, 0xA0704A, 0xF28C1B, 0xFFB070),
            Time = TimeOfDay.Day, Weather = Weather.Ash, Music = MusicStyle.MetalHardcore,
            Npcs = [Firefighter, Villager, Ranger],
            Events =
            [
                E(ArenaEventKind.FireSpread, "Fire flare-up", "Api menjalar", "A patch of peat bursts into flame under the fighters.", "Sepetak gambut tiba-tiba menyala di bawah petarung.", true),
                E(ArenaEventKind.WaterBomb, "Water bomb", "Bom air", "A helicopter drops water over the arena.", "Helikopter menjatuhkan air ke arena.", true),
            ],
            HalfWidth = 8f, CrowdDensity = 25,
        },
        new()
        {
            Id = "alun-alun-ponorogo", Name = T("Ponorogo Town Square", "Alun-alun Ponorogo"), Region = "Ponorogo, Jawa Timur",
            Description = T("The Grebeg Suro festival: reog troupes, kendang drums and a square packed shoulder to shoulder.",
                            "Festival Grebeg Suro: rombongan reog, tabuhan kendang, dan alun-alun penuh sesak."),
            Palette = new(0x6A5A40, 0x2A2050, 0x4A4060, 0x2E8B57, 0xFFD39A),
            Time = TimeOfDay.Night, Weather = Weather.Clear, Music = MusicStyle.ReogGamelan,
            Npcs = [Warok, Musician, Dancer, Tourist, Hawker],
            Events =
            [
                E(ArenaEventKind.Procession, "Reog parade", "Kirab reog", "A full reog troupe dances across the back of the square.", "Satu rombongan reog menari melintasi alun-alun."),
                E(ArenaEventKind.Fireworks, "Festival fireworks", "Kembang api festival", "Fireworks light up the night sky.", "Kembang api menerangi langit malam."),
            ],
            CrowdDensity = 85,
        },
        new()
        {
            Id = "malam-pengerupukan", Name = T("Pengerupukan Night", "Malam Pengerupukan"), Region = "Denpasar, Bali",
            Description = T("The night before Nyepi. Giant ogoh-ogoh effigies are spun at the crossroads to the crash of baleganjur.",
                            "Malam sebelum Nyepi. Ogoh-ogoh raksasa diputar di perempatan diiringi gemuruh baleganjur."),
            Palette = new(0x4A3A30, 0x140C24, 0x3A2440, 0xC0392B, 0xFFB86B),
            Time = TimeOfDay.Night, Weather = Weather.Night, Music = MusicStyle.Baleganjur,
            Npcs = [Pecalang, Musician, Tourist, Kid],
            Events =
            [
                E(ArenaEventKind.Procession, "Ogoh-ogoh spin", "Ogoh-ogoh diputar", "Bearers spin a giant effigy at the crossroads.", "Para pengusung memutar ogoh-ogoh raksasa di perempatan.", true),
                E(ArenaEventKind.Fireworks, "Torch flare", "Obor menyala", "Torches flare up along the road.", "Obor menyala sepanjang jalan."),
            ],
            HalfWidth = 8f, CrowdDensity = 80,
        },
        new()
        {
            Id = "jalan-demo", Name = T("Protest Avenue", "Jalan Demo"), Region = "Jakarta Pusat",
            Description = T("Students, banners and barricades in front of the government building. The water cannon truck is idling.",
                            "Mahasiswa, spanduk, dan barikade di depan gedung pemerintahan. Mobil water cannon sudah bersiap."),
            Palette = new(0x5A5A5A, 0x9AA5B5, 0xB0B8C0, 0xF6C21B, 0xFFFFFF),
            Time = TimeOfDay.Day, Weather = Weather.Cloudy, Music = MusicStyle.Punk,
            Npcs = [Student, Officer, Journalist, Hawker, Photographer],
            Events =
            [
                E(ArenaEventKind.WaterCannon, "Water cannon", "Water cannon", "A jet of water sweeps across the arena.", "Semprotan air menyapu arena.", true),
                E(ArenaEventKind.TearGas, "Tear gas", "Gas air mata", "A cloud drifts over the crowd and everyone coughs.", "Awan gas melayang di atas massa dan semua terbatuk."),
                E(ArenaEventKind.Protest, "Long march", "Long march", "A new column of protesters arrives singing.", "Barisan demonstran baru tiba sambil bernyanyi."),
            ],
            HalfWidth = 8.5f, CrowdDensity = 95,
        },
        new()
        {
            Id = "pangkalan-ojol", Name = T("Under the Flyover", "Pangkalan Ojol"), Region = "Jakarta Selatan",
            Description = T("Under a concrete flyover at night, where a hundred drivers wait for orders and share one coffee stall.",
                            "Di bawah jalan layang pada malam hari, tempat seratus pengemudi menunggu order dan berbagi satu warung kopi."),
            Palette = new(0x4A4E55, 0x1E2433, 0x3A4255, 0x00A651, 0xFFE2B8),
            Time = TimeOfDay.Night, Weather = Weather.Rain, Music = MusicStyle.DangdutKoplo,
            Npcs = [OjolDriver, Hawker, Commuter],
            Events =
            [
                E(ArenaEventKind.MotorbikeSwarm, "Motorbike swarm", "Serbuan motor", "A swarm of motorbikes cuts through the arena.", "Rombongan motor memotong lewat arena.", true),
                E(ArenaEventKind.Downpour, "Downpour", "Hujan deras", "Rain hammers the flyover and everyone huddles closer.", "Hujan mengguyur dan semua berdesakan berteduh."),
            ],
        },
        new()
        {
            Id = "ruang-sidang", Name = T("The Courtroom", "Ruang Sidang Tipikor"), Region = "Jakarta",
            Description = T("Wood panelling, a red-and-white flag and a gallery full of cameras. The verdict is very anticipated. A fictional courtroom.",
                            "Panel kayu, bendera merah putih, dan balkon penuh kamera. Putusan hari ini sangat dinantikan. Ruang sidang fiksi."),
            Palette = new(0x5A3A24, 0xE8D8B8, 0xC8B898, 0xE0322B, 0xFFF4DE),
            Time = TimeOfDay.Day, Weather = Weather.Clear, Music = MusicStyle.OrchestralHybrid,
            Npcs = [Judge, Lawyer, Journalist, Officer, Politician],
            Events =
            [
                E(ArenaEventKind.Fireworks, "Camera flashes", "Kilatan kamera", "Every camera in the gallery flashes at once.", "Semua kamera di balkon memotret bersamaan."),
                E(ArenaEventKind.Protest, "Order in the court", "Harap tenang", "The judge bangs the gavel and the gallery erupts.", "Hakim mengetuk palu dan balkon riuh."),
            ],
            HalfWidth = 6.5f, CrowdDensity = 45,
        },
    ];
}
