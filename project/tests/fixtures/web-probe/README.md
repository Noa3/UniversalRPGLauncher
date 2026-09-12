# Synthetic MV/MZ plugin probe fixtures

These are small project-owned JavaScript fixtures, not RPG Maker games or vendor core scripts. The core-named files are filename markers only and explicitly say so. They must never be used as evidence of a complete MV/MZ boot.

Both fixtures check plugin parameters, scoped script metadata, zero-delay timers and an animation callback. The MZ fixture additionally checks PluginManager command invocation with the correct receiver.

Node semantic check (does not test C#/Jint/Godot):

```sh
node scripts/test-web-probe-fixtures.mjs
```

Actual Godot/Jint developer probe, after a successful .NET build:

```sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "project/tests/fixtures/web-probe/mz" --engine mz --execute-plugins --frames 3
```

Use the mv directory and `--engine mv` for the other fixture. The game directory stays read-only; output goes to the app-owned `user://web-plugin-probes` directory. See `docs/MV_MZ_TESTING.md` for scope, safety and report interpretation.
