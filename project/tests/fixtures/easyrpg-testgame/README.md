# Real LCF parser fixtures

These parser-only fixtures are copied from the public [EasyRPG/TestGame](https://github.com/EasyRPG/TestGame) repository at commit `4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313`.

The source repository includes a GPL-3.0 `COPYING` file and `AUTHORS.md` with provenance for its bundled assets. UniversalRPG keeps only the LCF data files below plus one chipset image; no executable, DLL, script, audio, RTP asset, or save file is imported. They are read as untrusted bytes by parser and rendering tests and are never executed.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `rm2000/RPG_RT.ldb` | 210227 | `9728e783c05540badd7d2939916e02b789d4079bd9f85759d9dbe67fda1ee123` |
| `rm2000/RPG_RT.lmt` | 6321 | `72329f51dbb1bc667c07fbcba5358e5717e6eca6be9ec50f2f235d2bce0a8c64` |
| `rm2000/Map0001.lmu` | 8544 | `087ce023e23d831df0c0e60e4b8a3bd8e507c2e3809e17e564fcae7b4b9a8a0d` |
| `rm2003/RPG_RT.ldb` | 416513 | `086dadfeeac35cb72a40742a759da60ec5377b0f06ab332d45783200999bb7a5` |
| `rm2003/RPG_RT.lmt` | 1734 | `35ff18ceda8ce13a613ad7f9088a3a7bfa88a86505833c7e953fd07b996cc1e0` |
| `rm2003/Map0001.lmu` | 8488 | `7a18ef96def5666eb0b8e76ae271d01cb2d23706f51e8bbc9a5f1848e2ba5825` |
| `rm2000/ChipSet/World.png` | 35812 | `7d3f28e1d825b254c6b5d8afca36e31575e54d9ab58a98678d89ab3573ba6855` |
| `rm2000/rendered/Map0001.png` | 19592 | `060e46f5c16472b40e0fc12315e36113beca527bb106baee1dcae7bb1d0f602f` |
| `rm2000/rendered/Map0001.legacy.png` | 18859 | `99d94f22bf16c15f7f13865871ca70a19d5eea3f7eeb3712c9155fd27edc5e1a` |
| `rm2000/CharSet/Chara1.png` | 18785 | `24442b6157d3f609fe3d6e588f23a42f012d3180c2c1729b2af0ddc188ce0cfc` |

Raw source paths are pinned to the same commit:

- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/RPG_RT.ldb>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/RPG_RT.lmt>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/Map0001.lmu>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/RPG_RT.ldb>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/RPG_RT.lmt>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/Map0001.lmu>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/ChipSet/World.png>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/CharSet/Chara1.png>

## Why the chipset image is included

`rm2000/ChipSet/World.png` was added for the chipset PNG decoder. It is the chipset that `rm2000/RPG_RT.ldb` actually uses: the map resolves `chipset_id = 1` and that entry's `chipset_name` is `World`, so the fixture is the real chipset for the pinned LDB, not a stand-in.

It is a passive image. It is parsed as untrusted bytes by `Rm2kChipsetBitmap` and never executed, and it is the only image kept. The earlier statement that no image is imported was true when the project only parsed LCF data.

The file also independently confirms two verified constants: it is 480 by 256 pixels, which matches the `30 * 16` tile grid derived from the chipset formulas in K-095, and it is an 8 bit paletted PNG, matching the Player's `Material::Chipset` spec and the paletted loader in `src/image_png.cpp`.

`rm2000/CharSet/Chara1.png` was added for the charset geometry in K-101. It independently confirms the verified cell size: 288 by 384 pixels is exactly four 72 pixel cells per row and three 128 pixel cells down, which is what `GetCharacterRect` requires with its `(index % 4, index / 4)` split and its `24 * 3` by `32 * 4` cell. Like the chipset it is a passive 8 bit paletted PNG.

The upstream project may change its contents or licensing in later commits. Update the pinned commit, hashes, and this note together if fixtures are refreshed.

## The rendered golden image

`rm2000/rendered/Map0001.png` is **not** from upstream. It is this project's initial 320x240 composition for the pinned map, chipset and charset, with the starting party and reference-derived page direction/pattern rules. It is a development regression baseline, not native RPG_RT/EasyRPG visual-parity evidence. The test compares every RGBA byte with zero tolerance.

The previous capture is preserved as `Map0001.legacy.png`. Refreshing the baseline was justified by pinned Player `0de2a9ab466a133ac6e192a84bd761a1d81f5f14` RefreshPage and liblcf `6854310c3432e553fd4ae672ce861899c80c3bd0`: normal SaveMapEventBase animation starts at frame1, page pattern is LMU0x18 (not translucent0x19), and fixed/spin pages apply their authored pattern. Independent native runtime tests compare opaque real-charset pixels with the reference cell geometry, including file-based initial fixed and normal pages. The refreshed capture differs at5777 pixels; a separate raw-LMU coordinate/pixel inspection found zero differences outside the22 event rectangles and the starting-hero rectangle. Both images were visually inspected. The old README hash/size did not match the old file; the legacy row above records its measured bytes/hash rather than repeating that stale metadata.

The pinned map is a single test room: 20 by 15 tiles, a lower layer of block D and E tiles, an upper layer of block F tiles that are fully transparent in that chipset, and no animated autotile. It renders to 13 distinct colours and its floor and wall tiles fill every pixel, so a transparency check is not meaningful on it and the transparency rule is verified per tile instead.
