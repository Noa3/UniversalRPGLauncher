# Native data compatibility fixture

`MVDataManager.excerpt.js` contains four provenance comments followed by unchanged lines 1–195 of `js/rpg_managers/DataManager.js` from `rpgtkoolmv/corescript` commit `9875c94cb92c655f4ff919458740bf1eb503ee0f`. The upstream full file blob is `9ddcc62f8324a29f202b376c77eaa63d871c1016`.

Source: https://github.com/rpgtkoolmv/corescript/blob/9875c94cb92c655f4ff919458740bf1eb503ee0f/js/rpg_managers/DataManager.js

License: MIT; copyright (c) 2015 KADOKAWA CORPORATION./YOJI OJIMA. The complete notice is in `LICENSE.MV` (upstream license blob `8365e056d2a2b297e7c6b95abbac671d0a860609`). This permission concerns this published core source, not proprietary game assets or RTPs.

This is a **test-only excerpt**, not a production engine replacement or full game. The Node test uses the actual original loading and metadata methods with the production URPG local-data adapter. Native storage, timers and unrelated image/option/retry dependencies are explicitly simulated. No vendor rendering, gameplay or full boot is being certified.

Run from the repository root:

```sh
node scripts/test-native-game-data.mjs
```

The test exercises all 14 database requests, original note metadata, lazy map data, a plugin extending the database list, and original failure behavior. It also tests the adapter's callback/abort/state semantics. C# integration coverage lives in `project/tests/core/TestNativeGameData.cs` and must be run under the actual Jint/Godot validation path.
