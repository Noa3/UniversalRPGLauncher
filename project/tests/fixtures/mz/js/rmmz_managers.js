// RPGMZ runtime signature placeholder.
// The real rmmz_managers.js is 83 KB of a game's own code and is not imported here.
//
// **A real file's shape, for the command extractor, written as data.** The
// reader in `MzScriptCommandReader` never executes any of it: it looks for
// `PluginManager`-style registrations and the command names beside them.
//
// The real engine registers its own commands in `rmmz_managers.js` through
// `Scene_Boot.prototype.setScene` and its managers; a plugin registers
// through `PluginManager.registerCommand(name, commandName, fn)`.

const $plugins = {};

PluginManager.registerCommand = function (pluginName, commandName, fn) {
    const key = pluginName + ":" + commandName;
    $plugins[key] = fn;
};

PluginManager.callCommand = function (pluginName, args) {
    const fn = $plugins[pluginName + ":" + args[0]];
    if (fn) {
        fn.apply(this, args.slice(1));
    } else {
        throw new Error("command not found: " + args[0]);
    }
};

Game_Party.prototype.addActor = function (actorId) {
    $gameParty.addActor(actorId);
};

Window_Base.prototype.drawLine = function (x, y, width, color) {
    // body
};

Scene_Menu.prototype.commandOpen = function () {
    // body
};

Window_ItemList.prototype.updateHelp = function () {
    // body
};
