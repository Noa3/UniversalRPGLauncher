# Original core-startup test fixture

`MVPluginManager.js` is an unchanged test-only copy of the MIT-licensed original MV PluginManager from `rpgtkoolmv/corescript`, path `js/rpg_managers/PluginManager.js`.

Upstream source: https://github.com/rpgtkoolmv/corescript/blob/master/js/rpg_managers/PluginManager.js
Content Git blob SHA: `491e9fa141ccfc6422dd03de865a6dc91bbf49ce`.
Retrieved 2026-09-12. Copyright (c) 2015 KADOKAWA CORPORATION./YOJI OJIMA. Full MIT notice: `LICENSE.MV` alongside this file.

The fixture is NOT a shipped replacement engine or a full game. Tests supply an explicit synthetic Array.contains utility. Their MZ scheduling variant is synthetic, not the complete original MZ core. No proprietary game data or RTP asset is included.

The core-stage loader reads production libraries/cores/config from the user's game rather than substituting this test file. Full rendering, audio, main.js startup and playability remain outside these tests.
