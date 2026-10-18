using TarkamArena.Core.AI;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Modes;

public sealed record StoryChapter(
    int Number,
    LocalizedText Title,
    string HeroId,
    string OpponentId,
    string StageId,
    CpuDifficulty Difficulty,
    LocalizedText Intro,
    LocalizedText Outro);

/// <summary>
/// The multi-character story: a street flyer for the Tarkam Arena cup pulls kampung fighters,
/// spirits and legends towards Pantai Selatan, where the tournament turns out to be a summoning.
/// </summary>
public static class StoryCampaign
{
    private static LocalizedText T(string en, string id) => new(en, id);

    public static readonly LocalizedText Prologue = T(
        "A flyer appears on every lamppost in the city overnight: TARKAM ARENA. The prize is not written anywhere. Everyone who reads it feels the same pull towards the southern sea.",
        "Dalam semalam, selebaran muncul di setiap tiang listrik kota: TARKAM ARENA. Hadiahnya tidak tertulis di mana pun. Semua yang membacanya merasakan tarikan yang sama menuju laut selatan.");

    public static readonly LocalizedText Epilogue = T(
        "Jarot tears the last flyer in half. The waves fall quiet. The queen smiles, for the first time in a century, at someone who refused her. The kampung keeps its fighters, and the arena keeps its legends.",
        "Jarot merobek selebaran terakhir. Ombak pun tenang. Sang ratu tersenyum, untuk pertama kalinya dalam seabad, kepada seseorang yang menolaknya. Kampung tetap memiliki petarungnya, dan arena tetap memiliki legendanya.");

    public static IReadOnlyList<StoryChapter> Chapters { get; } =
    [
        new(1, T("The Flyer", "Selebaran"), "gembel-lampu-merah", "preman-pasar", "pasar-becek", CpuDifficulty.Easy,
            T("Jarot finds the flyer taped to his traffic light. Codet wants it, because Codet wants everything in this market.",
              "Jarot menemukan selebaran tertempel di lampu merahnya. Codet menginginkannya, karena Codet menginginkan semua yang ada di pasar ini."),
            T("Codet hands over the flyer with a bloody nose. 'There are more of us,' he warns. 'Not just people.'",
              "Codet menyerahkan selebaran dengan hidung berdarah. 'Kita bukan satu-satunya,' katanya. 'Bukan cuma manusia.'")),
        new(2, T("Last Bus South", "Bus Terakhir ke Selatan"), "kasir-minimarket", "tukang-parkir", "terminal-kampung-rambutan", CpuDifficulty.Easy,
            T("Sinta closes the till early to catch the last bus south. Priit says the bay is full, and the fee is everything in her bag.",
              "Sinta menutup kasir lebih awal demi bus terakhir ke selatan. Priit bilang lahan penuh, dan tarifnya seluruh isi tasnya."),
            T("Priit waves the bus out himself. 'Left, left, keep going,' he mumbles. 'Something is calling everyone south.'",
              "Priit sendiri yang memandu bus keluar. 'Kiri, kiri, terus,' gumamnya. 'Ada yang memanggil semua orang ke selatan.'")),
        new(3, T("Lesson After Dark", "Pelajaran Setelah Gelap"), "guru-matematika", "kuntilanak", "kota-tua", CpuDifficulty.Normal,
            T("Pak Darmo stays late grading papers in the old town. A laugh echoes down the colonnade. The spirits have read the flyer too.",
              "Pak Darmo lembur memeriksa ulangan di kota tua. Sebuah tawa menggema di sepanjang lorong. Para makhluk halus juga membaca selebaran itu."),
            T("Kunti fades into the mist, still laughing. 'The wall between our worlds is thin this year, teacher. Count the days to the full moon.'",
              "Kunti menghilang dalam kabut sambil tertawa. 'Dinding antara dunia kita sedang tipis tahun ini, Pak Guru. Hitunglah hari menuju purnama.'")),
        new(4, T("The Angry Forest", "Hutan yang Marah"), "pesilat", "genderuwo", "hutan-kalimantan", CpuDifficulty.Normal,
            T("Rangga trains deep in Borneo. The Genderuwo blames every human for the chainsaws, and it will not listen to apologies.",
              "Rangga berlatih jauh di pedalaman Borneo. Genderuwo menyalahkan semua manusia atas gergaji mesin, dan ia tak mau mendengar permintaan maaf."),
            T("Ucok the orangutan steps between them. The Genderuwo backs into its banyan tree. Rangga bows to the forest and heads south.",
              "Ucok si orang utan melangkah di antara mereka. Genderuwo mundur ke pohon beringinnya. Rangga membungkuk pada hutan lalu berangkat ke selatan.")),
        new(5, T("Giant in the Paddies", "Raksasa di Sawah"), "harimau-sumatra", "buto-ijo", "sawah-jawa-tengah", CpuDifficulty.Normal,
            T("Buto Ijo is eating the harvest one field at a time. Datuk Belang came down from the hills because the farmers asked him to.",
              "Buto Ijo melahap panen satu petak demi satu petak. Datuk Belang turun gunung karena para petani memintanya."),
            T("The giant flees towards the sea, clutching the flyer. 'She promised me a feast,' it bellows. 'She promised all of us.'",
              "Sang raksasa kabur ke arah laut sambil menggenggam selebaran. 'Dia menjanjikanku pesta,' raungnya. 'Dia menjanjikan kami semua.'")),
        new(6, T("Night Shift", "Ronda Malam"), "emak-emak-komplek", "pocong", "kampung-ambon", CpuDifficulty.Hard,
            T("Something hops past Bu Ratna's gate every night at two. Tonight she is waiting with her best sandal.",
              "Ada yang melompat lewat depan pagar Bu Ratna setiap jam dua malam. Malam ini ia menunggu dengan sandal terbaiknya."),
            T("Joni just wanted someone to untie him. Ratna does it, then scolds him for twenty minutes. He hops south with a thank-you.",
              "Joni cuma ingin ada yang melepas talinya. Ratna melepasnya, lalu mengomelinya dua puluh menit. Ia melompat ke selatan sambil berterima kasih.")),
        new(7, T("Seal of the Stupa", "Segel Stupa"), "cenderawasih", "leak-bali", "candi-borobudur", CpuDifficulty.Hard,
            T("At dawn, Leak tries to break the old seal under the stupas so more spirits can cross over. The bird of paradise arrives first.",
              "Saat fajar, Leak mencoba memecah segel tua di bawah stupa agar lebih banyak roh bisa menyeberang. Burung cenderawasih tiba lebih dulu."),
            T("The seal holds, barely. Leak's mask cracks. 'You cannot stop the tide,' it hisses, and vanishes towards the southern shore.",
              "Segel bertahan, nyaris. Topeng Leak retak. 'Kau tak bisa menahan pasang,' desisnya, lalu menghilang ke pantai selatan.")),
        new(8, T("The Long Shadow", "Bayang Panjang"), "abang-bakso", "begu-ganjang", "gunung-bromo", CpuDifficulty.Hard,
            T("The semi-final is held on the sea of sand. Kumis came to sell meatballs to the crowd. Begu Ganjang came to make it grow.",
              "Semifinal digelar di lautan pasir. Kumis datang untuk berjualan bakso ke penonton. Begu Ganjang datang untuk tumbuh setinggi-tingginya."),
            T("A bowl of hot broth to the face shrinks any spirit. Kumis wins the semi-final, then gives his finals spot to the man who started it all.",
              "Semangkuk kuah panas di wajah bisa mengecilkan roh mana pun. Kumis memenangkan semifinal, lalu memberikan tiket finalnya pada orang yang memulai semua ini.")),
        new(9, T("Queen of the Southern Sea", "Ratu Laut Selatan"), "gembel-lampu-merah", "nyi-roro-kidul", "pantai-selatan", CpuDifficulty.Legend,
            T("The final is on the black sand of Parangtritis. The prize was never money: the champion serves the queen beneath the waves, forever.",
              "Final digelar di pasir hitam Parangtritis. Hadiahnya tak pernah uang: sang juara akan mengabdi pada ratu di bawah ombak, selamanya."),
            T("Jarot stands, soaked and bleeding, and says no. The queen has not heard that word in a hundred years.",
              "Jarot berdiri, basah kuyup dan berdarah, lalu berkata tidak. Sang ratu tak pernah mendengar kata itu selama seratus tahun.")),
    ];
}
