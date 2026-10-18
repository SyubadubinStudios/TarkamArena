# Tarkam Arena - Legendary Fighting Tournament from Indonesia

## Solution Design Document

### Vision
Tarkam Arena adalah game fighting 3D khas Indonesia yang menggabungkan elemen terbaik dari Mortal Kombat, Street Fighter, Tekken, WWE/WWF, arena fighter, dan party fighting.

## Core Features
- 1 vs 1
- 2 vs 2
- Tag Battle
- Free For All
- Survival
- Story Campaign
- Tournament Mode
- Online Ranked
- Clan Wars
- Custom Matches

## Character Concepts
### Profesi
- Gembel Lampu Merah
- Kasir Indomaret
- Guru Matematik
- Pesilat
- Preman Pasar
- Tukang Parkir
- Satpam Komplek
- Abang Bakso
- Emak-emak Komplek

### Makhluk Mistis
- Kuntilanak
- Pocong
- Buto Ijo
- Genderuwo
- Wewe Gombel
- Begu Ganjang
- Leak Bali
- Kuyang
- Baphomet
- Ogoh-ogoh
- Reog Ponorogo (Singo Barong)
- Dukun Santet

### Warga & Satire
Tokoh-tokoh fiksi dari kehidupan kota dan satir sosial. Semua tokoh adalah karakter fiksi; kesamaan dengan tokoh nyata hanyalah kebetulan.
- Pejabat Korup
- Anggota DPR
- Buzzer Politik
- Influencer
- Anak STM
- Mahasiswi Demo
- Kang Ojol
- Tukang Debus
- Anak Punk Jalanan
- Pemadam Karhutla
- Horeg Dancer (Penari Sound Horeg)

### Satwa Nusantara
- Orang Utan
- Bekantan
- Komodo
- Harimau Sumatra
- Cenderawasih Spirit

### Karakter Rahasia
- Nyi Roro Kidul
- Gatotkaca
- Si Pitung
- Sangkuriang
- Jaka Tarub

## Combat System
- Light Attack
- Heavy Attack
- Grab & Throw
- Counter
- Parry
- Dodge
- Rage Mode
- Adrenaline Mode
- Air Combo
- Launcher
- Team Combo

## Jurus Pamungkas (Signature Moves)
Setiap karakter memiliki 3 jurus pamungkas sendiri, dikeluarkan dengan kombinasi tombol dan dibayar
dengan meter Jurus (terisi pelan sendiri, lebih cepat saat memukul, sedikit saat dipukul/menangkis):
- `↓ ↘ → + S` (biaya 250), `↓ ↙ ← + S` (biaya 400), `↓ ↓ + S` (biaya 700, paling kuat)
- Awalan kebal, layar membeku dengan cut-in potret + nama jurus, arena meredup
- Bisa membatalkan serangan biasa yang kena (super cancel); CPU juga memakainya

Sembilan jenis efek, disesuaikan dengan karakter:
- Panggilan makhluk halus / hewan yang ikut menyerang (kepala Kuyang, Kunti kembar, bidadari, burung)
- Serbuan massa (pendemo, konvoi ojol, geng arisan, jagoan kampung, truk sound horeg)
- Senjata pusaka raksasa (pacul Jaka Tarub, golok Si Pitung, penggaris guru, gerobak bakso, kapak pemadam)
- Manipulasi cuaca & alam (petir, hujan api, bom air, papan rumus jatuh dari langit)
- Angkat & banting (lawan diterbangkan lalu dibanting: jeweran emak-emak, rambut Kunti, selendang)
- Angin topan yang menyedot dan melambungkan lawan
- Rentetan lemparan (bakso, kapur, sandal, amplop)
- Gempa yang merambat di lantai
- Gelombang (air, kuah, suara toa)

Setiap jurus punya VFX sendiri: rig 3D bertema, partikel (api, air, daun, bulu, asap, aura gaib,
listrik, kertas, not musik, emas), gelombang kejut, getaran kamera, dan suara.

## Finisher System
Setiap karakter memiliki:
- 3 Fatality
- 2 Brutality
- 2 Friendship
- 1 Mercy
- 1 Legendary Finisher

## Visual Effects
- HDR Rendering
- Volumetric Fog
- Dynamic Weather
- Destructible Environment
- Particle System
- Cinematic Camera
- Motion Blur
- Dynamic Lighting

## Stages
- Pasar Becek
- Sawah Jawa Tengah
- Gedung DPR
- Stasiun Transjakarta
- Terminal Kampung Rambutan
- Kampung Ambon
- Hutan Kalimantan
- Pantai Selatan
- Kota Tua Jakarta
- Gunung Bromo
- Candi Borobudur
- Karnaval Sound Horeg (Malang)
- Hutan Gambut Riau (karhutla)
- Alun-alun Ponorogo (Grebeg Suro)
- Malam Pengerupukan (Bali, malam sebelum Nyepi)
- Jalan Demo (Jakarta Pusat)
- Pangkalan Ojol (bawah jalan layang)
- Ruang Sidang Tipikor (fiksi)

## Living Arena NPC System
Setiap arena memiliki NPC unik dengan perilaku berbeda.

### NPC Activities
- Berjalan
- Berdagang
- Mengobrol
- Bersorak
- Merekam video
- Kabur saat fatality
- Bereaksi terhadap karakter tertentu

### Arena Examples
#### Pasar Becek
NPC:
- Pedagang
- Pembeli
- Tukang angkut
- Tukang sayur

Event:
- Ayam lepas
- Gerobak lewat
- Kambing mengamuk

#### Terminal Kampung Rambutan
NPC:
- Sopir
- Kernet
- Penumpang
- Pedagang asongan

Event:
- Bus masuk terminal
- Keramaian penumpang

#### Hutan Kalimantan
NPC:
- Ranger
- Peneliti
- Orang Utan

Event:
- Orang utan melempar buah
- Pohon tumbang

#### Karnaval Sound Horeg
NPC:
- Penari horeg
- Kru sound system
- Anak-anak
- Pedagang asongan
- Warga desa

Event:
- Bass drop (gelombang suara melempar petarung)
- Pawai truk sound
- Listrik padam

#### Hutan Gambut Riau
NPC:
- Pemadam karhutla
- Warga desa
- Ranger

Event:
- Api menjalar dari gambut
- Bom air dari helikopter

#### Alun-alun Ponorogo
NPC:
- Warok
- Penabuh gamelan reog
- Penari jathilan
- Wisatawan

Event:
- Kirab reog
- Kembang api festival

#### Malam Pengerupukan
NPC:
- Pecalang
- Penabuh baleganjur
- Wisatawan
- Anak-anak

Event:
- Ogoh-ogoh diputar di perempatan
- Obor menyala

#### Jalan Demo
NPC:
- Mahasiswa
- Polisi
- Wartawan
- Pedagang asongan

Event:
- Semprotan water cannon
- Gas air mata
- Long march

#### Pangkalan Ojol
NPC:
- Pengemudi ojol
- Pedagang kopi
- Komuter

Event:
- Serbuan motor
- Hujan deras

#### Ruang Sidang Tipikor
NPC:
- Hakim
- Pengacara
- Wartawan
- Petugas

Event:
- Kilatan kamera
- "Harap tenang!" (ketukan palu)

### Pengenalan Tokoh (Celebrity Recognition)
NPC mengenali petarung tertentu dan bereaksi: pedagang takut pada Preman Pasar, mahasiswa bersorak untuk Mahasiswi Demo, wartawan memotret Pejabat Korup, pengemudi ojol bersorak untuk Kang Ojol, warok menghormati Reog Ponorogo, dan semua warga kabur dari makhluk gaib.

## Dynamic Crowd System
- Crowd Excitement Meter
- Crowd Reputation System
- Dynamic Reactions
- Celebrity Recognition

## Audio System
Traditional:
- Gamelan
- Angklung
- Degung Sunda
- Talempong
- Sasando

- Baleganjur (Bali)
- Gamelan Reog (kendang, kenong, slompret)

Modern:
- Metal Hardcore
- Rock
- EDM
- Orchestral Hybrid
- Dangdut Koplo
- Punk
- Sound Horeg (bass berat ala karnaval Jawa Timur)

Efek suara khas arena:
- Bass drop sound horeg, klakson truk sound
- Sirene water cannon, yel-yel demonstran
- Raungan baleganjur dan sorakan pengerupukan
- Derak api gambut dan baling-baling helikopter
- Klakson dan knalpot rombongan motor ojol
- Ketukan palu hakim dan kilatan kamera

## Story Mode
Cerita multi-karakter dengan konflik manusia, dunia gaib, legenda Nusantara, dan turnamen Tarkam Arena.

## Character Progression
- Costume Unlock
- Victory Pose
- Intro Animation
- Emotes
- Player Banner
- Achievements

## Character Creator
Pemain dapat membuat:
- Petarung manusia
- Makhluk mistis
- Jawara daerah

## About / Credits
Scrolling credits bergaya film dengan:
- Wayang silhouette
- Drone landscape Indonesia
- Orchestra Gamelan Hybrid

Penutup:
> Legenda tidak dilahirkan. Legenda ditempa di arena.

## Technology Stack
### Framework
- .NET 10
- Avalonia UI

### Graphics
- ThreeNet
- Vibe_Graphics

### Physics
- BepuPhysics

### Networking
- LiteNetLib
- SignalR

### Database
- SQLite
- PostgreSQL

## Content Creation Pipeline
Concept Art -> Rodin MCP -> Blender MCP -> Optimization -> ThreeNet -> Build

### Rodin MCP
- Character Generation
- Animation Generation
- Auto Rigging

### Blender MCP
- Modeling
- Rigging
- Environment Creation

## Installers
### Windows
TarkamArena.Setup.exe

### Linux
- TarkamArena.AppImage
- TarkamArena.deb
- TarkamArena.rpm

### MacOS
TarkamArena.dmg

### Xbox
Microsoft Store Package

### PlayStation
PlayStation Package

### Nintendo Switch
NSP Package

## DLC Ideas
### Horror Nusantara Pack
- Sundel Bolong
- Tuyul
- Palasik

### Jawara Nusantara Pack
- Pendekar Betawi
- Jawara Banten
- Pendekar Madura

### Future Indonesia Pack
- Cyborg Ojol
- AI Dukun
- Polisi Drone

## Monetization
- Base Game
- Cosmetic DLC
- Story Expansion
- Optional Battle Pass

## Tagline
"Dari pasar becek sampai dunia gaib, semua petarung punya cerita. Masuk arena, rebut kejayaan, dan jadilah legenda Indonesia."
