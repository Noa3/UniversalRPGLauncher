# Local Godot Editors

Godot 4.7.2 stable .NET editor packages were downloaded from the official `godotengine/godot-builds` GitHub release on 2026-08-20.

| Platform | Package | SHA-256 |
|---|---|---|
| Linux x86-64 | `Godot_v4.7.2-stable_mono_linux_x86_64.zip` | `129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247` |
| Windows x86-64 | `Godot_v4.7.2-stable_mono_win64.zip` | `a2a48473a7414c5f19fab690518caebb738c09ef9601f6bd2388676a7f53b3c0` |

Installed paths:

```text
tools/godot/editors/4.7.2/linux-x86_64/
tools/godot/editors/4.7.2/windows-x86_64/
```

Editor binaries are ignored by Git. Download sources:

- <https://github.com/godotengine/godot-builds/releases/tag/4.7.2-stable>
- <https://godotengine.org/download/archive/4.7.2-stable/>

The .NET editor requires a separate 64-bit .NET SDK. The project currently contains GDScript only, so standard and .NET Godot editors can both open it.
