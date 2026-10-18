using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Combat;

/// <summary>What a signature move does in the simulation.</summary>
public enum SignatureKind : byte
{
    /// <summary>Summons helpers (spirits, animals, nymphs) that fly to the target and attack several times.</summary>
    Summon,
    /// <summary>A crowd charges across the arena: protesters, bikers, bodyguards, monkeys, a sound truck.</summary>
    Crowd,
    /// <summary>A giant signature weapon swung in front of the fighter: pacul, golok, ruler, oar, briefcase.</summary>
    Weapon,
    /// <summary>Weather and nature strike from the sky at the opponent: lightning, fire rain, water bombs, gavels.</summary>
    Storm,
    /// <summary>The opponent is lifted into the air, held, and slammed into the ground.</summary>
    Levitate,
    /// <summary>A whirlwind that travels forward, pulling in and juggling whoever it catches.</summary>
    Tornado,
    /// <summary>A volley of thrown projectiles: meatballs, chalk, durians, envelopes.</summary>
    Barrage,
    /// <summary>A shockwave that runs along the ground in both directions.</summary>
    Quake,
    /// <summary>A wall that sweeps forward: a tidal wave, a sound wave, a flood of broth.</summary>
    Wave,
}

/// <summary>The prop the presentation builds for a signature move.</summary>
public enum SignatureProp : byte
{
    Orb,
    Ghost,
    Head,
    Person,
    Beast,
    Bird,
    Bike,
    Truck,
    Cart,
    Pole,
    Blade,
    Paddle,
    Slab,
    Hand,
    Card,
    Ball,
    Bolt,
    Fireball,
    Water,
    Sound,
    Leaf,
    Arrow,
    Hoe,
}

/// <summary>The particle flavour that rides along with a signature move.</summary>
public enum SignatureFlavor : byte
{
    Sparks,
    Fire,
    Water,
    Leaves,
    Feathers,
    Smoke,
    Ghostly,
    Electric,
    Paper,
    Notes,
    Dust,
    Gold,
}

/// <summary>Look of a signature move: the prop, two colours and a particle flavour.</summary>
public readonly record struct SignatureTheme(SignatureProp Prop, uint Primary, uint Secondary, SignatureFlavor Flavor);

/// <summary>One signature move ("Jurus Pamungkas") of a fighter.</summary>
public sealed record SignatureMove(int Slot, SignatureKind Kind, SignatureTheme Theme, LocalizedText Name, LocalizedText Description)
{
    /// <summary>Jurus meter cost per slot (the meter holds 1000).</summary>
    public static readonly int[] Costs = [250, 400, 700];

    /// <summary>Input per slot, facing-relative: quarter circle forward, quarter circle back, down-down; each + Special.</summary>
    public static readonly string[] InputGlyphs = ["↓ ↘ → + S", "↓ ↙ ← + S", "↓ ↓ + S"];

    public int Cost => Costs[Slot];
    public string Input => InputGlyphs[Slot];
}

/// <summary>
/// Every fighter's three signature moves. Hand-authored for the main roster; DLC and creator
/// fighters get a set derived from their fighting style.
/// </summary>
public static class SignatureCatalog
{
    private static LocalizedText T(string en, string id) => new(en, id);

    private static SignatureTheme Th(SignatureProp prop, uint a, uint b, SignatureFlavor f) => new(prop, a, b, f);

    private static (SignatureKind, SignatureTheme, LocalizedText, LocalizedText) S(SignatureKind kind, SignatureTheme theme, string en, string id, string descEn, string descId) =>
        (kind, theme, T(en, id), T(descEn, descId));

    private static readonly Dictionary<string, (SignatureKind Kind, SignatureTheme Theme, LocalizedText Name, LocalizedText Desc)[]> s_table = new(StringComparer.Ordinal)
    {
        ["gembel-lampu-merah"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0xB0B0B8, 0xE0322B, SignatureFlavor.Sparks), "Tin Can Rain", "Hujan Kaleng", "A volley of dented cans and bottle caps.", "Rentetan kaleng penyok dan tutup botol."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x6B5B45, 0xF6C21B, SignatureFlavor.Notes), "Buskers' Charge", "Serbuan Pengamen", "Every busker at the crossroads storms in with ukuleles.", "Semua pengamen perempatan menyerbu sambil membawa ukulele."),
            S(SignatureKind.Weapon, Th(SignatureProp.Pole, 0x2A2A2A, 0xE0322B, SignatureFlavor.Electric), "Traffic Light Slam", "Tiang Lampu Merah", "Rips out the traffic light and swings it, lights still blinking.", "Mencabut tiang lampu merah lalu mengayunkannya, lampunya masih berkedip."),
        ],
        ["kasir-minimarket"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Card, 0xF3EEE1, 0x1F4FB0, SignatureFlavor.Paper), "Receipt Storm", "Hujan Struk", "Razor-sharp receipts fired from the till.", "Struk setajam silet ditembakkan dari mesin kasir."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x1F4FB0, 0xE0322B, SignatureFlavor.Paper), "Promo Stampede", "Serbuan Promo", "Shoppers chasing a two-for-one deal trample everything.", "Pembeli pemburu promo beli satu gratis satu menerjang semuanya."),
            S(SignatureKind.Weapon, Th(SignatureProp.Cart, 0xC0C0C8, 0xE0322B, SignatureFlavor.Sparks), "Trolley of Doom", "Troli Maut", "A fully loaded shopping trolley, swung like a hammer.", "Troli belanja penuh muatan, diayun seperti palu."),
        ],
        ["guru-matematika"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0xF3EEE1, 0xB0E0FF, SignatureFlavor.Dust), "Chalk Machine Gun", "Kapur Beruntun", "Thirty years of aim, all at once.", "Tiga puluh tahun latihan membidik, sekaligus."),
            S(SignatureKind.Storm, Th(SignatureProp.Slab, 0x2E4A3A, 0xF3EEE1, SignatureFlavor.Dust), "Falling Formulas", "Badai Rumus", "Blackboards full of equations rain from the sky.", "Papan tulis penuh rumus berjatuhan dari langit."),
            S(SignatureKind.Weapon, Th(SignatureProp.Paddle, 0xC9A27E, 0x5A3A1E, SignatureFlavor.Dust), "Giant Ruler", "Penggaris Raksasa", "A one-metre ruler, enlarged to five.", "Penggaris satu meter, diperbesar jadi lima."),
        ],
        ["pesilat"] =
        [
            S(SignatureKind.Tornado, Th(SignatureProp.Leaf, 0xF3EEE1, 0xB8862B, SignatureFlavor.Leaves), "Silat Whirlwind", "Pusaran Angin Silat", "A spinning kick that becomes a typhoon.", "Tendangan berputar yang menjadi angin topan."),
            S(SignatureKind.Summon, Th(SignatureProp.Beast, 0xF28C1B, 0xFFF0A0, SignatureFlavor.Gold), "Spirit Tiger", "Harimau Gaib", "The tiger of his school's legend leaps out to strike.", "Harimau legenda perguruannya melompat keluar menyerang."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xB8862B, 0xFFF0A0, SignatureFlavor.Gold), "Inner Power", "Tenaga Dalam", "Lifts the opponent with pure inner power and drops them.", "Mengangkat lawan dengan tenaga dalam lalu menjatuhkannya."),
        ],
        ["preman-pasar"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x1A1A1A, 0xE0322B, SignatureFlavor.Dust), "Market Gang", "Anak Buah Pasar", "Codet whistles and the whole gang comes running.", "Codet bersiul dan seluruh anak buahnya berlarian datang."),
            S(SignatureKind.Weapon, Th(SignatureProp.Pole, 0xC9B03A, 0x6B4A2B, SignatureFlavor.Dust), "Bamboo Club", "Pentung Bambu", "A bamboo pole straight from the stall frame.", "Bambu langsung dicabut dari rangka lapak."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xA86A44, 0xE0322B, SignatureFlavor.Dust), "Lift and Slam", "Angkat dan Banting", "Hoists the opponent over his head and slams them.", "Mengangkat lawan di atas kepala lalu membantingnya."),
        ],
        ["tukang-parkir"] =
        [
            S(SignatureKind.Wave, Th(SignatureProp.Sound, 0xF6C21B, 0xF28C1B, SignatureFlavor.Notes), "Sonic Whistle", "Peluit Sonik", "A whistle so loud it knocks people over.", "Peluit sekeras itu sampai orang-orang terjengkang."),
            S(SignatureKind.Crowd, Th(SignatureProp.Bike, 0x2E5C8A, 0xF28C1B, SignatureFlavor.Smoke), "Illegal Parking", "Parkir Liar", "A row of motorbikes reverses out all at once.", "Sederet motor mundur keluar bersamaan."),
            S(SignatureKind.Weapon, Th(SignatureProp.Pole, 0xF3EEE1, 0xE0322B, SignatureFlavor.Sparks), "Parking Barrier", "Palang Parkir", "The striped barrier arm swings down. No ticket, no exit.", "Palang bergaris turun. Tidak ada karcis, tidak ada jalan keluar."),
        ],
        ["satpam-komplek"] =
        [
            S(SignatureKind.Weapon, Th(SignatureProp.Pole, 0x1A1A1A, 0xF6C21B, SignatureFlavor.Electric), "Night Shift Baton", "Pentungan Ronda", "The baton he has polished for twenty years.", "Pentungan yang ia gosok selama dua puluh tahun."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x6B5B45, 0xF6C21B, SignatureFlavor.Notes), "Village Patrol", "Ronda Kampung", "Neighbours with kentongan drums sweep the street.", "Warga membawa kentongan menyisir jalan."),
            S(SignatureKind.Quake, Th(SignatureProp.Slab, 0x2C3E66, 0xF3EEE1, SignatureFlavor.Dust), "Gate Slam", "Gerbang Ditutup", "Slams the complex gate so hard the ground shakes.", "Menutup gerbang komplek sampai tanahnya bergetar."),
        ],
        ["abang-bakso"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0x8C5A2B, 0xF6C21B, SignatureFlavor.Smoke), "Meatballs Galore", "Bakso Beranak", "Meatballs that split into more meatballs.", "Bakso yang beranak menjadi bakso-bakso lagi."),
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0xC9902B, 0xE0322B, SignatureFlavor.Smoke), "Broth Flood", "Banjir Kuah", "A tidal wave of scalding, spicy broth.", "Gelombang kuah panas yang pedas."),
            S(SignatureKind.Weapon, Th(SignatureProp.Cart, 0x2E8B57, 0xF3EEE1, SignatureFlavor.Smoke), "Runaway Cart", "Gerobak Ngebut", "The cart, the bowls, the stove, all of it.", "Gerobaknya, mangkoknya, kompornya, semuanya."),
        ],
        ["emak-emak-komplek"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Slab, 0x2E8B57, 0xF6C21B, SignatureFlavor.Dust), "Homing Sandals", "Sandal Kendali", "Flip-flops that always find the naughty one.", "Sandal jepit yang selalu menemukan anak nakal."),
            S(SignatureKind.Crowd, Th(SignatureProp.Bike, 0x9C27B0, 0xF6C21B, SignatureFlavor.Smoke), "Arisan Gang", "Geng Arisan", "The moms ride in on scooters, left signal on, turning right.", "Para emak datang naik motor matic, sein kiri, belok kanan."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xD49A6E, 0x9C27B0, SignatureFlavor.Dust), "Ear Pull", "Jeweran Maut", "Lifted by the ear, then sat down very firmly.", "Diangkat lewat telinga, lalu didudukkan dengan sangat tegas."),
        ],
        ["kuntilanak"] =
        [
            S(SignatureKind.Summon, Th(SignatureProp.Ghost, 0xF3EEE1, 0xE0322B, SignatureFlavor.Ghostly), "Twin Kunti", "Kunti Kembar", "Her sisters drift out of the trees, laughing.", "Saudari-saudarinya melayang keluar dari pohon sambil tertawa."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x15120F, 0xF3EEE1, SignatureFlavor.Ghostly), "Hair Snare", "Rambut Penjerat", "Her hair coils around the victim and lifts them up.", "Rambutnya melilit korban dan mengangkatnya ke udara."),
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xE0322B, 0xF3EEE1, SignatureFlavor.Ghostly), "Thursday Night", "Malam Jumat Kliwon", "A blood-red moon and lightning that only strikes the brave.", "Bulan semerah darah dan petir yang hanya menyambar pemberani."),
        ],
        ["pocong"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Ghost, 0xF3EEE1, 0xCFC8B6, SignatureFlavor.Ghostly), "Pocong Army", "Pasukan Pocong", "A whole graveyard hops across the arena.", "Satu kuburan melompat-lompat melintasi arena."),
            S(SignatureKind.Tornado, Th(SignatureProp.Card, 0xF3EEE1, 0xCFC8B6, SignatureFlavor.Ghostly), "Shroud Tornado", "Gulungan Kafan", "Spins so fast his shroud becomes a whirlwind.", "Berputar begitu cepat sampai kafannya menjadi angin puyuh."),
            S(SignatureKind.Quake, Th(SignatureProp.Slab, 0x5A4632, 0xF3EEE1, SignatureFlavor.Dust), "Graveyard Hop", "Lompatan Kuburan", "One enormous hop, one enormous landing.", "Satu lompatan raksasa, satu pendaratan raksasa."),
        ],
        ["buto-ijo"] =
        [
            S(SignatureKind.Quake, Th(SignatureProp.Slab, 0x3FA265, 0x5A3A1E, SignatureFlavor.Dust), "Giant's Stomp", "Hentakan Raksasa", "The earth cracks under one green foot.", "Bumi retak di bawah satu kaki hijau."),
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0x6A5A4E, 0x3FA265, SignatureFlavor.Dust), "Boulder Toss", "Lempar Batu Kali", "River boulders, thrown like pebbles.", "Batu kali dilempar seperti kerikil."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x3FA265, 0xF6C21B, SignatureFlavor.Dust), "Giant's Grip", "Genggaman Raksasa", "Picks the opponent up like a snack, then changes his mind.", "Mengangkat lawan seperti camilan, lalu berubah pikiran."),
        ],
        ["genderuwo"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0x3A2A20, 0xE0322B, SignatureFlavor.Smoke), "Phantom Stones", "Hujan Batu Gaib", "Stones thrown by nobody, from nowhere.", "Batu dilempar oleh tak seorang pun, dari entah mana."),
            S(SignatureKind.Summon, Th(SignatureProp.Beast, 0x1A1410, 0xE0322B, SignatureFlavor.Smoke), "Forest Shadows", "Bayangan Rimba", "Shaggy shadows peel off the trees and pounce.", "Bayangan berbulu lepas dari pepohonan lalu menerkam."),
            S(SignatureKind.Storm, Th(SignatureProp.Pole, 0x5A3A1E, 0x2F5A2A, SignatureFlavor.Leaves), "Banyan's Wrath", "Beringin Murka", "The old banyan drops its heaviest branches.", "Beringin tua menjatuhkan dahan-dahan terberatnya."),
        ],
        ["wewe-gombel"] =
        [
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x8E7A6A, 0x4A3B52, SignatureFlavor.Ghostly), "Long Embrace", "Dekapan Wewe", "Arms that reach across the arena and carry you off.", "Lengan yang menjangkau seberang arena dan membawamu pergi."),
            S(SignatureKind.Summon, Th(SignatureProp.Ghost, 0xB0A0C0, 0x4A3B52, SignatureFlavor.Ghostly), "The Lost Children", "Anak-anak Hilang", "The children she hid come out to play rough.", "Anak-anak yang ia sembunyikan keluar dan bermain kasar."),
            S(SignatureKind.Storm, Th(SignatureProp.Water, 0xC8C0D0, 0x4A3B52, SignatureFlavor.Smoke), "Gombel Fog", "Kabut Bukit Gombel", "A cold fog that strikes from above.", "Kabut dingin yang menyambar dari atas."),
        ],
        ["begu-ganjang"] =
        [
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x3B3B55, 0x6BD0FF, SignatureFlavor.Ghostly), "Pulled Upwards", "Ditarik ke Atas", "The longer you look up, the higher you go.", "Makin lama kamu mendongak, makin tinggi kamu terbang."),
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0x6BD0FF, 0xF3EEE1, SignatureFlavor.Electric), "Toba Lightning", "Petir Toba", "Lightning from the storms over Lake Toba.", "Petir dari badai di atas Danau Toba."),
            S(SignatureKind.Summon, Th(SignatureProp.Ghost, 0x1E1E2A, 0x6BD0FF, SignatureFlavor.Ghostly), "Giant Shadow", "Bayang Raksasa", "His shadow stands up on its own and stomps.", "Bayangannya berdiri sendiri lalu menginjak-injak."),
        ],
        ["leak-bali"] =
        [
            S(SignatureKind.Storm, Th(SignatureProp.Fireball, 0xFF6A1A, 0xF6C21B, SignatureFlavor.Fire), "Leak Fire Rain", "Hujan Api Leak", "Balls of cemetery fire fall like rain.", "Bola api kuburan berjatuhan seperti hujan."),
            S(SignatureKind.Summon, Th(SignatureProp.Head, 0xC9A27E, 0xFF6A1A, SignatureFlavor.Fire), "Flying Heads", "Kepala Terbang", "Glowing heads leave their bodies and bite.", "Kepala-kepala bercahaya meninggalkan tubuhnya lalu menggigit."),
            S(SignatureKind.Tornado, Th(SignatureProp.Fireball, 0xFF3A1A, 0xF6C21B, SignatureFlavor.Fire), "Rangda's Vortex", "Pusaran Rangda", "A tornado of black magic fire.", "Angin puting beliung api ilmu hitam."),
        ],
        ["orang-utan"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Ball, 0x9AAA3A, 0x5A6A1A, SignatureFlavor.Leaves), "Durian Rain", "Hujan Durian", "Ripe, heavy and spiky.", "Matang, berat, dan berduri."),
            S(SignatureKind.Crowd, Th(SignatureProp.Beast, 0xB4521E, 0x2E8B57, SignatureFlavor.Leaves), "Forest Troop", "Kawanan Rimba", "The whole family swings in from the canopy.", "Seluruh keluarganya berayun turun dari kanopi."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x3A5A2A, 0xB4521E, SignatureFlavor.Leaves), "Vine Swing", "Ayunan Liana", "Ties the opponent to a vine, swings, lets go.", "Mengikat lawan ke sulur, mengayun, lalu melepaskannya."),
        ],
        ["bekantan"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Beast, 0xC26B2E, 0xE9D7B5, SignatureFlavor.Leaves), "Proboscis Patrol", "Pasukan Bekantan", "A dozen big noses, one direction.", "Selusin hidung besar, satu arah."),
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0x4A8AA0, 0xE9D7B5, SignatureFlavor.Water), "Barito Current", "Arus Sungai Barito", "Calls the river over its banks.", "Memanggil sungai meluap dari tepiannya."),
            S(SignatureKind.Tornado, Th(SignatureProp.Leaf, 0x2E5A3A, 0xC26B2E, SignatureFlavor.Leaves), "Mangrove Spin", "Pusaran Bakau", "A whirl of mangrove leaves and river mud.", "Pusaran daun bakau dan lumpur sungai."),
        ],
        ["komodo"] =
        [
            S(SignatureKind.Summon, Th(SignatureProp.Beast, 0x5C6B4A, 0xF6C21B, SignatureFlavor.Dust), "Komodo Hatchlings", "Anak Komodo", "Small, fast and already venomous.", "Kecil, cepat, dan sudah berbisa."),
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0x7AA03A, 0x3E4733, SignatureFlavor.Smoke), "Venom Cloud", "Kabut Bisa", "A breath of venom that rolls across the floor.", "Napas berbisa yang bergulung di lantai."),
            S(SignatureKind.Quake, Th(SignatureProp.Slab, 0x5C6B4A, 0x6E7A55, SignatureFlavor.Dust), "Tail Quake", "Gempa Ekor", "One swing of the tail, and the island shakes.", "Satu kibasan ekor, pulau pun bergetar."),
        ],
        ["harimau-sumatra"] =
        [
            S(SignatureKind.Summon, Th(SignatureProp.Beast, 0xF28C1B, 0x1A1A1A, SignatureFlavor.Gold), "Ancestor Tigers", "Roh Harimau", "The spirits of old tigers run beside him.", "Roh harimau-harimau tua berlari di sampingnya."),
            S(SignatureKind.Tornado, Th(SignatureProp.Sound, 0xF28C1B, 0xF3EEE1, SignatureFlavor.Dust), "Roaring Storm", "Auman Badai", "A roar that turns into a spinning gale.", "Auman yang berubah menjadi angin berputar."),
            S(SignatureKind.Weapon, Th(SignatureProp.Blade, 0xF3EEE1, 0xF28C1B, SignatureFlavor.Sparks), "Giant Claw", "Cakar Raksasa", "A claw the size of a door, glowing with the forest's anger.", "Cakar sebesar pintu, menyala oleh amarah hutan."),
        ],
        ["cenderawasih"] =
        [
            S(SignatureKind.Tornado, Th(SignatureProp.Leaf, 0xF2B705, 0x8C1C13, SignatureFlavor.Feathers), "Feather Storm", "Badai Bulu", "A cyclone of golden plumes, each one a blade.", "Topan bulu emas, setiap helainya sebilah pisau."),
            S(SignatureKind.Crowd, Th(SignatureProp.Bird, 0xF2B705, 0x2E8B57, SignatureFlavor.Feathers), "Paradise Flock", "Kawanan Cenderawasih", "A flock of birds of paradise dives across the arena.", "Sekawanan cenderawasih menukik melintasi arena."),
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xFFF0A0, 0xF2B705, SignatureFlavor.Gold), "Papuan Dawn", "Fajar Papua", "Beams of the first sunrise strike the ground.", "Sinar matahari pertama menyambar tanah."),
        ],
        ["nyi-roro-kidul"] =
        [
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0x0E7A52, 0xBFE6FF, SignatureFlavor.Water), "Southern Tide", "Ombak Pantai Selatan", "The southern sea answers her call.", "Laut selatan menjawab panggilannya."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x0E7A52, 0xD4AF37, SignatureFlavor.Water), "Green Sash", "Selendang Hijau", "Her sash lifts the opponent towards the waves.", "Selendangnya mengangkat lawan menuju ombak."),
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xBFE6FF, 0x0E7A52, SignatureFlavor.Electric), "Ocean Storm", "Badai Samudra", "Thunder over the black sand of Parangtritis.", "Guntur di atas pasir hitam Parangtritis."),
        ],
        ["gatotkaca"] =
        [
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xF6C21B, 0xFFFFFF, SignatureFlavor.Electric), "Brajamusti Lightning", "Petir Brajamusti", "His fists call lightning down from heaven.", "Tinjunya memanggil petir turun dari kahyangan."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x8C1C13, 0xD4AF37, SignatureFlavor.Gold), "Fly and Slam", "Terbang dan Banting", "Carries the opponent into the clouds, then down.", "Membawa lawan terbang ke awan, lalu ke bawah."),
            S(SignatureKind.Quake, Th(SignatureProp.Fireball, 0xFF6A1A, 0xD4AF37, SignatureFlavor.Fire), "Meteor Fist", "Tinju Meteor", "Falls from the sky like a meteor, fist first.", "Jatuh dari langit seperti meteor, tinju lebih dulu."),
        ],
        ["si-pitung"] =
        [
            S(SignatureKind.Weapon, Th(SignatureProp.Blade, 0xD0D0D8, 0x5A3A1E, SignatureFlavor.Sparks), "Rawa Belong Golok", "Golok Rawa Belong", "A golok that never misses an oppressor.", "Golok yang tak pernah meleset dari penindas."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0xE0322B, 0xF3EEE1, SignatureFlavor.Paper), "Kampung Champions", "Jagoan Kampung", "Betawi fighters and ondel-ondel charge with him.", "Jagoan Betawi dan ondel-ondel menyerbu bersamanya."),
            S(SignatureKind.Barrage, Th(SignatureProp.Blade, 0xD0D0D8, 0xE0322B, SignatureFlavor.Sparks), "Golok Volley", "Lempar Golok", "Throws golok after golok, each one returning to his hand.", "Melempar golok demi golok, semuanya kembali ke tangannya."),
        ],
        ["sangkuriang"] =
        [
            S(SignatureKind.Weapon, Th(SignatureProp.Paddle, 0x8C5A2B, 0x2E8B57, SignatureFlavor.Water), "Giant Oar", "Dayung Raksasa", "The oar of the boat he never finished.", "Dayung dari perahu yang tak pernah selesai."),
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0x3A7AA0, 0xBFE6FF, SignatureFlavor.Water), "Citarum Dam", "Bendung Citarum", "Releases the whole lake in one night.", "Melepaskan seisi danau dalam satu malam."),
            S(SignatureKind.Quake, Th(SignatureProp.Cart, 0x5A3A1E, 0x2E8B57, SignatureFlavor.Dust), "Boat Kick", "Tendang Perahu", "Kicks the boat over and it becomes a mountain.", "Menendang perahu hingga terbalik dan menjadi gunung."),
        ],
        ["jaka-tarub"] =
        [
            S(SignatureKind.Weapon, Th(SignatureProp.Hoe, 0x9AA4B0, 0x6BD0FF, SignatureFlavor.Leaves), "Heavenly Hoe", "Pacul Kahyangan", "The farmer's pacul, blessed by the nymphs, comes down like a guillotine.", "Pacul petani yang diberkati para bidadari, turun bagai pisau pancung."),
            S(SignatureKind.Summon, Th(SignatureProp.Ghost, 0xF3EEE1, 0x6BD0FF, SignatureFlavor.Gold), "Nymph Allies", "Bidadari Penolong", "Seven nymphs descend, and they are not happy.", "Tujuh bidadari turun, dan mereka tidak senang."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x6BD0FF, 0xF3EEE1, SignatureFlavor.Gold), "Flying Shawl", "Selendang Terbang", "The stolen shawl carries the opponent into the sky.", "Selendang curian membawa lawan terbang ke langit."),
        ],
        ["pejabat-korup"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Card, 0xF3EEE1, 0x2E8B57, SignatureFlavor.Paper), "Envelope Rain", "Hujan Amplop", "Envelopes full of 'appreciation', thrown hard.", "Amplop berisi 'uang terima kasih', dilempar keras-keras."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x1A1A1A, 0x2A2A2A, SignatureFlavor.Paper), "Personal Bodyguards", "Pengawal Pribadi", "Men in black suits clear the way.", "Pria berjas hitam membuka jalan."),
            S(SignatureKind.Weapon, Th(SignatureProp.Slab, 0x5A3A1E, 0x2E8B57, SignatureFlavor.Paper), "Cash Briefcase", "Koper Uang Pelicin", "A briefcase so full of cash it is a weapon.", "Koper yang begitu penuh uang sampai menjadi senjata."),
        ],
        ["anggota-dpr"] =
        [
            S(SignatureKind.Wave, Th(SignatureProp.Sound, 0xF3EEE1, 0xE0322B, SignatureFlavor.Notes), "Endless Speech", "Pidato Tanpa Ujung", "A speech so long the sound itself pushes people back.", "Pidato begitu panjang sampai suaranya mendorong orang mundur."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x1E2A4A, 0xD4AF37, SignatureFlavor.Paper), "Study Trip Entourage", "Rombongan Studi Banding", "The whole committee, with luggage, back from abroad.", "Seluruh komisi beserta koper, baru pulang studi banding."),
            S(SignatureKind.Storm, Th(SignatureProp.Pole, 0x6B4A2B, 0xD4AF37, SignatureFlavor.Gold), "Gavel Drop", "Ketuk Palu", "Giant gavels fall from the sky. Passed.", "Palu raksasa jatuh dari langit. Sah."),
        ],
        ["buzzer-politik"] =
        [
            S(SignatureKind.Tornado, Th(SignatureProp.Card, 0x6BD0FF, 0x1F4FB0, SignatureFlavor.Electric), "Hashtag Storm", "Badai Tagar", "A trending topic you can actually get hit by.", "Topik trending yang benar-benar bisa memukulmu."),
            S(SignatureKind.Crowd, Th(SignatureProp.Head, 0x2A2A44, 0x6BD0FF, SignatureFlavor.Electric), "Bot Swarm", "Serbuan Akun Bot", "Four hundred anonymous accounts, all at once.", "Empat ratus akun anonim, sekaligus."),
            S(SignatureKind.Barrage, Th(SignatureProp.Card, 0xE0322B, 0xF3EEE1, SignatureFlavor.Electric), "Notification Spam", "Spam Notifikasi", "Every notification arrives at once, physically.", "Semua notifikasi datang bersamaan, secara fisik."),
        ],
        ["influencer"] =
        [
            S(SignatureKind.Barrage, Th(SignatureProp.Orb, 0xFFFFFF, 0xF2B5D4, SignatureFlavor.Gold), "Ring Light Flash", "Kilatan Ring Light", "Blinding beauty-mode flashes.", "Kilatan mode cantik yang menyilaukan."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0xF2B5D4, 0xF3EEE1, SignatureFlavor.Paper), "Die-hard Fans", "Fans Garis Keras", "Fans with phones out stampede for a selfie.", "Fans dengan ponsel di tangan menyerbu demi selfie."),
            S(SignatureKind.Levitate, Th(SignatureProp.Orb, 0xF2B5D4, 0xFFFFFF, SignatureFlavor.Gold), "Going Viral", "Viral Melambung", "Lifted on a wave of views, then dropped by the algorithm.", "Terangkat oleh banjir views, lalu dijatuhkan algoritma."),
        ],
        ["anak-stm"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Bike, 0xF3EEE1, 0x6E7680, SignatureFlavor.Smoke), "STM Convoy", "Konvoi STM", "The whole class arrives on loud bikes.", "Satu angkatan datang naik motor berknalpot bising."),
            S(SignatureKind.Barrage, Th(SignatureProp.Blade, 0xA0A0A8, 0xF6C21B, SignatureFlavor.Sparks), "Wrench Volley", "Lempar Kunci Pas", "Every spanner in the workshop, airborne.", "Semua kunci di bengkel, terbang."),
            S(SignatureKind.Weapon, Th(SignatureProp.Pole, 0x1A1A1A, 0xA0A0A8, SignatureFlavor.Sparks), "Gear Belt", "Ikat Pinggang Gir", "A belt buckled with a motorbike sprocket.", "Ikat pinggang berkepala gir motor."),
        ],
        ["mahasiswi-demo"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0xF6C21B, 0x1F4FB0, SignatureFlavor.Paper), "Call the Protesters", "Panggil Pendemo", "A column of students in campus jackets charges with banners.", "Barisan mahasiswa berjaket almamater menyerbu membawa spanduk."),
            S(SignatureKind.Wave, Th(SignatureProp.Sound, 0xF6C21B, 0xE0322B, SignatureFlavor.Notes), "Thousand-Decibel Megaphone", "Toa Seribu Desibel", "One chant through the megaphone, one shockwave.", "Satu yel-yel lewat toa, satu gelombang kejut."),
            S(SignatureKind.Storm, Th(SignatureProp.Slab, 0xF3EEE1, 0xE0322B, SignatureFlavor.Paper), "Banner Downpour", "Hujan Spanduk", "Protest banners rain down from the overpass.", "Spanduk demo berjatuhan dari jembatan penyeberangan."),
        ],
        ["kang-ojol"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Bike, 0x00A651, 0x1A1A1A, SignatureFlavor.Smoke), "Ojol Convoy", "Konvoi Ojol", "Every driver in the city answers the call.", "Semua pengemudi sekota menjawab panggilan."),
            S(SignatureKind.Barrage, Th(SignatureProp.Head, 0x00A651, 0xF3EEE1, SignatureFlavor.Smoke), "Helmet Throw", "Lempar Helm", "Spare helmets for everyone, delivered at speed.", "Helm cadangan untuk semua, diantar dengan kencang."),
            S(SignatureKind.Tornado, Th(SignatureProp.Bike, 0x00A651, 0xF6C21B, SignatureFlavor.Smoke), "Drift Tornado", "Drift Muter", "Spins his bike so fast it becomes a tornado of smoke.", "Memutar motornya begitu cepat sampai menjadi topan asap."),
        ],
        ["tukang-debus"] =
        [
            S(SignatureKind.Weapon, Th(SignatureProp.Blade, 0xFF6A1A, 0xF6C21B, SignatureFlavor.Fire), "Flaming Golok", "Golok Api", "A golok wrapped in flame from his own breath.", "Golok berselimut api dari napasnya sendiri."),
            S(SignatureKind.Barrage, Th(SignatureProp.Fireball, 0xFF6A1A, 0xF6C21B, SignatureFlavor.Fire), "Fire Breath", "Semburan Api", "Kerosene, a torch, and a very deep breath.", "Minyak tanah, obor, dan napas yang sangat dalam."),
            S(SignatureKind.Quake, Th(SignatureProp.Slab, 0x1A1A1A, 0xE0322B, SignatureFlavor.Fire), "Iron Body Blast", "Ledakan Kebal", "Takes the hit, and the shockwave knocks everyone else down.", "Menerima pukulan, dan gelombangnya menjatuhkan semua orang."),
        ],
        ["anak-punk"] =
        [
            S(SignatureKind.Wave, Th(SignatureProp.Sound, 0xE0322B, 0x1A1A1A, SignatureFlavor.Notes), "Full Distortion", "Distorsi Penuh", "One chord through a broken amp.", "Satu kunci gitar lewat ampli rusak."),
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x1A1A1A, 0xE0322B, SignatureFlavor.Notes), "Mosh Pit", "Pogo Massal", "Every punk in Bandung pogos through the arena.", "Semua anak punk Bandung ber-pogo melintasi arena."),
            S(SignatureKind.Tornado, Th(SignatureProp.Person, 0xE0322B, 0x1A1A1A, SignatureFlavor.Dust), "Circle Pit", "Pogo Melingkar", "A circle pit so big it has its own weather.", "Pogo melingkar sebesar itu sampai punya cuaca sendiri."),
        ],
        ["pemadam-karhutla"] =
        [
            S(SignatureKind.Wave, Th(SignatureProp.Water, 0x4AA0E0, 0xF3EEE1, SignatureFlavor.Water), "High-Pressure Jet", "Semprotan Bertekanan", "The hose at full pressure, aimed at you.", "Selang tekanan penuh, diarahkan padamu."),
            S(SignatureKind.Storm, Th(SignatureProp.Water, 0x4AA0E0, 0xF28C1B, SignatureFlavor.Water), "Helicopter Water Bomb", "Bom Air Helikopter", "The water-bombing helicopter drops on his signal.", "Helikopter pembom air menjatuhkan muatannya atas aba-abanya."),
            S(SignatureKind.Weapon, Th(SignatureProp.Blade, 0xE0322B, 0xF3EEE1, SignatureFlavor.Fire), "Fire Axe", "Kapak Pemadam", "The axe that has cut a thousand firebreaks.", "Kapak yang sudah membuat seribu sekat bakar."),
        ],
        ["horeg-dancer"] =
        [
            S(SignatureKind.Quake, Th(SignatureProp.Sound, 0xFF3EA5, 0x7CFC00, SignatureFlavor.Notes), "Bass Drop", "Bass Drop", "Ten thousand watts of sub-bass hit the ground.", "Sepuluh ribu watt sub-bass menghantam tanah."),
            S(SignatureKind.Tornado, Th(SignatureProp.Sound, 0x7CFC00, 0xFF3EA5, SignatureFlavor.Notes), "Horeg Twister", "Goyang Topan", "She dances so hard the air starts spinning.", "Ia bergoyang begitu keras sampai udara ikut berputar."),
            S(SignatureKind.Crowd, Th(SignatureProp.Truck, 0x222228, 0xFF3EA5, SignatureFlavor.Notes), "Sound Truck Pass", "Truk Sound Lewat", "A three-storey sound system rolls through the fight.", "Sound system tiga lantai melintas di tengah pertarungan."),
        ],
        ["kuyang"] =
        [
            S(SignatureKind.Summon, Th(SignatureProp.Head, 0xE8DCCB, 0xE0322B, SignatureFlavor.Ghostly), "Floating Heads", "Kepala Melayang", "Her sisters' heads glide in over the river.", "Kepala saudari-saudarinya melayang datang dari sungai."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xE0322B, 0x2A1A1A, SignatureFlavor.Ghostly), "Night Drain", "Isapan Malam", "Lifts the victim with a red glow and drains them.", "Mengangkat korban dengan cahaya merah lalu mengisapnya."),
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xE0322B, 0x2A1A1A, SignatureFlavor.Ghostly), "Red Moon", "Bulan Merah", "Strikes of red light from a red moon.", "Sambaran cahaya merah dari bulan merah."),
        ],
        ["baphomet"] =
        [
            S(SignatureKind.Storm, Th(SignatureProp.Bolt, 0xF2B705, 0x2A2A2A, SignatureFlavor.Electric), "Amber Eclipse", "Gerhana Kuning", "The sun goes dark and amber lightning falls.", "Matahari menggelap dan petir kuning berjatuhan."),
            S(SignatureKind.Summon, Th(SignatureProp.Beast, 0x2A2A2A, 0xF2B705, SignatureFlavor.Smoke), "Shadow Goats", "Kambing Bayangan", "Horned shadows gallop out of the dark.", "Bayangan bertanduk berlari keluar dari kegelapan."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0x2A2A2A, 0xF2B705, SignatureFlavor.Smoke), "Dark Throne", "Tahta Kegelapan", "Raises the opponent onto an invisible throne, then topples it.", "Mengangkat lawan ke tahta tak kasatmata, lalu menjatuhkannya."),
        ],
        ["ogoh-ogoh"] =
        [
            S(SignatureKind.Crowd, Th(SignatureProp.Person, 0x111111, 0xF3EEE1, SignatureFlavor.Fire), "Bearers' Parade", "Arak-arakan Pengusung", "The bearers run the effigy through the crowd.", "Para pengusung mengarak ogoh-ogoh menembus kerumunan."),
            S(SignatureKind.Quake, Th(SignatureProp.Pole, 0xC9B03A, 0xC0392B, SignatureFlavor.Dust), "Bamboo Stomp", "Hentakan Bambu", "The bamboo frame slams down with the gamelan.", "Rangka bambu menghentak seiring gamelan."),
            S(SignatureKind.Tornado, Th(SignatureProp.Fireball, 0xC0392B, 0xF6C21B, SignatureFlavor.Fire), "Crossroads Spin", "Putaran Perempatan", "Spun three times at the crossroads, it becomes a storm.", "Diputar tiga kali di perempatan, ia menjadi badai."),
        ],
        ["reog-ponorogo"] =
        [
            S(SignatureKind.Tornado, Th(SignatureProp.Leaf, 0x2E8B57, 0x1F4FB0, SignatureFlavor.Feathers), "Peacock Fan Storm", "Kibasan Merak", "The giant peacock fan whips up a whirlwind.", "Kipas merak raksasa mengibaskan angin puyuh."),
            S(SignatureKind.Crowd, Th(SignatureProp.Beast, 0xE0322B, 0xF6C21B, SignatureFlavor.Dust), "Jathilan Riders", "Pasukan Jathilan", "Horse dancers gallop in on woven horses.", "Penari kuda lumping berderap datang di atas kuda anyaman."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xB22222, 0xF6C21B, SignatureFlavor.Gold), "Barong Bite", "Gigitan Barong", "Lifts the opponent in the Singo Barong's jaws.", "Mengangkat lawan di rahang Singo Barong."),
        ],
        ["dukun-santet"] =
        [
            S(SignatureKind.Summon, Th(SignatureProp.Head, 0x7A8A6A, 0x2E8B57, SignatureFlavor.Ghostly), "Tuyul Delivery", "Kiriman Tuyul", "Sends his little helpers to pick your pockets, painfully.", "Mengirim tuyul-tuyulnya untuk merogoh sakumu, dengan menyakitkan."),
            S(SignatureKind.Levitate, Th(SignatureProp.Hand, 0xC9B03A, 0x7A1F1F, SignatureFlavor.Smoke), "Voodoo Doll", "Boneka Santet", "Whatever happens to the doll happens to you.", "Apa pun yang terjadi pada boneka terjadi padamu."),
            S(SignatureKind.Storm, Th(SignatureProp.Arrow, 0x8A8A8A, 0x7A1F1F, SignatureFlavor.Smoke), "Rain of Nails", "Hujan Paku", "Rusty nails fall from a clear sky.", "Paku berkarat jatuh dari langit cerah."),
        ],
    };

    /// <summary>The three signature moves of <paramref name="c"/>.</summary>
    public static SignatureMove[] For(CharacterDefinition c)
    {
        var entries = s_table.TryGetValue(c.Id, out var e) ? e : Fallback(c);
        var result = new SignatureMove[3];
        for (int i = 0; i < 3; i++) result[i] = new SignatureMove(i, entries[i].Item1, entries[i].Item2, entries[i].Item3, entries[i].Item4);
        return result;
    }

    public static bool IsHandAuthored(string id) => s_table.ContainsKey(id);

    private static (SignatureKind, SignatureTheme, LocalizedText, LocalizedText)[] Fallback(CharacterDefinition c)
    {
        uint a = c.Body.PrimaryColor, b = c.Body.AccentColor;
        return c.Archetype switch
        {
            Archetype.Grappler =>
            [
                S(SignatureKind.Quake, Th(SignatureProp.Slab, a, b, SignatureFlavor.Dust), "Earth Breaker", "Pemecah Bumi", "A stomp that splits the floor.", "Hentakan yang membelah lantai."),
                S(SignatureKind.Weapon, Th(SignatureProp.Pole, a, b, SignatureFlavor.Sparks), "Heavy Club", "Gada Berat", "A club as heavy as a motorbike.", "Gada seberat motor."),
                S(SignatureKind.Levitate, Th(SignatureProp.Hand, a, b, SignatureFlavor.Dust), "Sky Slam", "Bantingan Langit", "Lifted high, dropped hard.", "Diangkat tinggi, dijatuhkan keras."),
            ],
            Archetype.Mystic =>
            [
                S(SignatureKind.Summon, Th(SignatureProp.Ghost, a, b, SignatureFlavor.Ghostly), "Spirit Call", "Panggilan Arwah", "Spirits answer and attack.", "Para arwah menjawab dan menyerang."),
                S(SignatureKind.Storm, Th(SignatureProp.Bolt, b, a, SignatureFlavor.Electric), "Night Lightning", "Petir Malam", "Lightning on command.", "Petir sesuai perintah."),
                S(SignatureKind.Levitate, Th(SignatureProp.Hand, a, b, SignatureFlavor.Ghostly), "Floating Curse", "Kutukan Melayang", "The curse lifts and drops its victim.", "Kutukan mengangkat lalu menjatuhkan korbannya."),
            ],
            Archetype.Beast =>
            [
                S(SignatureKind.Summon, Th(SignatureProp.Beast, a, b, SignatureFlavor.Dust), "Pack Call", "Panggil Kawanan", "The pack arrives.", "Kawanan berdatangan."),
                S(SignatureKind.Tornado, Th(SignatureProp.Leaf, a, b, SignatureFlavor.Leaves), "Jungle Gale", "Angin Rimba", "A whirlwind of leaves and claws.", "Pusaran daun dan cakar."),
                S(SignatureKind.Quake, Th(SignatureProp.Slab, a, b, SignatureFlavor.Dust), "Stampede", "Hentakan Kawanan", "The ground shakes under the herd.", "Tanah bergetar di bawah kawanan."),
            ],
            Archetype.Legend =>
            [
                S(SignatureKind.Storm, Th(SignatureProp.Bolt, b, a, SignatureFlavor.Electric), "Heaven's Strike", "Sambaran Kahyangan", "Lightning from the old gods.", "Petir dari para dewa lama."),
                S(SignatureKind.Wave, Th(SignatureProp.Water, a, b, SignatureFlavor.Water), "Legend's Tide", "Gelombang Legenda", "A wave of legend sweeps the arena.", "Gelombang legenda menyapu arena."),
                S(SignatureKind.Levitate, Th(SignatureProp.Hand, a, b, SignatureFlavor.Gold), "Ascension Slam", "Bantingan Kahyangan", "Up to heaven and back down.", "Naik ke kahyangan lalu turun lagi."),
            ],
            Archetype.Striker =>
            [
                S(SignatureKind.Tornado, Th(SignatureProp.Leaf, a, b, SignatureFlavor.Dust), "Cyclone Kick", "Tendangan Topan", "A kick that becomes a cyclone.", "Tendangan yang menjadi topan."),
                S(SignatureKind.Barrage, Th(SignatureProp.Blade, b, a, SignatureFlavor.Sparks), "Blade Volley", "Rentetan Pisau", "A volley of thrown blades.", "Rentetan pisau terbang."),
                S(SignatureKind.Weapon, Th(SignatureProp.Blade, b, a, SignatureFlavor.Sparks), "Signature Blade", "Bilah Pusaka", "A heirloom blade, drawn at last.", "Bilah pusaka, akhirnya dihunus."),
            ],
            _ =>
            [
                S(SignatureKind.Barrage, Th(SignatureProp.Ball, a, b, SignatureFlavor.Dust), "Kampung Barrage", "Rentetan Kampung", "Whatever is lying around, thrown hard.", "Apa saja yang tergeletak, dilempar keras."),
                S(SignatureKind.Crowd, Th(SignatureProp.Person, a, b, SignatureFlavor.Paper), "Call the Kampung", "Panggil Warga", "The whole neighbourhood joins in.", "Satu kampung ikut turun tangan."),
                S(SignatureKind.Weapon, Th(SignatureProp.Pole, a, b, SignatureFlavor.Dust), "Bamboo Pole", "Galah Bambu", "A long bamboo pole, swung wide.", "Galah bambu panjang, diayun lebar."),
            ],
        };
    }

    public static LocalizedText KindName(SignatureKind kind) => kind switch
    {
        SignatureKind.Summon => new("Summon", "Panggilan"),
        SignatureKind.Crowd => new("Crowd charge", "Serbuan massa"),
        SignatureKind.Weapon => new("Signature weapon", "Senjata pusaka"),
        SignatureKind.Storm => new("Weather strike", "Sambaran cuaca"),
        SignatureKind.Levitate => new("Lift & slam", "Angkat & banting"),
        SignatureKind.Tornado => new("Tornado", "Angin topan"),
        SignatureKind.Barrage => new("Barrage", "Rentetan"),
        SignatureKind.Quake => new("Quake", "Gempa"),
        _ => new("Wave", "Gelombang"),
    };
}
