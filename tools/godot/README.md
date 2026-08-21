# Local Godot Editors

Godot 4.7.2 stable .NET editor package metadata is recorded here. Editor binaries themselves are intentionally excluded from repository/ZIP artifacts.

| Platform | Package | SHA-256 |
|---|---|---|
| Linux x86-64 | `Godot_v4.7.2-stable_mono_linux_x86_64.zip` | `129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247` |
| Windows x86-64 | `Godot_v4.7.2-stable_mono_win64.zip` | `a2a48473a7414c5f19fab690518caebb738c09ef9601f6bd2388676a7f53b3c0` |

Expected optional local paths:

```text
tools/godot/editors/4.7.2/linux-x86_64/
tools/godot/editors/4.7.2/windows-x86_64/
```

Editor binaries are ignored by Git. Download sources:

- <https://github.com/godotengine/godot-builds/releases/tag/4.7.2-stable>
- <https://godotengine.org/download/archive/4.7.2-stable/>

The .NET editor requires a separate 64-bit .NET SDK. The canonical application/runtime is currently GDScript, while a partial experimental C# port is also present. Use the .NET editor when validating that port; the standard editor is sufficient for the canonical GDScript validation suite.
