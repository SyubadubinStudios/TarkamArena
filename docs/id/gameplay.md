# Cara bermain

![Pilih pertandingan](../screenshots/03-mode-select.png)

## Kontrol

| Aksi | Pemain 1 | Pemain 2 | Gamepad |
|---|---|---|---|
| Gerak / mundur | A / D | ← / → | Stik kiri atau D-pad |
| Lompat | W | ↑ | Atas |
| Jongkok | S | ↓ | Bawah |
| Serangan ringan | J | NumPad 1 | X / □ |
| Serangan berat | K | NumPad 2 | Y / △ |
| Tangkap & banting | L | NumPad 3 | B / ○ |
| Tahan (ketuk untuk tangkis) | U | NumPad 4 | RB / R1 |
| Jurus khusus | I | NumPad 5 | A / ✕ |
| Menghindar | O | NumPad 6 | LB / L1 |
| Tukar rekan | H | NumPad 0 | LT / L2 |
| Mode amarah | Spasi | NumPad 7 | L3 / R3 |
| Jeda | Esc atau P | | Start |

Menu: panah atau WASD untuk bergerak, Enter/J untuk memilih, Esc/K untuk kembali, Q/E atau Tab untuk
berganti tab.

## Sistem pertarungan

Pertandingan berjalan di simulasi deterministik 60 Hz dengan frame data sungguhan: setiap jurus punya
frame awalan (startup), aktif, dan pemulihan, serta hitstun, blockstun, dan dorongan.

| Mekanik | Cara kerja |
|---|---|
| **Serangan ringan** | Jab cepat. Tekan Ringan lagi saat kena untuk merangkai Jab → Cross → Hook. |
| **Serangan berat** | Lambat dan kuat, serangan atas: harus ditahan sambil berdiri. |
| **Serangan bawah** | Bawah + Ringan. Harus ditahan sambil jongkok (Bawah + Tahan). |
| **Launcher** | Bawah + Berat. Melempar lawan ke udara; tekan Atas saat kena untuk mengejarnya. |
| **Kombo udara** | Ringan/Berat di udara. Setiap pukulan udara berikutnya melambungkan lawan sedikit lebih rendah; setelah enam pukulan lawan jatuh. Berat di udara membanting lawan ke tanah. |
| **Tangkap & banting** | Tak bisa ditahan dari jarak dekat; menukar posisi. Dua tangkapan bersamaan saling lepas (*tech*). Gagal pada lawan yang melayang atau sedang terkena hitstun. |
| **Counter** | Pukulan yang masuk saat lawan sedang memulai jurus: +25 % damage dan hitstun lebih lama. |
| **Tangkis (parry)** | Ketuk Tahan dalam 6 frame sebelum pukulan mendarat: tanpa damage, penyerang terpaku, dan adrenalinmu bertambah. |
| **Menghindar** | Langkah samping/guling singkat dengan frame kebal. Tahan maju untuk berguling melewati lawan. |
| **Jurus khusus** | Khusus, Maju + Khusus, dan Bawah + Khusus (atau gerakan ↓↘→ + serangan dan →↓↘ + serangan). Proyektil, terjangan, teleportasi, tangkapan khusus, reversal, dan gelombang kejut tergantung gaya petarung. Jurus khusus bisa membatalkan serangan biasa yang kena. |
| **Mode amarah** | Menerima damage mengisi meter Amarah (dua kali lebih cepat di bawah 25 % darah). Saat penuh, tekan Amarah: ledakan mendorong semua lawan dan damage-mu +25 % selama 8 detik. |
| **Mode adrenalin** | Pukulan yang kena dan tangkisan mengisi Adrenalin; saat penuh aktif sendiri: jalan lebih cepat dan pemulihan lebih singkat selama 6 detik. |
| **Team combo** | Di 2 lawan 2 dan Tag Battle, memukul lawan yang sedang dikombo rekanmu memberi +20 % damage. Di Tag Battle, menukar rekan saat lawan terkena hitstun membuat rekan masuk dengan launcher. |
| **Penurunan kombo** | Setiap pukulan dalam kombo 10 % lebih lemah (minimal 35 %). |

## Finisher

Saat ronde penentuan dimenangkan, pihak yang kalah berdiri linglung dan muncul tulisan **HABISI!**
Pemenang punya sekitar lima detik untuk memasukkan urutan empat input. HUD menampilkan daftar milik
petarungmu; [halaman petarung](characters.md) memuat semua urutannya.

| Jenis | Per petarung | Efek |
|---|---|---|
| Fatality | 3 | Penutup klasik |
| Brutality | 2 | Hujan pukulan brutal |
| Persahabatan | 2 | Mengakhiri pertarungan dengan kebaikan (lengkap dengan konfeti) |
| Ampunan | 1 | Yang kalah bangkit dengan 15 % darah dan pertarungan berlanjut (sekali per pertandingan) |
| Finisher Legenda | 1 | Butuh sorakan penonton penuh (meter sorakan ≥ 80 %) |

Urutan memakai F (ke arah lawan), B (menjauh), U, D, serta L/H/G/S untuk Ringan, Berat, Tangkap, Khusus.

## Mode permainan

| Mode | Keterangan |
|---|---|
| 1 lawan 1 | Tiga ronde |
| 2 lawan 2 | Empat petarung sekaligus, dua per tim |
| Tag Battle | Dua petarung per tim, satu di arena; yang KO digantikan rekannya |
| Bebas Semua | Tiga atau empat petarung, semua lawan semua |
| Survival | Gelombang tanpa akhir; darah terbawa, +20 % setiap menang; makin sulit |
| Mode Cerita | Sembilan bab melintasi Jawa, Borneo, Bali, dan laut selatan |
| Turnamen | Piala Tarkam delapan petarung: perempat final, semifinal, final |
| Ranked Online | Pertandingan berperingkat lewat server matchmaking, peer to peer dengan rollback |
| Perang Klan | Kemenangan ranked memberi poin untuk klanmu |
| Pertandingan Kustom | Ronde, waktu, damage, event arena, dan finisher diatur sendiri |
| Latihan | Waktu tak terbatas, darah terisi lagi, tanpa event |

## Arena yang hidup

Setiap arena punya warganya sendiri (pedagang, sopir, mahasiswa, warok, pecalang…). Mereka berjalan,
berdagang, mengobrol, bersorak, merekam pertarungan dengan ponsel, kabur saat finisher, dan
**mengenali petarung tertentu**: pedagang pasar takut pada Preman Pasar, mahasiswa bersorak untuk
Mahasiswi Demo, wartawan mengerubungi Pejabat Korup, semua orang kabur dari makhluk gaib.

Event arena terjadi di tengah pertarungan: ayam lepas, gerobak lewat, kambing mengamuk, bus datang,
orang utan melempar buah, ombak besar, bass drop sound horeg, water cannon, api gambut menjalar,
serbuan motor di bawah jalan layang. Event bertanda "memengaruhi pertarungan" adalah rintangan yang
menjatuhkan petarung (didahului peringatan merah berkedip).

## Penonton

Meter sorakan (VU meter sound system di bawah HUD) naik karena kombo, counter, tangkisan, launcher,
team combo, dan KO, lalu turun pelan bila tak ada yang menyerang. Penonton yang riuh memberi bonus
adrenalin kecil dan membuka Finisher Legenda. Sorakan tertinggimu menjadi **reputasi penonton** untuk
petarung tersebut.

## Progres

Setiap pertandingan memberi XP, koin, dan reputasi penonton. Level dan koin membuka kostum, pose
kemenangan, intro, emote, dan banner di **Koleksi**. Ada 42 pencapaian. Karakter rahasia terbuka
dengan cara berikut:

| Legenda | Cara membuka |
|---|---|
| Nyi Roro Kidul | Tamatkan Mode Cerita |
| Gatotkaca | Juarai Turnamen |
| Si Pitung | Capai gelombang Survival ke-10 |
| Sangkuriang | Lakukan 25 finisher |
| Jaka Tarub | Raih 30 pencapaian |

## Kreator petarung

![Kreator](../screenshots/14-creator.png)

Rakit petarung manusia, makhluk mistis, atau jawara daerah: pilih gaya (brawler, striker, grappler,
mystic, beast), bagikan tepat 24 poin ke Tenaga, Kecepatan, Pertahanan, dan Teknik, atur tinggi,
postur, warna, dan detail siluet, lalu lihat pratinjau 3D langsung. Petarung buatanmu muncul di tab
"Petarungmu" saat memilih karakter, lengkap dengan jurus khusus dan finisher.
