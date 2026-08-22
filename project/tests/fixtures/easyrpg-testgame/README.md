# Real LCF parser fixtures

These parser-only fixtures are copied from the public [EasyRPG/TestGame](https://github.com/EasyRPG/TestGame) repository at commit `4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313`.

The source repository includes a GPL-3.0 `COPYING` file and `AUTHORS.md` with provenance for its bundled assets. UniversalRPG keeps only the six LCF data files below; no executable, DLL, script, image, audio, RTP asset, or save file is imported. They are read as untrusted bytes by parser tests and are never executed.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `rm2000/RPG_RT.ldb` | 210227 | `9728e783c05540badd7d2939916e02b789d4079bd9f85759d9dbe67fda1ee123` |
| `rm2000/RPG_RT.lmt` | 6321 | `72329f51dbb1bc667c07fbcba5358e5717e6eca6be9ec50f2f235d2bce0a8c64` |
| `rm2000/Map0001.lmu` | 8544 | `087ce023e23d831df0c0e60e4b8a3bd8e507c2e3809e17e564fcae7b4b9a8a0d` |
| `rm2003/RPG_RT.ldb` | 416513 | `086dadfeeac35cb72a40742a759da60ec5377b0f06ab332d45783200999bb7a5` |
| `rm2003/RPG_RT.lmt` | 1734 | `35ff18ceda8ce13a613ad7f9088a3a7bfa88a86505833c7e953fd07b996cc1e0` |
| `rm2003/Map0001.lmu` | 8488 | `7a18ef96def5666eb0b8e76ae271d01cb2d23706f51e8bbc9a5f1848e2ba5825` |

Raw source paths are pinned to the same commit:

- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/RPG_RT.ldb>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/RPG_RT.lmt>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2000/Map0001.lmu>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/RPG_RT.ldb>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/RPG_RT.lmt>
- <https://raw.githubusercontent.com/EasyRPG/TestGame/4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313/TestGame-2003/Map0001.lmu>

The upstream project may change its contents or licensing in later commits. Update the pinned commit, hashes, and this note together if fixtures are refreshed.
