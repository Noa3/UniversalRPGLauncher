# Interface Localization

English (`en`) is the default and fallback. The app currently includes `de`, `es`, `fr`, `ja`, `ko`, and `zh_CN`.

## Add a Language

1. Copy `locale/en.po` to `locale/<code>.po`.
2. Set the PO `Language` header and translate every `msgstr`.
3. Add the file to `internationalization/locale/translations` in `project.godot`.
4. Add `("<code>", "Native language name")` to `InterfaceLocales` in `app/ui/Main.cs`.
5. Run Godot with `--language <code>` and test narrow/mobile layouts.
6. Verify placeholders such as `{count}`, `{engine}`, and `{reason}` remain unchanged.

Use stable message IDs in code (`tr("ACTION_RESCAN")`), not source-language sentences. Use `tr_n()` for count-dependent text. New visible text must have an English catalog entry before merge.

## Fonts

`assets/fonts/NotoSansCJKsc-Regular.otf` provides bundled Latin, Japanese, Korean, and Simplified Chinese coverage. Its SIL OFL 1.1 license is included beside the font.

When adding scripts not covered by this font, add a licensed fallback font and test exported desktop and mobile builds. Do not rely only on host system fonts.

## Game Text

Interface localization and game translation are separate. Imported game text may use legacy encodings and must pass through the runtime text decoder. Planned game translation packs use non-destructive VFS overlays; see `idea.md`.
