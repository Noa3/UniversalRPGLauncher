# Real RPG Maker XP data fixtures

These are data files two real RPG Maker XP installations wrote, copied from
games the repository was given and frozen here with their sizes and hashes.

An XP game keeps its database as marshal and its scripts as marshal. A game whose
archive is not encrypted keeps both in plain files under `Data/`, which is the
case for both of these. **No executable, DLL, script, archive, save, audio or
image is imported.** The scripts are deliberately left out: a script is code, and
this repository does not run a game's code. A marshal file is a value, a string,
a number and a list of them, and the reader turns that back into the same.

## Why two games and not one

The two were made on different installations, and neither the language nor the
encoding of their strings is the same: one is a Japanese installation
(`RGSS104J.dll`, `Game.rxproj` naming it) and one is an English one
(`RGSS104E.dll`). A reader that turned a string into text would give a different
answer for each of them, and a reader tested against only one of them would never
notice.

## What the tests claim and what they do not

`test_real_xp_data.cs` reads every file below and walks every value in it. That
covers the marshal reader against bytes a game actually wrote, which no amount of
checking against the rubies' own sources can replace.

It does **not** claim that a game's data is understood. Reading a value and
knowing what a game means by it are two different things, and only the first
exists. A map file is read as an `RPG::Map` holding its members; nothing in this
repository knows what a member called `@events` is for.

## `rgss-xp` — a Japanese installation

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `rgss-xp/Actors.rxdata` | 13533 | `e704c8430cd50e39eb06a7fa74123c32c49cda018aedcf2a23e8206a43173105` |
| `rgss-xp/Classes.rxdata` | 4012 | `096fc76788184472a414bce4fd8fa9044e9fea474cd08801cd249ed8bc2bf06a` |
| `rgss-xp/Enemies.rxdata` | 21906 | `c7dabd2baf265af2cc6cebbc54edcb7112de64ad16fc45e20277a0689e903953` |
| `rgss-xp/Items.rxdata` | 30070 | `d108dff8afbe674d182b3257e3d77f21443488ec8605dfdd71929363bafcc883` |
| `rgss-xp/Skills.rxdata` | 8917 | `34f052387396adc580ce54a68110935144fbd13f3b2779a05e457ca011a358b8` |
| `rgss-xp/States.rxdata` | 2651 | `11652bdcf156308c2b9cfad040cb9720d5f7a69e5fb761d3b42dd738df8c2716` |
| `rgss-xp/System.rxdata` | 7983 | `75824e963810c492ab9b807cf9f89a7b1f204eed72aab54972287a9a861b609f` |
| `rgss-xp/Map003.rxdata` | 4753 | `0282f07793f344736e57977b620c31009882bb59f6b0846875706d5bf1e040c1` |
| `rgss-xp/Game.ini` | 115 | `28d1eef57d12a39c487e534c2bc6a64a9deae3628fe5f07b338b188aa180e5fc` |
| `rgss-xp/Game.rxproj` | 10 | `a3663b8cdf844ddd02ac5421d83dab67cc6ba16c9706a6e80513ea5fd52c6e8a` |

## `rgss-xp-microquest` — an English installation

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `rgss-xp-microquest/Actors.rxdata` | 13609 | `d47c61388a5381554e2fd41ec355ac91462e888322c74cdfbe0ecea4fdafc6b7` |
| `rgss-xp-microquest/Classes.rxdata` | 3314 | `366f860ca7156c03b4117fd213519eea636f2b7b446bfb099b6efca44692fa5d` |
| `rgss-xp-microquest/Enemies.rxdata` | 7000 | `8ec94be176b8bb33c16aafde5101bc55c8935a0d479a97af8e43f2546307cb9e` |
| `rgss-xp-microquest/Items.rxdata` | 6729 | `b8868e2a3d68545374ed2c580a3ed96ddc307a28b47fa0e9b336619275e6a7ff` |
| `rgss-xp-microquest/Skills.rxdata` | 15997 | `de824008bc4abdbca6b1807a31fa6183bf7f8eb8954de2542a8d2f7b42ff08b9` |
| `rgss-xp-microquest/States.rxdata` | 2207 | `708662c054a65a5bf4bbc33bddb33620e601d1b03cc14fc12a4950f432c4680b` |
| `rgss-xp-microquest/System.rxdata` | 2095 | `badbd008e5669254360178b1ff417e71e3da3e87b6c2176f52a9ad0d2266e168` |
| `rgss-xp-microquest/Armors.rxdata` | 4489 | `462cf749af3d9e822edf4b0bddd5f38b50806fd46b092f0c79bfe9184cfcbc2c` |
| `rgss-xp-microquest/Animations.rxdata` | 155213 | `dc61147239e7ca91bbf6c7eb95eb11038662301661c6368976fbc4ea3b129b78` |
| `rgss-xp-microquest/Game.ini` | 126 | `9580697145f9149989aa38d77382af32a22a63ccee380c3641bf8372d2d25666` |
| `rgss-xp-microquest/Game.rxproj` | 10 | `a3663b8cdf844ddd02ac5421d83dab67cc6ba16c9706a6e80513ea5fd52c6e8a` |

## `kirikiri` — a game that is not an engine this repository reads

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `kirikiri/Game.ini` | 289 | `f847dbd0a18474eed610984badb600c5dc2653c56bf2749645cc4187eebb6187` |

This is a KiriKiri game and **not** a Wolf game. It was offered as a Wolf game and
it is worth keeping here for the reason it is not one: it carries the settings a
KiriKiri runtime reads (`SoftModeFlag`, `FrameSkip`, `SEandBGM`), its data folder
holds a version directory, and it has no archive of any kind, so there is nothing
in it anywhere holding a `WOLFM` header. A folder full of what a Wolf game would
have is not a Wolf game, and a detector that answers either way rather than
refusing has guessed.

# RM2K fixture

## `rm2k-dragon-destiny` — a real RPG Maker 2000/2003 game

A game made in the editor this repository's parser was written against, with
seven hundred and forty three maps and a database of four hundred and sixteen
kilobytes. **No executable, DLL, save, image, audio or script is imported** — the
four files below are the plain data files the engine itself reads and writes.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `rm2k-dragon-destiny/RPG_RT.ldb` | 416600 | `c469bed202e7e56ceaa25e21646b4f9536fb1cde538516a2c47ce14a87622712` |
| `rm2k-dragon-destiny/RPG_RT.lmt` | 57214 | `b566835eed9218aa99eba88a9e4ad1eb2359eb546d68409216531ae524f0d98c` |
| `rm2k-dragon-destiny/Map0001.lmu` | 1227 | `995dd70cfe21d8d4831637f80b235b04774a627f67bfe1fb488c87f3661a6ead` |
| `rm2k-dragon-destiny/Map0100.lmu` | 1230 | `ca9db5abc386233530c10b54d18a3a6e4a1e381c7c13bd3151ed214e03fafae5` |

### Why this matters

The other RM2K fixture in this repository is EasyRPG's test game: two hundred and
ten bytes of database, six bytes of map tree, five maps. It is the right size for
testing a parser's framing and it is the wrong size for testing a parser at all,
because every count, length and index in a game of that size fits in a byte and
would not in this one.

`test_real_rm2k_data.cs` reads this game's database, its map tree and two of its
maps. What it claims is that the parser reads them. What it does **not** claim is
that the parser understands an actor, an event, a page or a chipset. A database
read as a dictionary of chunks is a database read; it is not a game.

One name in it was measured rather than remembered: a `.lmu` holds an
`LcfMapUnit`, not an `LcfMap`. The parser's own test game agrees, but the name
was wrong in a first draft of this test and only the file itself settled it.
