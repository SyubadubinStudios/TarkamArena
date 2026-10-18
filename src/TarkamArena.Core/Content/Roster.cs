using TarkamArena.Core.Localization;
using static TarkamArena.Core.Content.BodyFeature;

namespace TarkamArena.Core.Content;

/// <summary>
/// Every fighter in the game. Order is stable: indices are used by save data and network packets,
/// so new fighters are appended, never inserted.
/// </summary>
public static class Roster
{
    // Declared before s_all: static fields initialise in textual order.
    private static readonly LocalizedText[] s_dlcFinisherTemplates =
    [
        new("Night Terror", "Teror Malam"), new("Final Verdict", "Vonis Akhir"), new("Last Breath", "Napas Terakhir"),
        new("Relentless Combo", "Kombo Tanpa Ampun"), new("Total Beatdown", "Hajar Habis"),
        new("Group Photo", "Foto Bareng"), new("Dance Battle", "Joget Bareng"),
        new("Walk Away", "Pergi Sana"),
        new("Legend Reborn", "Legenda Bangkit"),
    ];

    private static readonly CharacterDefinition[] s_all = Build();
    private static readonly Dictionary<string, CharacterDefinition> s_byId = s_all.ToDictionary(c => c.Id, StringComparer.Ordinal);

    public static IReadOnlyList<CharacterDefinition> All => s_all;

    /// <summary>Base game fighters (no DLC).</summary>
    public static IEnumerable<CharacterDefinition> BaseGame => s_all.Where(c => c.Dlc == DlcPack.None);

    public static CharacterDefinition Get(string id) =>
        s_byId.TryGetValue(id, out var c) ? c : throw new KeyNotFoundException($"Unknown fighter '{id}'.");

    public static bool TryGet(string id, out CharacterDefinition character) => s_byId.TryGetValue(id, out character!);

    public static int IndexOf(string id) => Array.FindIndex(s_all, c => c.Id == id);

    public static IEnumerable<CharacterDefinition> InCategory(CharacterCategory category) =>
        s_all.Where(c => c.Category == category && c.Dlc == DlcPack.None);

    private static LocalizedText T(string en, string id) => new(en, id);

    private static CharacterDefinition[] Build() =>
    [
        // ───────────── Profesi / Professions ─────────────
        new()
        {
            Id = "gembel-lampu-merah", Name = T("Bang Jarot", "Bang Jarot"),
            Title = T("The Traffic Light Drifter", "Gembel Lampu Merah"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Striker, Origin = "Jakarta",
            Bio = T("Jarot has lived at the same crossroads for twenty years. He knows every light cycle by heart and fights like the traffic: stop, wait, then all at once.",
                    "Jarot sudah dua puluh tahun hidup di perempatan yang sama. Ia hafal setiap siklus lampu dan bertarung seperti lalu lintas: berhenti, menunggu, lalu menerjang sekaligus."),
            Stats = new(5, 8, 4, 7),
            Body = new(1.72f, 0.95f, 1.0f, 0x6B5B45, 0x3E4A3D, 0xB07A50, Hat | LongHair, 0xE0322B),
            Specials = new(T("Tin Can Toss", "Lempar Kaleng"), T("Red Light Somersault", "Salto Lampu Merah"), T("Busker's Smash", "Kencrung Maut")),
            FinisherNames =
            [
                T("Green Means Go", "Lampu Hijau, Jalan!"), T("Rush Hour Pileup", "Tabrakan Jam Sibuk"), T("Coin Rain", "Hujan Receh"),
                T("Window Washer", "Lap Kaca Paksa"), T("Busker Encore", "Ngamen Tambahan"),
                T("Shared Rice Box", "Nasi Bungkus Berdua"), T("Street Duet", "Duet Jalanan"),
                T("Spare Change", "Seikhlasnya Saja"),
                T("King of the Crossroads", "Raja Perempatan"),
            ],
            Catchphrase = T("Spare a little? No? Then take this!", "Seikhlasnya, Bang? Nggak? Ya udah, terima ini!"),
        },
        new()
        {
            Id = "kasir-minimarket", Name = T("Mbak Sinta", "Mbak Sinta"),
            Title = T("The Minimarket Cashier", "Kasir Minimarket"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Striker, Origin = "Bekasi",
            Bio = T("Sinta has scanned a million items and smiled through every one. Her greeting is legendary; so is the speed of her elbow.",
                    "Sinta sudah memindai sejuta barang dan tersenyum di setiap transaksi. Sapaannya melegenda, begitu juga kecepatan sikunya."),
            Stats = new(4, 9, 5, 7),
            Body = new(1.60f, 0.85f, 1.0f, 0x1F4FB0, 0xE0322B, 0xD9A47A, Uniform | Headscarf, 0xF6C21B),
            Specials = new(T("Receipt Whip", "Struk Panjang"), T("Barcode Dash", "Scan Barcode"), T("Cash Drawer Ding", "Laci Kasir Ding!")),
            FinisherNames =
            [
                T("Price Check", "Cek Harga"), T("Void Transaction", "Batal Transaksi"), T("Shelf Restock", "Isi Ulang Rak"),
                T("Bag It Up", "Kantong Plastik Paksa"), T("Member Points", "Poin Member Ganda"),
                T("Free Sample", "Tester Gratis"), T("Buy One Get One", "Beli Satu Gratis Satu"),
                T("Exact Change", "Uang Pas Saja"),
                T("Grand Opening Sale", "Promo Grand Opening"),
            ],
            Catchphrase = T("Would you like to top up too, sis?", "Sekalian isi pulsanya, Kak?"),
        },
        new()
        {
            Id = "guru-matematika", Name = T("Pak Darmo", "Pak Darmo"),
            Title = T("The Math Teacher", "Guru Matematika"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Mystic, Origin = "Yogyakarta",
            Bio = T("Thirty-one years of teaching, and not one student has escaped his chalk. Pak Darmo calculates the angle of every throw before it leaves his hand.",
                    "Tiga puluh satu tahun mengajar, dan belum ada satu murid pun yang lolos dari kapurnya. Pak Darmo menghitung sudut setiap lemparan sebelum dilepas."),
            Stats = new(5, 5, 6, 9),
            Body = new(1.68f, 0.95f, 1.05f, 0xC9B78F, 0x5A4632, 0xC48D62, Glasses, 0xF3EEE1),
            Specials = new(T("Chalk Sniper", "Lemparan Kapur"), T("Wooden Ruler Lunge", "Sabetan Penggaris"), T("Pop Quiz", "Ulangan Dadakan")),
            FinisherNames =
            [
                T("Division by Zero", "Pembagian Nol"), T("Pythagorean Fold", "Lipatan Pythagoras"), T("Final Exam", "Ujian Akhir"),
                T("Show Your Work", "Tulis Caranya!"), T("Homework Avalanche", "Longsoran PR"),
                T("Extra Credit", "Nilai Tambahan"), T("Field Trip", "Karyawisata"),
                T("Remedial Class", "Remedial Saja"),
                T("Theorem of Legends", "Teorema Sang Legenda"),
            ],
            Catchphrase = T("Turn to page forty-seven.", "Kerjakan halaman empat puluh tujuh."),
        },
        new()
        {
            Id = "pesilat", Name = T("Mas Rangga", "Mas Rangga"),
            Title = T("The Silat Master", "Pesilat"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Striker, Origin = "Padang",
            Bio = T("Rangga trained in a surau courtyard before dawn every day of his childhood. His silat is patient, low and devastating.",
                    "Rangga berlatih di halaman surau sebelum subuh sepanjang masa kecilnya. Silatnya sabar, rendah, dan mematikan."),
            Stats = new(6, 8, 6, 10),
            Body = new(1.74f, 0.95f, 1.0f, 0x161616, 0xB8862B, 0xB57B52, Headscarf, 0xB8862B),
            Specials = new(T("Tiger Stance", "Jurus Harimau"), T("Crescent Kick", "Tendangan Sabit"), T("Joint Lock", "Kuncian Sendi")),
            FinisherNames =
            [
                T("Falling Leaf", "Daun Gugur"), T("Thousand Steps", "Langkah Seribu"), T("Spine of the Tiger", "Tulang Harimau"),
                T("Elbow Rain", "Hujan Siku"), T("Sweeping River", "Sapuan Sungai"),
                T("Bow of Respect", "Salam Hormat"), T("Training Partner", "Teman Latihan"),
                T("Spared by Honour", "Dimaafkan Guru"),
                T("Spirit of the Ancestors", "Roh Para Leluhur"),
            ],
            Catchphrase = T("Silat is not for showing off.", "Silat bukan untuk gaya-gayaan."),
        },
        new()
        {
            Id = "preman-pasar", Name = T("Bang Codet", "Bang Codet"),
            Title = T("The Market Thug", "Preman Pasar"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Grappler, Origin = "Medan",
            Bio = T("Every stall in the market pays Codet's 'security fee'. He has a scar for every argument he won, and he has won all of them.",
                    "Setiap lapak di pasar membayar 'uang keamanan' ke Codet. Ia punya bekas luka untuk setiap debat yang ia menangkan, dan ia menang semuanya."),
            Stats = new(9, 4, 7, 4),
            Body = new(1.82f, 1.25f, 0.95f, 0x2B2B2B, 0x8C1C13, 0xA86A44, Mask, 0xE0322B),
            Specials = new(T("Shakedown", "Palak Kilat"), T("Stall Charge", "Seruduk Lapak"), T("Market Slam", "Bantingan Pasar")),
            FinisherNames =
            [
                T("Protection Fee", "Uang Keamanan"), T("Closing Time", "Pasar Bubar"), T("Fish Market Toss", "Lempar Ikan Asin"),
                T("Overturned Stall", "Lapak Terbalik"), T("Scar Collection", "Koleksi Codet"),
                T("Market Discount", "Diskon Pasar"), T("Lifting Buddy", "Angkat Barang Bareng"),
                T("Debt Forgiven", "Utang Lunas"),
                T("Lord of the Market", "Penguasa Pasar"),
            ],
            Catchphrase = T("You're on my turf now.", "Ini wilayah gue, Bos."),
        },
        new()
        {
            Id = "tukang-parkir", Name = T("Bang Priit", "Bang Priit"),
            Title = T("The Parking Attendant", "Tukang Parkir"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Brawler, Origin = "Surabaya",
            Bio = T("Nobody sees Priit arrive, but he is always there when you leave. His whistle can stop a truck and his footwork can stop you.",
                    "Tak ada yang melihat Priit datang, tapi ia selalu ada saat kamu mau pergi. Peluitnya bisa menghentikan truk dan langkahnya bisa menghentikanmu."),
            Stats = new(5, 7, 5, 6),
            Body = new(1.70f, 1.0f, 1.0f, 0xF28C1B, 0x2E5C8A, 0xB98055, Hat | Uniform, 0xF6C21B),
            Specials = new(T("Whistle Blast", "Priiit!"), T("Reverse, Reverse", "Mundur Terus"), T("Two Thousand Fee", "Goceng Dulu")),
            FinisherNames =
            [
                T("Tow Away Zone", "Area Derek"), T("Double Parked", "Parkir Ganda"), T("Final Whistle", "Peluit Terakhir"),
                T("Left, Left, Keep Going", "Kiri, Kiri, Terus"), T("Wheel Clamp", "Gembok Roda"),
                T("Free Parking", "Parkir Gratis"), T("Help Push Start", "Bantu Dorong Motor"),
                T("No Charge Today", "Hari Ini Gratis"),
                T("Guardian of Every Lot", "Penjaga Segala Lahan"),
            ],
            Catchphrase = T("Left, left, keep going... stop!", "Kiri, kiri, terus... yak, stop!"),
        },
        new()
        {
            Id = "satpam-komplek", Name = T("Pak Slamet", "Pak Slamet"),
            Title = T("The Neighbourhood Guard", "Satpam Komplek"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Grappler, Origin = "Depok",
            Bio = T("Slamet has guarded the same gate through floods, blackouts and three mayors. He never sleeps on the night shift, or so he says.",
                    "Slamet menjaga gerbang yang sama melewati banjir, mati lampu, dan tiga kali ganti wali kota. Ia tidak pernah tidur saat jaga malam, katanya."),
            Stats = new(7, 4, 9, 5),
            Body = new(1.75f, 1.15f, 1.0f, 0x2C3E66, 0xF3EEE1, 0xB27C55, Hat | Uniform | Weapon, 0xF6C21B),
            Specials = new(T("Baton Strike", "Pentungan Ronda"), T("Night Patrol", "Patroli Malam"), T("Alarm Gong", "Kentongan Siaga")),
            FinisherNames =
            [
                T("Gate Closed", "Portal Ditutup"), T("Curfew", "Jam Malam"), T("Lights Out", "Mati Lampu"),
                T("Guest Book Slam", "Buku Tamu Wajib"), T("Patrol Round", "Keliling Komplek"),
                T("Hot Coffee Break", "Ngopi di Pos"), T("Neighbourhood Watch", "Ronda Bersama"),
                T("Let Through", "Silakan Lewat"),
                T("Guardian of the Gate", "Penjaga Gerbang Abadi"),
            ],
            Catchphrase = T("ID card, please.", "Mohon tinggalkan KTP, Pak."),
        },
        new()
        {
            Id = "abang-bakso", Name = T("Mas Kumis", "Mas Kumis"),
            Title = T("The Meatball Vendor", "Abang Bakso"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Brawler, Origin = "Wonogiri",
            Bio = T("Kumis pushes a cart heavier than a motorbike up every hill in town. The broth is hot, the fists are hotter.",
                    "Kumis mendorong gerobak yang lebih berat dari motor ke setiap tanjakan di kota. Kuahnya panas, tinjunya lebih panas."),
            Stats = new(7, 5, 6, 6),
            Body = new(1.69f, 1.1f, 1.0f, 0xF3EEE1, 0x2E8B57, 0xB98055, Hat | Apron | Cart, 0xE0322B),
            Specials = new(T("Meatball Barrage", "Bakso Beranak"), T("Runaway Cart", "Gerobak Ngebut"), T("Hot Broth Splash", "Siraman Kuah Panas")),
            FinisherNames =
            [
                T("Extra Spicy", "Sambal Lima Sendok"), T("Bowl Smash", "Mangkok Pecah"), T("Mega Meatball", "Bakso Jumbo"),
                T("Noodle Lasso", "Laso Mi Kuning"), T("Bell of Doom", "Kentongan Mangkok"),
                T("Free Refill", "Tambah Kuah Gratis"), T("Late Night Snack", "Bakso Tengah Malam"),
                T("Pay Tomorrow", "Bayar Besok Aja"),
                T("Emperor of Broth", "Kaisar Kuah Kaldu"),
            ],
            Catchphrase = T("Meatballs! Spicy ones too!", "Bakso, bakso! Yang pedas ada!"),
        },
        new()
        {
            Id = "emak-emak-komplek", Name = T("Bu Ratna", "Bu Ratna"),
            Title = T("The Neighbourhood Mom", "Emak-emak Komplek"),
            Category = CharacterCategory.Profession, Archetype = Archetype.Brawler, Origin = "Bandung",
            Bio = T("Ratna runs the arisan, the WhatsApp group and the whole street. Her left indicator means she is turning right, and nobody dares argue.",
                    "Ratna mengatur arisan, grup WhatsApp, dan satu jalan penuh. Lampu sein kirinya berarti belok kanan, dan tak ada yang berani protes."),
            Stats = new(6, 6, 7, 7),
            Body = new(1.58f, 1.1f, 1.0f, 0x9C27B0, 0xF6C21B, 0xD49A6E, Headscarf, 0xE0322B),
            Specials = new(T("Homing Flip-Flop", "Sandal Kendali"), T("Left Signal, Right Turn", "Sein Kiri Belok Kanan"), T("Endless Scolding", "Omelan Panjang")),
            FinisherNames =
            [
                T("The Wooden Spoon", "Centong Sakti"), T("Group Chat Exposed", "Viral di Grup"), T("Laundry Line", "Jemuran Maut"),
                T("Sandal Boomerang", "Sandal Bumerang"), T("Arisan Collection", "Tagihan Arisan"),
                T("Have You Eaten?", "Udah Makan Belum?"), T("Gossip Session", "Ngerumpi Bareng"),
                T("Just This Once", "Kali Ini Dimaafkan"),
                T("Queen of the Complex", "Ratu Komplek"),
            ],
            Catchphrase = T("I've told you a hundred times!", "Kamu tuh ya, dibilangin dari tadi!"),
        },

        // ───────────── Makhluk Mistis / Mystic Creatures ─────────────
        new()
        {
            Id = "kuntilanak", Name = T("Kunti", "Kunti"),
            Title = T("The Weeping Lady", "Kuntilanak"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Pontianak",
            Bio = T("Her laugh carries across rivers. When it sounds far away she is right behind you; when it sounds close, run.",
                    "Tawanya terdengar melintasi sungai. Saat terdengar jauh, ia ada tepat di belakangmu; saat terdengar dekat, larilah."),
            Stats = new(6, 8, 4, 8),
            Body = new(1.70f, 0.8f, 1.0f, 0xF3EEE1, 0xDCD6C8, 0xE8DCCB, LongHair | Floating, 0xE0322B),
            Specials = new(T("Piercing Laughter", "Tawa Melengking"), T("Drifting Flight", "Terbang Melayang"), T("Midnight Claw", "Cakar Tengah Malam")),
            FinisherNames =
            [
                T("Hair Cocoon", "Kepompong Rambut"), T("Banana Tree Ambush", "Sergapan Pohon Pisang"), T("Last Laugh", "Tawa Terakhir"),
                T("Nail Flurry", "Kuku Seribu"), T("River Mist", "Kabut Sungai"),
                T("Lullaby", "Nina Bobo"), T("Hair Care Day", "Hari Keramas"),
                T("Fade Away", "Menghilang Saja"),
                T("Queen of the Night", "Ratu Malam Jumat"),
            ],
            Catchphrase = T("Hihihihi...", "Hihihihi..."),
        },
        new()
        {
            Id = "pocong", Name = T("Pocong Joni", "Pocong Joni"),
            Title = T("The Bound Spirit", "Pocong"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Brawler, Origin = "Solo",
            Bio = T("Joni's ties were never untied, so he hops. He has hopped for so long that his legs, wherever they are, are terrifyingly strong.",
                    "Tali Joni tak pernah dilepas, jadi ia melompat. Ia sudah melompat begitu lama sampai kakinya, di mana pun itu, jadi sangat kuat."),
            Stats = new(7, 5, 6, 5),
            Body = new(1.75f, 0.9f, 1.0f, 0xF3EEE1, 0xCFC8B6, 0xD6CFC0, Shroud | Bound, 0xF6C21B),
            Specials = new(T("Shroud Hop", "Lompat Pocong"), T("Shroud Roll", "Guling Kafan"), T("Binding Knot", "Ikatan Tali")),
            FinisherNames =
            [
                T("Final Hop", "Lompatan Pamungkas"), T("Wrapped Up", "Dibungkus Rapi"), T("Graveyard Bounce", "Pantulan Kuburan"),
                T("Headbutt Barrage", "Sundulan Bertubi"), T("Rolling Thunder", "Guling Petir"),
                T("Untie Me Please", "Tolong Lepasin Talinya"), T("Hopscotch", "Main Engklek"),
                T("Hop Away", "Lompat Pulang"),
                T("King of the Cemetery", "Raja Pemakaman"),
            ],
            Catchphrase = T("Untie me... please...", "Lepasin... talinya..."),
        },
        new()
        {
            Id = "buto-ijo", Name = T("Buto Ijo", "Buto Ijo"),
            Title = T("The Green Giant", "Buto Ijo"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Grappler, Origin = "Jawa Tengah",
            Bio = T("A hungry giant from the old tales, once outwitted by Timun Mas. He has been waiting a very long time for a rematch.",
                    "Raksasa lapar dari dongeng lama yang pernah dikalahkan Timun Mas. Ia sudah lama sekali menunggu pertandingan ulang."),
            Stats = new(10, 3, 8, 3),
            Body = new(2.25f, 1.45f, 1.1f, 0x2E8B57, 0x5A3A1E, 0x3FA265, Horns | Fur, 0xF6C21B),
            Specials = new(T("Giant's Roar", "Raungan Raksasa"), T("Earth Pound", "Hantaman Bumi"), T("Swallow Whole", "Telan Bulat-bulat")),
            FinisherNames =
            [
                T("Cucumber Revenge", "Dendam Timun Mas"), T("Mountain Stomp", "Injakan Gunung"), T("Giant Appetite", "Nafsu Makan Raksasa"),
                T("Ground and Pound", "Tumbuk Tanah"), T("Boulder Toss", "Lempar Batu Kali"),
                T("Snack Time", "Waktunya Ngemil"), T("Gentle Giant", "Raksasa Baik Hati"),
                T("Not Hungry Today", "Lagi Kenyang"),
                T("Titan of Java", "Raksasa Tanah Jawa"),
            ],
            Catchphrase = T("I smell something tasty...", "Hmm... bau manusia..."),
        },
        new()
        {
            Id = "genderuwo", Name = T("Genderuwo", "Genderuwo"),
            Title = T("The Forest Shadow", "Genderuwo"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Grappler, Origin = "Jawa Timur",
            Bio = T("A shaggy shadow that lives in old banyan trees. It throws stones at those who disrespect the forest, and it never misses.",
                    "Bayangan berbulu yang tinggal di pohon beringin tua. Ia melempar batu ke siapa pun yang tak menghormati hutan, dan tak pernah meleset."),
            Stats = new(9, 4, 7, 5),
            Body = new(2.05f, 1.35f, 1.0f, 0x2A1F18, 0x4A3526, 0x3A2A20, Fur | Glow, 0xE0322B),
            Specials = new(T("Hurled Stone", "Batu Melayang"), T("Shadow Lunge", "Terkaman Bayangan"), T("Dark Grip", "Cengkeram Gelap")),
            FinisherNames =
            [
                T("Banyan Embrace", "Pelukan Beringin"), T("Stone Storm", "Badai Batu"), T("Eyes in the Dark", "Mata dalam Gelap"),
                T("Shaggy Slam", "Bantingan Bulu"), T("Forest Toss", "Lempar ke Hutan"),
                T("Tree Planting", "Tanam Pohon"), T("Firefly Night", "Malam Kunang-kunang"),
                T("Leave the Forest", "Pergi dari Hutanku"),
                T("Lord of the Banyan", "Penguasa Beringin"),
            ],
            Catchphrase = T("Respect the old trees.", "Hormati pohon tua."),
        },
        new()
        {
            Id = "wewe-gombel", Name = T("Wewe Gombel", "Wewe Gombel"),
            Title = T("The Hiding Mother", "Wewe Gombel"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Semarang",
            Bio = T("She hides neglected children from careless parents and returns them only when they are missed. Her long arms reach further than you think.",
                    "Ia menyembunyikan anak-anak yang ditelantarkan dan hanya mengembalikannya setelah mereka dirindukan. Lengannya menjangkau lebih jauh dari dugaanmu."),
            Stats = new(6, 6, 6, 8),
            Body = new(1.85f, 1.0f, 1.0f, 0x4A3B52, 0x2E2633, 0x8E7A6A, LongHair | LongArms, 0xB8862B),
            Specials = new(T("Treetop Vanish", "Sembunyi di Pohon"), T("Long Embrace", "Rangkulan Panjang"), T("Mournful Cry", "Tangis Pilu")),
            FinisherNames =
            [
                T("Hide and Never Seek", "Petak Umpet Abadi"), T("Gombel Hill", "Bukit Gombel"), T("Long Reach", "Jangkauan Panjang"),
                T("Branch Whip", "Cambuk Dahan"), T("Lost and Found", "Hilang Dicari"),
                T("Bedtime Story", "Dongeng Sebelum Tidur"), T("Family Reunion", "Kumpul Keluarga"),
                T("Go Home", "Pulanglah, Nak"),
                T("Mother of the Hills", "Ibu Penjaga Bukit"),
            ],
            Catchphrase = T("Who forgot you, little one?", "Siapa yang melupakanmu, Nak?"),
        },
        new()
        {
            Id = "begu-ganjang", Name = T("Begu Ganjang", "Begu Ganjang"),
            Title = T("The Growing Spirit", "Begu Ganjang"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Sumatera Utara",
            Bio = T("A spirit from Batak tales that grows taller every time you look up at it. The longer you stare, the further you fall.",
                    "Roh dari kisah Batak yang tumbuh makin tinggi setiap kali kamu mendongak. Makin lama kamu menatap, makin jauh kamu jatuh."),
            Stats = new(7, 5, 5, 8),
            Body = new(2.40f, 0.8f, 0.9f, 0x1E1E2A, 0x3B3B55, 0x5C5470, LongArms | Glow, 0x6BD0FF),
            Specials = new(T("Skyward Growth", "Tumbuh Menjulang"), T("Looming Gaze", "Tatapan Tinggi"), T("Long Shadow", "Bayang Panjang")),
            FinisherNames =
            [
                T("Don't Look Up", "Jangan Mendongak"), T("Toba Depths", "Kedalaman Toba"), T("Endless Height", "Tinggi Tanpa Batas"),
                T("Stilt Stomp", "Injakan Jangkung"), T("Shadow Toss", "Lempar Bayangan"),
                T("Tor-tor Dance", "Manortor Bersama"), T("View from Above", "Pemandangan dari Atas"),
                T("Shrink Back", "Mengecil Lagi"),
                T("Spirit of the Highlands", "Roh Tanah Tinggi"),
            ],
            Catchphrase = T("Look up. Higher. Higher.", "Lihat ke atas. Lebih tinggi. Lebih tinggi."),
        },
        new()
        {
            Id = "leak-bali", Name = T("Leak", "Leak"),
            Title = T("The Night Sorcerer", "Leak Bali"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Bali",
            Bio = T("By day a quiet villager, by night a sorcerer of the Rangda tradition. Its head glows as it flies over the rice terraces.",
                    "Siang hari warga desa yang pendiam, malam hari penyihir aliran Rangda. Kepalanya bercahaya saat terbang di atas sawah terasering."),
            Stats = new(6, 7, 5, 9),
            Body = new(1.72f, 0.9f, 1.15f, 0x7A1F1F, 0xF3EEE1, 0xC9A27E, Mask | LongHair | Glow, 0xF6C21B),
            Specials = new(T("Leak Fire", "Api Leak"), T("Flying Head", "Kepala Terbang"), T("Desti Curse", "Kutukan Desti")),
            FinisherNames =
            [
                T("Rangda's Call", "Panggilan Rangda"), T("Cemetery Flame", "Api Setra"), T("Night Transformation", "Ngeleak Malam"),
                T("Fireball Rain", "Hujan Bola Api"), T("Mask of Fear", "Topeng Ketakutan"),
                T("Kecak Chorus", "Paduan Kecak"), T("Temple Offering", "Canang Sari"),
                T("Balance Restored", "Rwa Bhineda"),
                T("Sovereign of Shadows", "Penguasa Ilmu Hitam"),
            ],
            Catchphrase = T("The night belongs to me.", "Malam adalah milikku."),
        },

        // ───────────── Satwa Nusantara / Wildlife ─────────────
        new()
        {
            Id = "orang-utan", Name = T("Si Ucok", "Si Ucok"),
            Title = T("The Forest Person", "Orang Utan"),
            Category = CharacterCategory.Wildlife, Archetype = Archetype.Grappler, Origin = "Kalimantan",
            Bio = T("Ucok guards the last stand of old forest with patience and enormous arms. He only fights when the chainsaws come.",
                    "Ucok menjaga hutan tua terakhir dengan kesabaran dan lengan yang sangat besar. Ia hanya bertarung saat gergaji mesin datang."),
            Stats = new(8, 5, 7, 6),
            Body = new(1.55f, 1.3f, 1.05f, 0xB4521E, 0x8A3A12, 0x6E4A33, Fur | LongArms, 0x2E8B57),
            Specials = new(T("Durian Toss", "Lempar Durian"), T("Vine Swing", "Ayunan Liana"), T("Jungle Hug", "Pelukan Rimba")),
            FinisherNames =
            [
                T("Canopy Drop", "Jatuh dari Kanopi"), T("Durian Crown", "Mahkota Durian"), T("Branch Breaker", "Patah Dahan"),
                T("Chest Drum", "Tabuh Dada"), T("Vine Tangle", "Lilitan Akar"),
                T("Fruit Sharing", "Bagi Buah"), T("Nest Building", "Bikin Sarang"),
                T("Back to the Trees", "Kembali ke Pohon"),
                T("Guardian of Borneo", "Penjaga Borneo"),
            ],
            Catchphrase = T("Hoo-hoo. (Leave the trees alone.)", "Huu-huu. (Jangan tebang pohonnya.)"),
        },
        new()
        {
            Id = "bekantan", Name = T("Pak Hidung", "Pak Hidung"),
            Title = T("The Proboscis Monkey", "Bekantan"),
            Category = CharacterCategory.Wildlife, Archetype = Archetype.Beast, Origin = "Banjarmasin",
            Bio = T("The pride of the mangroves, with a nose to match. He leaps between river boats faster than any ferry.",
                    "Kebanggaan hutan bakau dengan hidung yang sepadan. Ia melompat antar perahu sungai lebih cepat dari kapal penyeberangan mana pun."),
            Stats = new(5, 9, 5, 7),
            Body = new(1.45f, 1.05f, 1.0f, 0xC26B2E, 0xE9D7B5, 0xE29A72, Fur | Tail | Snout, 0xE9D7B5),
            Specials = new(T("Mighty Nose", "Hidung Sakti"), T("Mangrove Leap", "Lompatan Bakau"), T("Bekantan Call", "Teriakan Bekantan")),
            FinisherNames =
            [
                T("Nose Dive", "Tukik Hidung"), T("River Market Splash", "Cebur Pasar Terapung"), T("Mangrove Maze", "Labirin Bakau"),
                T("Tail Spin", "Putaran Ekor"), T("Belly Bounce", "Pantulan Perut"),
                T("Boat Ride", "Naik Klotok"), T("Mangrove Picnic", "Piknik Bakau"),
                T("Swim Away", "Berenang Pulang"),
                T("Prince of the Rivers", "Pangeran Seribu Sungai"),
            ],
            Catchphrase = T("Honk!", "Hongk!"),
        },
        new()
        {
            Id = "komodo", Name = T("Ora", "Ora"),
            Title = T("The Last Dragon", "Komodo"),
            Category = CharacterCategory.Wildlife, Archetype = Archetype.Grappler, Origin = "Nusa Tenggara Timur",
            Bio = T("Ora is old, patient and venomous. She can wait for days. Her bite does the rest.",
                    "Ora sudah tua, sabar, dan berbisa. Ia bisa menunggu berhari-hari. Gigitannya mengurus sisanya."),
            Stats = new(8, 5, 8, 5),
            Body = new(1.30f, 1.4f, 1.1f, 0x5C6B4A, 0x3E4733, 0x6E7A55, Tail | Scales | Snout | Quadruped, 0xF6C21B),
            Specials = new(T("Venom Bite", "Gigitan Berbisa"), T("Tail Lash", "Kibasan Ekor"), T("Dragon Pounce", "Terkaman Naga")),
            FinisherNames =
            [
                T("Slow Venom", "Bisa Perlahan"), T("Rinca Feast", "Pesta Rinca"), T("Dragon's Jaw", "Rahang Naga"),
                T("Tail Whip Combo", "Cambuk Ekor Beruntun"), T("Death Roll", "Gulingan Maut"),
                T("Sunbathing", "Berjemur Bareng"), T("Island Tour", "Tur Pulau Komodo"),
                T("Not Worth Eating", "Kurang Gurih"),
                T("Ancient Dragon", "Naga Purba"),
            ],
            Catchphrase = T("Hsssss.", "Hsssss."),
        },
        new()
        {
            Id = "harimau-sumatra", Name = T("Datuk Belang", "Datuk Belang"),
            Title = T("The Sumatran Tiger", "Harimau Sumatra"),
            Category = CharacterCategory.Wildlife, Archetype = Archetype.Beast, Origin = "Sumatera",
            Bio = T("Villagers call him Datuk out of respect. He moves through the tall grass without a sound and strikes before you hear the rustle.",
                    "Warga desa memanggilnya Datuk karena hormat. Ia bergerak di rumput tinggi tanpa suara dan menyerang sebelum kamu mendengar desirnya."),
            Stats = new(8, 9, 5, 7),
            Body = new(1.60f, 1.2f, 1.05f, 0xE0802B, 0x1A1A1A, 0xF0E0C8, Fur | Tail | Snout, 0x1A1A1A),
            Specials = new(T("Striped Claw", "Cakar Loreng"), T("Lightning Pounce", "Terkam Kilat"), T("Sumatran Roar", "Auman Sumatra")),
            FinisherNames =
            [
                T("Tall Grass", "Rumput Tinggi"), T("Kerinci Hunt", "Perburuan Kerinci"), T("Crimson Stripes", "Loreng Merah"),
                T("Claw Frenzy", "Amukan Cakar"), T("Pounce and Pin", "Terkam dan Kunci"),
                T("Big Cat Nap", "Tidur Siang"), T("Play Fight", "Main Gelut"),
                T("Respect Given", "Hormat Diterima"),
                T("Spirit of Sumatra", "Roh Rimba Sumatra"),
            ],
            Catchphrase = T("Grrrr...", "Grrrr..."),
        },
        new()
        {
            Id = "cenderawasih", Name = T("Cenderawasih", "Cenderawasih"),
            Title = T("The Bird of Paradise Spirit", "Roh Cenderawasih"),
            Category = CharacterCategory.Wildlife, Archetype = Archetype.Mystic, Origin = "Papua",
            Bio = T("The spirit of the bird of paradise dances at the edge of the clouds. Its plumes cut like blades and shine like sunrise.",
                    "Roh burung cenderawasih menari di tepi awan. Bulunya setajam pisau dan berkilau seperti matahari terbit."),
            Stats = new(5, 9, 4, 9),
            Body = new(1.55f, 0.85f, 0.95f, 0xF2B705, 0x8C1C13, 0x2E8B57, Wings | Beak | Glow | Floating, 0x6BD0FF),
            Specials = new(T("Plume Dance", "Tarian Bulu"), T("Dawn Wings", "Sayap Fajar"), T("Feather Storm", "Badai Bulu")),
            FinisherNames =
            [
                T("Plume Blades", "Bilah Bulu"), T("Paradise Lost", "Surga yang Hilang"), T("Raja Ampat Dive", "Tukik Raja Ampat"),
                T("Wing Flurry", "Kepakan Bertubi"), T("Sky Spin", "Putaran Langit"),
                T("Courtship Dance", "Tarian Kawin"), T("Morning Song", "Kicau Pagi"),
                T("Fly Free", "Terbang Bebas"),
                T("Herald of Paradise", "Utusan Surga"),
            ],
            Catchphrase = T("Wak-wak-wak!", "Wak-wak-wak!"),
        },

        // ───────────── Karakter Rahasia / Secret Legends ─────────────
        new()
        {
            Id = "nyi-roro-kidul", Name = T("Nyi Roro Kidul", "Nyi Roro Kidul"),
            Title = T("Queen of the Southern Sea", "Ratu Pantai Selatan"),
            Category = CharacterCategory.Secret, Archetype = Archetype.Legend, Origin = "Pantai Selatan",
            Bio = T("The queen of the southern ocean commands the waves themselves. Those who wear green on her beach are said to be called to her palace.",
                    "Ratu samudra selatan memerintah ombak itu sendiri. Konon, siapa yang memakai hijau di pantainya akan dipanggil ke istananya."),
            Stats = new(8, 7, 8, 10),
            Body = new(1.75f, 0.95f, 1.0f, 0x0E7A52, 0xD4AF37, 0xD9A47A, Crown | LongHair | Glow | Cape, 0xD4AF37),
            Specials = new(T("Southern Wave", "Ombak Selatan"), T("Green Sash", "Selendang Hijau"), T("Call of the Sea", "Panggilan Samudra")),
            FinisherNames =
            [
                T("Undertow", "Arus Balik"), T("Palace Beneath", "Istana Bawah Laut"), T("Tidal Judgement", "Pengadilan Pasang"),
                T("Sash Strangle", "Lilitan Selendang"), T("Wave Barrage", "Gempuran Ombak"),
                T("Pearl Gift", "Hadiah Mutiara"), T("Seashell Song", "Lagu Kerang"),
                T("Returned to Shore", "Dikembalikan ke Pantai"),
                T("Sovereign of the Southern Sea", "Penguasa Laut Selatan"),
            ],
            Catchphrase = T("The sea always takes back what is hers.", "Laut selalu mengambil kembali miliknya."),
            IsSecret = true,
            UnlockHint = T("Win the Story Campaign.", "Tamatkan Mode Cerita."),
        },
        new()
        {
            Id = "gatotkaca", Name = T("Gatotkaca", "Gatotkaca"),
            Title = T("Muscles of Wire, Bones of Iron", "Otot Kawat Tulang Besi"),
            Category = CharacterCategory.Secret, Archetype = Archetype.Legend, Origin = "Pringgandani",
            Bio = T("The flying knight of the wayang stage, son of Bima, forged in the cauldron of the gods. He fights from the sky.",
                    "Ksatria terbang dari panggung wayang, putra Bima, ditempa di kawah Candradimuka. Ia bertarung dari langit."),
            Stats = new(9, 8, 9, 7),
            Body = new(1.85f, 1.15f, 1.0f, 0x8C1C13, 0xD4AF37, 0xB07A50, Crown | Cape | Wings, 0xD4AF37),
            Specials = new(T("Brajamusti Fist", "Pukulan Brajamusti"), T("Pringgandani Flight", "Terbang Pringgandani"), T("Antakusuma Shield", "Tameng Antakusuma")),
            FinisherNames =
            [
                T("Candradimuka Forge", "Kawah Candradimuka"), T("Sky Meteor", "Meteor Langit"), T("Iron Bones", "Tulang Besi"),
                T("Wire Muscle Rush", "Otot Kawat Beruntun"), T("Brajamusti Barrage", "Brajamusti Seribu"),
                T("Wayang Bow", "Sembah Wayang"), T("Flight Lesson", "Les Terbang"),
                T("A Knight's Mercy", "Belas Kasih Ksatria"),
                T("Knight of the Sky", "Ksatria Langit"),
            ],
            Catchphrase = T("My muscles are wire, my bones are iron!", "Ototku kawat, tulangku besi!"),
            IsSecret = true,
            UnlockHint = T("Win a Tournament on any difficulty.", "Juarai Mode Turnamen di tingkat kesulitan apa pun."),
        },
        new()
        {
            Id = "si-pitung", Name = T("Si Pitung", "Si Pitung"),
            Title = T("The Robin Hood of Betawi", "Jagoan Betawi"),
            Category = CharacterCategory.Secret, Archetype = Archetype.Legend, Origin = "Rawa Belong, Jakarta",
            Bio = T("The legendary jagoan of Betawi who took from the colonial landlords and gave to the kampung. Bullets, they say, never touched him.",
                    "Jagoan legendaris Betawi yang merampas dari tuan tanah kolonial dan membagikannya ke kampung. Konon peluru tak pernah menyentuhnya."),
            Stats = new(8, 9, 7, 9),
            Body = new(1.73f, 1.0f, 1.0f, 0x111111, 0xE0322B, 0xB07A50, Hat | Weapon, 0xE0322B),
            Specials = new(T("Betawi Golok", "Golok Betawi"), T("Rawa Belong Silat", "Silat Rawa Belong"), T("Bulletproof", "Ilmu Kebal")),
            FinisherNames =
            [
                T("Golok Justice", "Keadilan Golok"), T("Kampung Revenge", "Balasan Kampung"), T("Rooftop Escape", "Kabur Lewat Genteng"),
                T("Sarong Snap", "Lecutan Sarung"), T("Palang Pintu", "Palang Pintu"),
                T("Share the Loot", "Bagi Rata"), T("Ondel-ondel Parade", "Arak Ondel-ondel"),
                T("Go Home to Your Mother", "Pulang Sono ke Emak"),
                T("Legend of Batavia", "Legenda Batavia"),
            ],
            Catchphrase = T("Don't mess with the kampung.", "Jangan macem-macem sama orang kampung."),
            IsSecret = true,
            UnlockHint = T("Reach Survival wave 10.", "Capai gelombang ke-10 di Mode Survival."),
        },
        new()
        {
            Id = "sangkuriang", Name = T("Sangkuriang", "Sangkuriang"),
            Title = T("The Builder of Tangkuban Perahu", "Penendang Perahu"),
            Category = CharacterCategory.Secret, Archetype = Archetype.Legend, Origin = "Bandung",
            Bio = T("Tricked by a false dawn, he kicked his unfinished boat over and it became a mountain. He has been angry ever since.",
                    "Tertipu fajar palsu, ia menendang perahu yang belum selesai hingga menjadi gunung. Sejak itu ia selalu marah."),
            Stats = new(10, 6, 7, 7),
            Body = new(1.90f, 1.25f, 1.0f, 0x5A3A1E, 0x2E8B57, 0xA86A44, LongHair | Weapon, 0xF6C21B),
            Specials = new(T("Boat Kick", "Tendang Perahu"), T("Citarum Dam", "Bendung Citarum"), T("False Dawn", "Fajar Palsu")),
            FinisherNames =
            [
                T("Tangkuban Perahu", "Tangkuban Perahu"), T("One Night Lake", "Danau Semalam"), T("Mountain Maker", "Pencipta Gunung"),
                T("Rage of Dawn", "Amukan Subuh"), T("Hull Crusher", "Remuk Lambung"),
                T("Angklung Ensemble", "Ensambel Angklung"), T("Boat Ride", "Naik Perahu"),
                T("Forgive the Past", "Maafkan Masa Lalu"),
                T("Wrath of Parahyangan", "Murka Parahyangan"),
            ],
            Catchphrase = T("The sun has not risen yet!", "Matahari belum terbit!"),
            IsSecret = true,
            UnlockHint = T("Perform 25 finishers.", "Lakukan 25 finisher."),
        },
        new()
        {
            Id = "jaka-tarub", Name = T("Jaka Tarub", "Jaka Tarub"),
            Title = T("The Celestial Thief", "Pencuri Selendang"),
            Category = CharacterCategory.Secret, Archetype = Archetype.Legend, Origin = "Grobogan",
            Bio = T("He once hid a celestial nymph's shawl by the lake. Now he fights with the borrowed grace of heaven and a lot of regret.",
                    "Ia pernah menyembunyikan selendang bidadari di tepi telaga. Kini ia bertarung dengan keanggunan pinjaman dari kahyangan dan banyak penyesalan."),
            Stats = new(7, 9, 6, 9),
            Body = new(1.76f, 0.95f, 1.0f, 0x2E5C8A, 0xF3EEE1, 0xB98055, Cape | Glow, 0x6BD0FF),
            Specials = new(T("Heavenly Shawl", "Selendang Bidadari"), T("Kahyangan Arrow", "Panah Kahyangan"), T("Nymph Lake", "Telaga Bidadari")),
            FinisherNames =
            [
                T("Stolen Shawl", "Selendang Curian"), T("Seventh Heaven", "Langit Ketujuh"), T("Rice Barn Secret", "Rahasia Lumbung Padi"),
                T("Arrow Volley", "Hujan Panah"), T("Lake Plunge", "Cebur Telaga"),
                T("Return the Shawl", "Kembalikan Selendangnya"), T("Moonlight Bath", "Mandi Cahaya Bulan"),
                T("Heaven Forgives", "Kahyangan Memaafkan"),
                T("Ascension", "Naik ke Kahyangan"),
            ],
            Catchphrase = T("Some things should never be stolen.", "Ada hal yang tak boleh dicuri."),
            IsSecret = true,
            UnlockHint = T("Earn 30 achievements.", "Raih 30 pencapaian."),
        },

        // ───────────── DLC: Horror Nusantara Pack ─────────────
        Dlc("sundel-bolong", "Sundel Bolong", ("The Hollow-Backed Lady", "Sundel Bolong"), CharacterCategory.Mystic, Archetype.Mystic, "Jawa",
            new(7, 7, 4, 8), new(1.72f, 0.8f, 1.0f, 0xF3EEE1, 0x7A1F1F, 0xE8DCCB, LongHair | Floating), DlcPack.HorrorNusantara,
            ("Satay Order", "Pesan Sate"), ("Back Turn", "Balik Badan"), ("Wailing Night", "Ratapan Malam")),
        Dlc("tuyul", "Tuyul", ("The Little Thief", "Pencuri Cilik"), CharacterCategory.Mystic, Archetype.Striker, "Jawa",
            new(3, 10, 3, 9), new(0.95f, 0.8f, 1.3f, 0x7A8A6A, 0x4A5A3A, 0x9AAA8A, None), DlcPack.HorrorNusantara,
            ("Coin Snatch", "Sambar Receh"), ("Wallet Dash", "Lari Bawa Dompet"), ("Crab Distraction", "Umpan Kepiting")),
        Dlc("palasik", "Palasik", ("The Head That Flies", "Kepala Terbang Minang"), CharacterCategory.Mystic, Archetype.Mystic, "Sumatera Barat",
            new(6, 8, 4, 8), new(1.70f, 0.85f, 1.2f, 0x3A2A3A, 0x8C1C13, 0xB07A50, Floating | Glow), DlcPack.HorrorNusantara,
            ("Night Flight", "Terbang Malam"), ("Soul Sip", "Isap Sukma"), ("Minang Curse", "Kutuk Ranah")),

        // ───────────── DLC: Jawara Nusantara Pack ─────────────
        Dlc("pendekar-betawi", "Bang Mamat", ("The Betawi Champion", "Pendekar Betawi"), CharacterCategory.Profession, Archetype.Striker, "Jakarta",
            new(7, 8, 6, 8), new(1.74f, 1.0f, 1.0f, 0x111111, 0xE0322B, 0xB07A50, Hat), DlcPack.JawaraNusantara,
            ("Cingkrik Strike", "Pukulan Cingkrik"), ("Palang Pintu Rush", "Terjang Palang Pintu"), ("Sarong Whip", "Cambuk Sarung")),
        Dlc("jawara-banten", "Ki Jaro", ("The Banten Warrior", "Jawara Banten"), CharacterCategory.Profession, Archetype.Brawler, "Banten",
            new(8, 7, 7, 7), new(1.76f, 1.05f, 1.0f, 0x1A1A1A, 0xF3EEE1, 0xA86A44, Headscarf | Weapon), DlcPack.JawaraNusantara,
            ("Debus Fist", "Tinju Debus"), ("Golok Arc", "Sabetan Golok"), ("Iron Skin", "Kulit Besi")),
        Dlc("pendekar-madura", "Cak Ronggo", ("The Madura Fighter", "Pendekar Madura"), CharacterCategory.Profession, Archetype.Brawler, "Madura",
            new(8, 7, 6, 7), new(1.72f, 1.05f, 1.0f, 0xE0322B, 0xF3EEE1, 0xA86A44, Weapon), DlcPack.JawaraNusantara,
            ("Celurit Crescent", "Bulan Sabit Celurit"), ("Karapan Charge", "Tanduk Karapan"), ("Salt Wind", "Angin Garam")),

        // ───────────── DLC: Future Indonesia Pack ─────────────
        Dlc("cyborg-ojol", "Mas Bimo", ("The Cyborg Rider", "Cyborg Ojol"), CharacterCategory.Profession, Archetype.Striker, "Jakarta 2077",
            new(7, 9, 6, 7), new(1.75f, 1.0f, 1.0f, 0x00A651, 0x1A1A1A, 0x8A9AAA, Helmet | Glow), DlcPack.FutureIndonesia,
            ("Order Accepted", "Order Diterima"), ("Shortcut Boost", "Lewat Jalan Tikus"), ("Five-Star Rating", "Bintang Lima")),
        Dlc("ai-dukun", "Mbah Algo", ("The AI Shaman", "AI Dukun"), CharacterCategory.Mystic, Archetype.Mystic, "Cloud Server Bantul",
            new(5, 6, 6, 10), new(1.70f, 0.9f, 1.05f, 0x2A2A44, 0x6BD0FF, 0x7A8AA0, Glow | Glasses), DlcPack.FutureIndonesia,
            ("Neural Mantra", "Mantra Neural"), ("Prediction Strike", "Serangan Prediksi"), ("Hallucination", "Halusinasi")),
        Dlc("polisi-drone", "Unit P-62", ("The Drone Officer", "Polisi Drone"), CharacterCategory.Profession, Archetype.Brawler, "Jakarta 2077",
            new(6, 7, 8, 7), new(1.80f, 1.05f, 0.95f, 0x2C3E66, 0xF6C21B, 0x9AA5B5, Helmet | Uniform | Glow), DlcPack.FutureIndonesia,
            ("Ticket Laser", "Laser Tilang"), ("Siren Dash", "Sirene Melaju"), ("Drone Swarm", "Kawanan Drone")),

        // ───────────── Warga & Satire / Society (roster expansion) ─────────────
        new()
        {
            Id = "pejabat-korup", Name = T("Pak Bambang", "Pak Bambang"),
            Title = T("The Corrupt Official", "Pejabat Korup"),
            Category = CharacterCategory.Society, Archetype = Archetype.Grappler, Origin = "Ibu Kota",
            Bio = T("Bambang has never signed anything without an envelope under it. He fights with a briefcase full of money and a lawyer on speed dial. Any resemblance to a real person is purely coincidental.",
                    "Bambang tak pernah menandatangani apa pun tanpa amplop di bawahnya. Ia bertarung dengan koper penuh uang dan pengacara di panggilan cepat. Kesamaan dengan tokoh nyata hanyalah kebetulan."),
            Stats = new(7, 3, 9, 6),
            Body = new(1.68f, 1.4f, 1.05f, 0x8C5A2B, 0x1A1A1A, 0xC48D62, Hat, 0xD4AF37),
            Specials = new(T("Envelope Toss", "Lempar Amplop"), T("Briefcase Bash", "Hantam Koper"), T("Cash Shield", "Tameng Uang Pelicin")),
            FinisherNames =
            [
                T("Budget Markup", "Mark-up Anggaran"), T("Tender Rigged", "Tender Diatur"), T("Golden Handshake", "Salam Tempel"),
                T("Briefcase Barrage", "Koper Bertubi"), T("Paperwork Avalanche", "Longsoran Berkas"),
                T("Free Infrastructure", "Peresmian Jembatan"), T("Campaign Gift", "Bagi Sembako"),
                T("Settled Out of Court", "Damai di Tempat"),
                T("Untouchable", "Kebal Hukum"),
            ],
            Catchphrase = T("I don't recall.", "Saya lupa, Yang Mulia."),
        },
        new()
        {
            Id = "anggota-dpr", Name = T("Ibu Dewi", "Ibu Dewi"),
            Title = T("The Member of Parliament", "Anggota DPR"),
            Category = CharacterCategory.Society, Archetype = Archetype.Mystic, Origin = "Senayan",
            Bio = T("Dewi naps through the morning session and wakes up just in time for the vote. Her speeches are so long they count as a ranged attack. A fictional legislator.",
                    "Dewi tidur sepanjang sidang pagi dan bangun tepat saat pemungutan suara. Pidatonya begitu panjang sampai terhitung serangan jarak jauh. Tokoh fiksi."),
            Stats = new(5, 5, 7, 8),
            Body = new(1.64f, 1.1f, 1.0f, 0x1E2A4A, 0xE0322B, 0xD49A6E, Headscarf | Uniform, 0xD4AF37),
            Specials = new(T("Endless Speech", "Pidato Tanpa Ujung"), T("Walk Out", "Walk Out Sidang"), T("Point of Order", "Interupsi!")),
            FinisherNames =
            [
                T("Bill Passed Overnight", "RUU Kilat Semalam"), T("Recess Forever", "Reses Abadi"), T("Gavel Drop", "Ketuk Palu"),
                T("Microphone Feedback", "Mik Berdenging"), T("Committee Pile-on", "Rapat Pansus"),
                T("Comparative Study Trip", "Studi Banding"), T("Photo Op", "Sesi Foto Bersama"),
                T("Aspirations Received", "Aspirasi Ditampung"),
                T("Voice of the People", "Suara Rakyat Sejati"),
            ],
            Catchphrase = T("Your aspirations have been noted.", "Aspirasi Anda kami tampung."),
        },
        new()
        {
            Id = "buzzer-politik", Name = T("Bang Viral", "Bang Viral"),
            Title = T("The Political Buzzer", "Buzzer Politik"),
            Category = CharacterCategory.Society, Archetype = Archetype.Striker, Origin = "Grup WhatsApp",
            Bio = T("Viral runs four hundred accounts from one bedroom. He can trend any hashtag before breakfast, and he will tag you in all of them.",
                    "Viral mengelola empat ratus akun dari satu kamar. Ia bisa membuat tagar apa pun trending sebelum sarapan, dan ia akan menandaimu di semuanya."),
            Stats = new(4, 9, 4, 9),
            Body = new(1.70f, 0.95f, 1.0f, 0x2B2B2B, 0x6BD0FF, 0xB98055, Glasses, 0x6BD0FF),
            Specials = new(T("Hashtag Storm", "Badai Tagar"), T("Bot Swarm Dash", "Serbuan Akun Bot"), T("Cancel Culture", "Ramai-ramai Hujat")),
            FinisherNames =
            [
                T("Doxxed", "Dibongkar Datanya"), T("Trending Topic", "Trending Satu"), T("Ratio'd", "Kalah Ratio"),
                T("Spam Barrage", "Spam Bertubi"), T("Fake News", "Hoaks Berantai"),
                T("Follow Back", "Folbek Dong"), T("Giveaway", "Giveaway Pulsa"),
                T("Account Suspended", "Akun Ditangguhkan"),
                T("King of the Timeline", "Raja Linimasa"),
            ],
            Catchphrase = T("Retweet if you agree.", "RT kalau setuju."),
        },
        new()
        {
            Id = "influencer", Name = T("Kak Cleo", "Kak Cleo"),
            Title = T("The Influencer", "Influencer"),
            Category = CharacterCategory.Society, Archetype = Archetype.Striker, Origin = "Jakarta Selatan",
            Bio = T("Cleo has two million followers and a ring light on her back at all times. Every fight is content, and every combo is a reel.",
                    "Cleo punya dua juta pengikut dan selalu membawa ring light di punggungnya. Setiap pertarungan adalah konten, setiap kombo adalah reel."),
            Stats = new(4, 9, 5, 8),
            Body = new(1.63f, 0.85f, 1.0f, 0xF2B5D4, 0xF3EEE1, 0xD9A47A, LongHair | Glow, 0xF2B5D4),
            Specials = new(T("Flash Selfie", "Selfie Silau"), T("Collab Dash", "Kolab Dadakan"), T("Live Stream", "Siaran Langsung")),
            FinisherNames =
            [
                T("Unfollowed", "Di-unfollow"), T("Viral for the Wrong Reasons", "Viral Salah Kaprah"), T("Ring Light Overload", "Ring Light Maksimal"),
                T("Selfie Stick Combo", "Kombo Tongsis"), T("Endorsement Deal", "Paid Promote"),
                T("Shout Out", "Shout Out Gratis"), T("Dance Challenge", "Tantangan Joget"),
                T("Story Deleted", "Story Dihapus"),
                T("Blue Tick Legend", "Legenda Centang Biru"),
            ],
            Catchphrase = T("Don't forget to like and subscribe!", "Jangan lupa like dan subscribe, ya!"),
        },
        new()
        {
            Id = "anak-stm", Name = T("Rizky", "Rizky"),
            Title = T("The Vocational Student", "Anak STM"),
            Category = CharacterCategory.Society, Archetype = Archetype.Brawler, Origin = "Bekasi",
            Bio = T("Rizky can rebuild a motorbike engine with his eyes closed. He shows up to every protest first and leaves last, with his school tie as a headband.",
                    "Rizky bisa merakit mesin motor sambil merem. Ia selalu datang paling awal ke setiap demo dan pulang paling akhir, dengan dasi sekolah sebagai ikat kepala."),
            Stats = new(6, 8, 5, 6),
            Body = new(1.68f, 0.95f, 1.0f, 0xF3EEE1, 0x6E7680, 0xB07A50, None, 0xE0322B),
            Specials = new(T("Wrench Throw", "Lempar Kunci Inggris"), T("Belt Whip", "Sabetan Ikat Pinggang"), T("Engine Rev", "Gas Pol")),
            FinisherNames =
            [
                T("Tune Up", "Servis Total"), T("Open Exhaust", "Knalpot Brong"), T("Final Exam", "Ujian Praktik"),
                T("Wrench Combo", "Kombo Kunci Pas"), T("School Brawl", "Tawuran Pulang Sekolah"),
                T("Fix Your Bike", "Benerin Motor Gratis"), T("Group Photo", "Foto Angkatan"),
                T("Truce", "Damai, Bro"),
                T("Pride of the Workshop", "Kebanggaan Bengkel"),
            ],
            Catchphrase = T("Let's go, the workshop is calling!", "Gas, bengkel memanggil!"),
        },
        new()
        {
            Id = "mahasiswi-demo", Name = T("Nadia", "Nadia"),
            Title = T("The Student Protester", "Mahasiswi Demo"),
            Category = CharacterCategory.Society, Archetype = Archetype.Mystic, Origin = "Depok",
            Bio = T("Nadia leads the chants with a megaphone and the facts with footnotes. She has read every clause of the bill you are trying to pass.",
                    "Nadia memimpin yel-yel dengan toa dan argumen dengan catatan kaki. Ia sudah membaca setiap pasal RUU yang sedang kamu kebut."),
            Stats = new(5, 7, 5, 9),
            Body = new(1.62f, 0.85f, 1.0f, 0xF6C21B, 0x1F4FB0, 0xD49A6E, Headscarf, 0xF6C21B),
            Specials = new(T("Megaphone Blast", "Teriakan Toa"), T("Long March", "Long March"), T("Banner Sweep", "Sapuan Spanduk")),
            FinisherNames =
            [
                T("Judicial Review", "Uji Materi"), T("Vote of No Confidence", "Mosi Tidak Percaya"), T("Thesis Defence", "Sidang Skripsi"),
                T("Chant Barrage", "Yel-yel Bertubi"), T("Sign Smash", "Hantam Poster"),
                T("Free Tutoring", "Les Gratis"), T("Campus Concert", "Pentas Kampus"),
                T("Peaceful Dialogue", "Dialog Damai"),
                T("Voice of the Generation", "Suara Satu Generasi"),
            ],
            Catchphrase = T("Read the bill first!", "Baca dulu RUU-nya!"),
        },
        new()
        {
            Id = "kang-ojol", Name = T("Kang Asep", "Kang Asep"),
            Title = T("The Ride-Hailing Driver", "Kang Ojol"),
            Category = CharacterCategory.Society, Archetype = Archetype.Striker, Origin = "Bandung",
            Bio = T("Asep knows every shortcut in the city and has a five-star rating to protect. Rain, traffic or a fighting tournament, the order gets delivered.",
                    "Asep hafal semua jalan tikus di kota dan punya rating bintang lima yang harus dijaga. Hujan, macet, atau turnamen tarung, pesanan tetap sampai."),
            Stats = new(6, 9, 5, 6),
            Body = new(1.70f, 1.0f, 1.0f, 0x00A651, 0x1A1A1A, 0xB07A50, Helmet, 0x00A651),
            Specials = new(T("Helmet Toss", "Lempar Helm"), T("Shortcut Rush", "Lewat Jalan Tikus"), T("Order Cancelled", "Orderan Dibatalkan")),
            FinisherNames =
            [
                T("One-Star Review", "Rating Bintang Satu"), T("Wrong Pin", "Titik Jemput Salah"), T("Rush Hour", "Jam Sibuk"),
                T("Helmet Combo", "Kombo Helm"), T("Exhaust Burn", "Panas Knalpot"),
                T("Free Delivery", "Gratis Ongkir"), T("Ride Home", "Antar Pulang"),
                T("Tip Included", "Ini Tipnya, Kang"),
                T("Legend of the Road", "Legenda Jalanan"),
            ],
            Catchphrase = T("Is this the right pickup point?", "Sesuai titik ya, Kak?"),
        },
        new()
        {
            Id = "tukang-debus", Name = T("Ki Sarmin", "Ki Sarmin"),
            Title = T("The Debus Performer", "Tukang Debus"),
            Category = CharacterCategory.Society, Archetype = Archetype.Grappler, Origin = "Banten",
            Bio = T("Sarmin walks on glass, chews on fire and shrugs off machete blows in the Banten debus tradition. Hitting him mostly hurts your hand.",
                    "Sarmin berjalan di atas kaca, mengunyah api, dan menahan bacokan golok dalam tradisi debus Banten. Memukulnya kebanyakan cuma menyakiti tanganmu."),
            Stats = new(7, 4, 10, 5),
            Body = new(1.74f, 1.1f, 1.0f, 0x1A1A1A, 0xE0322B, 0xA86A44, Headscarf | Weapon, 0xE0322B),
            Specials = new(T("Fire Breath", "Semburan Api"), T("Glass Walk", "Jalan di Atas Beling"), T("Iron Skin", "Kulit Kebal")),
            FinisherNames =
            [
                T("Bed of Nails", "Kasur Paku"), T("Fire Eater", "Pemakan Api"), T("Golok Test", "Uji Golok"),
                T("Glass Shower", "Hujan Beling"), T("Iron Headbutt", "Sundulan Besi"),
                T("Village Show", "Pertunjukan Kampung"), T("Teach the Kids", "Ajari Anak-anak"),
                T("Not Today", "Lain Kali Saja"),
                T("Unbreakable Banten", "Banten Tak Terkalahkan"),
            ],
            Catchphrase = T("Go on, hit me.", "Ayo, bacok saja."),
        },
        new()
        {
            Id = "anak-punk", Name = T("Jerry", "Jerry"),
            Title = T("The Street Punk", "Anak Punk Jalanan"),
            Category = CharacterCategory.Society, Archetype = Archetype.Striker, Origin = "Bandung",
            Bio = T("Jerry plays three chords on a broken ukulele and shares his last rice with anyone. His mohawk is held up by sugar water and pure attitude.",
                    "Jerry memainkan tiga kunci di ukulele rusak dan membagi nasi terakhirnya dengan siapa saja. Mohawk-nya berdiri berkat air gula dan sikap."),
            Stats = new(6, 8, 4, 7),
            Body = new(1.70f, 0.9f, 1.0f, 0x1A1A1A, 0xE0322B, 0xB98055, Weapon, 0xE0322B),
            Specials = new(T("Ukulele Smash", "Hantam Ukulele"), T("Stage Dive", "Lompat ke Penonton"), T("Mosh Pit", "Pogo Massal")),
            FinisherNames =
            [
                T("Anarchy in the Kampung", "Anarki di Kampung"), T("Safety Pin", "Peniti Maut"), T("Feedback Loop", "Distorsi Penuh"),
                T("Boot Stomp", "Injakan Sepatu Lars"), T("Circle Pit", "Pogo Melingkar"),
                T("Shared Rice", "Makan Bareng"), T("Street Gig", "Konser Jalanan"),
                T("Peace, Bro", "Damai Itu Indah"),
                T("Punk Never Dies", "Punk Tak Pernah Mati"),
            ],
            Catchphrase = T("Oi! Oi! Oi!", "Oi! Oi! Oi!"),
        },
        new()
        {
            Id = "pemadam-karhutla", Name = T("Bang Damar", "Bang Damar"),
            Title = T("The Forest Firefighter", "Pemadam Karhutla"),
            Category = CharacterCategory.Society, Archetype = Archetype.Brawler, Origin = "Riau",
            Bio = T("Damar has spent every dry season inside the smoke of the peat fires. He fights with a high-pressure hose and the patience of someone who has seen the forest burn.",
                    "Damar menghabiskan setiap musim kemarau di tengah asap kebakaran gambut. Ia bertarung dengan selang bertekanan tinggi dan kesabaran orang yang pernah melihat hutan terbakar."),
            Stats = new(7, 5, 8, 6),
            Body = new(1.76f, 1.15f, 1.0f, 0xF28C1B, 0xF6C21B, 0xA86A44, Helmet | Uniform, 0xF3EEE1),
            Specials = new(T("Water Jet", "Semprotan Air"), T("Firebreak Charge", "Sekat Bakar"), T("Water Bomb", "Bom Air")),
            FinisherNames =
            [
                T("Smoke Out", "Kabut Asap"), T("Peat Pit", "Lubang Gambut"), T("Pressure Max", "Tekanan Maksimal"),
                T("Hose Whip", "Cambuk Selang"), T("Axe Combo", "Kombo Kapak"),
                T("Plant a Tree", "Tanam Pohon"), T("Cool Down", "Pendinginan"),
                T("Rescued", "Diselamatkan"),
                T("Guardian of the Peatlands", "Penjaga Lahan Gambut"),
            ],
            Catchphrase = T("Nobody burns my forest.", "Tak ada yang boleh membakar hutanku."),
        },
        new()
        {
            Id = "horeg-dancer", Name = T("Mbak Lala", "Mbak Lala"),
            Title = T("The Sound Horeg Dancer", "Penari Horeg"),
            Category = CharacterCategory.Society, Archetype = Archetype.Striker, Origin = "Malang",
            Bio = T("Lala dances in front of a sound-system wall taller than a house. The bass shakes roof tiles loose for three villages; she does not even blink.",
                    "Lala berjoget di depan tembok sound system yang lebih tinggi dari rumah. Bass-nya merontokkan genteng tiga desa; ia bahkan tidak berkedip."),
            Stats = new(5, 9, 4, 8),
            Body = new(1.62f, 0.85f, 1.0f, 0x7CFC00, 0xFF3EA5, 0xD49A6E, Hat | Glow, 0xFF3EA5),
            Specials = new(T("Bass Drop", "Bass Drop"), T("Horeg Shuffle", "Goyang Horeg"), T("Subwoofer Slam", "Hantaman Subwoofer")),
            FinisherNames =
            [
                T("Roof Tiles Fall", "Genteng Rontok"), T("Ten Thousand Watts", "Sepuluh Ribu Watt"), T("Speaker Wall", "Tembok Speaker"),
                T("Bass Combo", "Kombo Bass"), T("Shake Shake", "Goyang Terus"),
                T("Village Party", "Pesta Rakyat"), T("Koplo Dance-off", "Adu Joget Koplo"),
                T("Volume Down", "Kecilin Volumenya"),
                T("Queen of the Carnival", "Ratu Karnaval"),
            ],
            Catchphrase = T("Turn it up!", "Gaskeun, pol!"),
        },

        // ───────────── Makhluk Mistis (roster expansion) ─────────────
        new()
        {
            Id = "kuyang", Name = T("Kuyang", "Kuyang"),
            Title = T("The Flying Head", "Kuyang"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Kalimantan",
            Bio = T("At night her head leaves her body and flies over the river villages, trailing a red glow. Lock your windows.",
                    "Pada malam hari kepalanya meninggalkan tubuh dan terbang di atas kampung tepi sungai dengan cahaya merah. Kunci jendelamu."),
            Stats = new(6, 8, 3, 9),
            Body = new(1.60f, 0.7f, 1.3f, 0x7A1F1F, 0x2A1A1A, 0xE8DCCB, LongHair | Floating | Glow, 0xE0322B),
            Specials = new(T("Crimson Trail", "Jejak Merah"), T("Head Dive", "Tukikan Kepala"), T("Night Hunger", "Lapar Malam")),
            FinisherNames =
            [
                T("Window Left Open", "Jendela Terbuka"), T("River Shadow", "Bayang Sungai"), T("Red Lantern", "Lentera Merah"),
                T("Hair Lash", "Cambuk Rambut"), T("Spiral Dive", "Tukikan Spiral"),
                T("Back to the Body", "Kembali ke Tubuh"), T("Moon Dance", "Tarian Bulan"),
                T("Spared at Dawn", "Diampuni Fajar"),
                T("Terror of Borneo", "Teror Borneo"),
            ],
            Catchphrase = T("Did you lock the window?", "Jendelanya sudah dikunci?"),
        },
        new()
        {
            Id = "baphomet", Name = T("Baphomet", "Baphomet"),
            Title = T("The Horned Outsider", "Iblis Bertanduk"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Legend, Origin = "Dunia Seberang",
            Bio = T("A horned outsider from a foreign legend, drawn by the tournament's thinning veil. The local spirits do not like it, and neither does the crowd.",
                    "Makhluk bertanduk dari legenda negeri seberang yang tertarik oleh tabir yang menipis. Para roh lokal tak menyukainya, begitu juga penonton."),
            Stats = new(9, 5, 7, 7),
            Body = new(2.10f, 1.3f, 1.1f, 0x2A2A2A, 0x5A3A1E, 0x4A3A3A, Horns | Wings | Fur | Glow, 0xF2B705),
            Specials = new(T("Amber Hex", "Kutuk Kuning"), T("Horn Charge", "Seruduk Tanduk"), T("Wing Gust", "Kepakan Sayap")),
            FinisherNames =
            [
                T("Goat's Judgement", "Penghakiman Kambing"), T("Eclipse", "Gerhana"), T("Horned Crown", "Mahkota Tanduk"),
                T("Hoof Stomp", "Injakan Kuku"), T("Wing Flurry", "Kepakan Bertubi"),
                T("Petting Zoo", "Kebun Binatang Mini"), T("Grass Snack", "Makan Rumput Bareng"),
                T("Banished Home", "Diusir Pulang"),
                T("Lord of the Outer Dark", "Penguasa Kegelapan Seberang"),
            ],
            Catchphrase = T("Mbeeek.", "Mbeeek."),
        },
        new()
        {
            Id = "ogoh-ogoh", Name = T("Ogoh-ogoh", "Ogoh-ogoh"),
            Title = T("The Parade Giant", "Ogoh-ogoh"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Grappler, Origin = "Bali",
            Bio = T("A bamboo-and-paper demon built for the night before Nyepi, meant to carry away bad spirits and be burned. This one decided not to be burned.",
                    "Raksasa bambu dan kertas yang dibuat untuk malam pengerupukan sebelum Nyepi, untuk membawa pergi roh jahat lalu dibakar. Yang satu ini memutuskan tidak mau dibakar."),
            Stats = new(9, 3, 8, 5),
            Body = new(2.30f, 1.5f, 1.25f, 0xC0392B, 0xF3EEE1, 0x8E44AD, Horns | LongHair | Fur, 0xF6C21B),
            Specials = new(T("Bamboo Slam", "Hantaman Bambu"), T("Parade Stomp", "Hentakan Arak-arakan"), T("Baleganjur Roar", "Raungan Baleganjur")),
            FinisherNames =
            [
                T("Pengerupukan", "Pengerupukan"), T("Paper Mache Crush", "Remuk Kertas"), T("Spin of the Bearers", "Putaran Pengusung"),
                T("Fang Bite", "Gigitan Taring"), T("Bamboo Frame", "Rangka Bambu"),
                T("Silent Day", "Hari Hening"), T("Parade Dance", "Joget Pawai"),
                T("Spared the Fire", "Batal Dibakar"),
                T("Demon of the Crossroads", "Bhuta Kala Perempatan"),
            ],
            Catchphrase = T("RAAAWR! (baleganjur intensifies)", "RAAAWR! (baleganjur makin kencang)"),
        },
        new()
        {
            Id = "reog-ponorogo", Name = T("Singo Barong", "Singo Barong"),
            Title = T("The Reog Ponorogo Dancer", "Reog Ponorogo"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Beast, Origin = "Ponorogo",
            Bio = T("The dancer carries a tiger mask and a peacock fan weighing as much as a person, using only his teeth. In the arena that jaw is a weapon.",
                    "Sang penari mengangkat topeng harimau dan kipas merak seberat manusia hanya dengan giginya. Di arena, rahang itu adalah senjata."),
            Stats = new(8, 6, 7, 7),
            Body = new(1.95f, 1.3f, 1.35f, 0xB22222, 0x1A1A1A, 0xA86A44, Fur | Mask | Crown, 0x2E8B57),
            Specials = new(T("Peacock Fan", "Kibasan Merak"), T("Barong Charge", "Terjangan Barong"), T("Warok Stomp", "Hentakan Warok")),
            FinisherNames =
            [
                T("Jaw of Iron", "Rahang Besi"), T("Peacock Storm", "Badai Bulu Merak"), T("Ponorogo Night", "Malam Grebeg Suro"),
                T("Whip of the Warok", "Cambuk Warok"), T("Mask Smash", "Hantaman Topeng"),
                T("Jathilan Dance", "Tari Jathilan"), T("Festival Parade", "Kirab Budaya"),
                T("Bow of the Warok", "Hormat Sang Warok"),
                T("King of the Reog", "Raja Reog"),
            ],
            Catchphrase = T("Hok-ya! Hok-ya!", "Hok-ya! Hok-ya!"),
        },
        new()
        {
            Id = "dukun-santet", Name = T("Mbah Kerto", "Mbah Kerto"),
            Title = T("The Black Magic Shaman", "Dukun Santet"),
            Category = CharacterCategory.Mystic, Archetype = Archetype.Mystic, Origin = "Banyuwangi",
            Bio = T("Kerto's clients come at midnight with a photo and a grudge. He fights with straw dolls, rusty nails and a kris that hums when someone lies.",
                    "Klien Kerto datang tengah malam membawa foto dan dendam. Ia bertarung dengan boneka jerami, paku berkarat, dan keris yang berdengung saat ada yang berbohong."),
            Stats = new(5, 5, 6, 10),
            Body = new(1.62f, 0.9f, 1.0f, 0x1A1A1A, 0x5A3A1E, 0xA86A44, Hat | Weapon | Glow, 0x7A1F1F),
            Specials = new(T("Rusty Nail Hex", "Santet Paku"), T("Straw Doll", "Boneka Jerami"), T("Kris Curse", "Kutuk Keris")),
            FinisherNames =
            [
                T("Midnight Package", "Kiriman Tengah Malam"), T("Pin Cushion", "Penuh Jarum"), T("Seventh Night", "Malam Ketujuh"),
                T("Kris Flurry", "Tusukan Keris"), T("Incense Choke", "Asap Kemenyan"),
                T("Lucky Charm", "Jimat Keberuntungan"), T("Love Potion", "Pelet Persahabatan"),
                T("Curse Lifted", "Santet Dicabut"),
                T("Grand Master of the Dark Arts", "Mbah Segala Ilmu"),
            ],
            Catchphrase = T("Bring me a photo.", "Bawa fotonya ke sini."),
        },
    ];


    private static CharacterDefinition Dlc(
        string id, string name, (string En, string Id) title, CharacterCategory category, Archetype archetype, string origin,
        CharacterStats stats, BodyShape body, DlcPack pack,
        (string En, string Id) neutral, (string En, string Id) forward, (string En, string Id) down) => new()
    {
        Id = id, Name = T(name, name), Title = title, Category = category, Archetype = archetype, Origin = origin,
        Bio = T($"{name} joins Tarkam Arena in a downloadable content pack.", $"{name} hadir di Tarkam Arena lewat paket konten tambahan."),
        Stats = stats, Body = body, Dlc = pack,
        Specials = new(neutral, forward, down),
        FinisherNames = s_dlcFinisherTemplates,
    };
}
