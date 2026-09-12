# Interface Localization

> **Last reviewed:** 2026-09-09

English (`en`) is the default and fallback. The app currently includes `de`, `es`, `fr`, `ja`, `ko`, and `zh_CN`.

All paths below are repository-relative. The Godot project lives under `project/`.

## Add a Language

1. Copy `project/locale/en.po` to `project/locale/<code>.po`.
2. Set the PO `Language` header and translate every `msgstr`.
3. Add the file to `internationalization/locale/translations` in `project/project.godot`.
4. Add `("<code>", "Native language name")` to `InterfaceLocales` in `project/app/ui/Main.cs`.
5. Run Godot against the project with `--language <code>` and test narrow/mobile layouts.
6. Verify placeholders such as `{count}`, `{engine}`, and `{reason}` remain unchanged.

Use stable message IDs in code, not source-language sentences. New visible launcher text should have an English catalog entry before merge.

## Fonts

`project/assets/fonts/NotoSansCJKsc-Regular.otf` provides bundled Latin, Japanese, Korean, and Simplified Chinese coverage. Its SIL OFL 1.1 license is stored at `project/assets/fonts/OFL.txt`.

When adding scripts not covered by the font, use a properly licensed fallback and test actual exported targets. Do not depend only on host system fonts.

## Game Text

Launcher localization and imported-game translation are separate systems.

Imported game text may use legacy encodings and must pass through the engine/runtime text decoding boundary. Translation/mod packs should be non-destructive VFS overlays tied to game/version identity.

Do not rewrite original game archives merely to translate UI/text.

See `idea.md` and `docs/COMPATIBILITY.md` for the long-term overlay policy.
