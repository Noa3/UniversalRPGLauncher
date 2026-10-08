using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Plugins;

/// <summary>
/// Saving and loading a running MZ game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shape is the engine's own, and every piece of it is
/// measured elsewhere in this repository:</strong> the contents in
/// <see cref="UniversalRPG.Web.MzSaveContents"/>, the file in
/// <see cref="UniversalRPG.Web.MzSaveStore"/>, and the two together are what
/// <c>DataManager.saveGame</c> and <c>DataManager.loadGame</c> do.
/// </para>
/// <code>
/// DataManager.saveGame = function(savefileId) {
///     const contents = this.makeSaveContents();
///     const saveName = this.makeSavename(savefileId);
///     return StorageManager.saveObject(saveName, contents).then(() => {
///         this._globalInfo[savefileId] = this.makeSavefileInfo();
///         this.saveGlobalInfo();
///         return 0;
///     });
/// };
/// DataManager.loadGame = function(savefileId) {
///     const saveName = this.makeSavename(savefileId);
///     return StorageManager.loadObject(saveName).then(contents => {
///         this.createGameObjects();
///         this.extractSaveContents(contents);
///         this.correctDataErrors();
///         return 0;
///     });
/// };
/// </code>
/// <para>
/// <strong>And a load happens in place.</strong> The engine's
/// <c>extractSaveContents</c> assigns ten whole objects to the globals, and
/// this runtime cannot do that without breaking the reference its own command
/// reader holds -- <strong>so it fills the containers the facts already own,
/// which is the same state by a different route.</strong> <strong>And that
/// is the difference between a load and the change that was measured to break
/// a test earlier in this repository's history.</strong>
/// </para>
/// </remarks>
public sealed partial class MzEngineRuntime
{
    private string? _saveDirectory;

    /// <summary>
    /// Where this game's saves live.
    /// </summary>
    /// <remarks>
    /// <strong>And the default is the engine's own place</strong> --
    /// measured: <c>StorageManager.fileDirectoryPath</c> is
    /// <c>path.join(path.dirname(process.mainModule.filename), "save/")</c>,
    /// which is the game's own folder. <strong>And it is settable, because
    /// an imported game is someone else's folder:</strong> it may be
    /// read-only and it may be shared, and a launcher that wrote into it
    /// would change data it does not own. <strong>The file names inside are
    /// the engine's own either way, so the saves stay portable.</strong>
    /// </remarks>
    public string SaveDirectory
    {
        get => _saveDirectory ?? Path.Combine(GameDirectory, "save");
        set => _saveDirectory = value;
    }

    /// <summary>
    /// What went wrong the last time a save or a load was asked for, or an
    /// empty string.
    /// </summary>
    public string SaveProblem { get; private set; } = "";

    /// <summary>
    /// The slots that have a save, which is what a load screen lists.
    /// </summary>
    public IReadOnlyList<int> Spielstaende() =>
        UniversalRPG.Web.MzSaveStore.Slots(SaveDirectory);

    /// <summary>
    /// Whether a slot has a save, which is
    /// <c>DataManager.savefileExists</c>.
    /// </summary>
    public bool SpielstandVorhanden(int pSlot) =>
        UniversalRPG.Web.MzSaveStore.Exists(
            SaveDirectory, UniversalRPG.Web.MzSaveStore.SlotName(pSlot));

    /// <summary>
    /// Writes the running game into a slot.
    /// </summary>
    /// <param name="pSlot">The slot, from one to twenty.</param>
    /// <returns>A report of what was written and what was not.</returns>
    /// <remarks>
    /// <strong>And it names the entries it cannot write.</strong> The
    /// engine's save has ten, and this runtime has six -- <c>screen</c>,
    /// <c>timer</c>, <c>actors</c> and <c>party</c> are not modelled yet.
    /// <strong>A save that silently dropped the party would be a save that
    /// loses a player's gold, and a report is the honest answer.</strong>
    /// </remarks>
    public string Speichern(int pSlot)
    {
        SaveProblem = "";

        if (State != PluginRuntimeState.Running)
        {
            SaveProblem = $"the game is {State} and not running, so there is "
                + "nothing to save";
            return SaveProblem;
        }

        if (pSlot < 1 || pSlot > UniversalRPG.Web.MzSaveStore.MaxSavefiles)
        {
            SaveProblem = $"slot {pSlot} is not a slot this engine offers: "
                + $"measured, DataManager.maxSavefiles returns "
                + $"{UniversalRPG.Web.MzSaveStore.MaxSavefiles} and "
                + "emptySavefileId starts at one";
            return SaveProblem;
        }

        var (inhalt, fehlend) = UniversalRPG.Web.MzSaveContents.Capture(
            Facts, pSlot, Frames);

        var name = UniversalRPG.Web.MzSaveStore.SlotName(pSlot);
        if (!UniversalRPG.Web.MzSaveStore.Save(
                SaveDirectory, name, inhalt, out var problem))
        {
            SaveProblem = problem;
            return SaveProblem;
        }

        // **And the file info is written too, because that is what a load
        // screen draws.** Measured: `saveGame` sets
        // `this._globalInfo[savefileId] = this.makeSavefileInfo()` and then
        // calls `saveGlobalInfo`, so the info file is an array indexed by
        // slot -- and a load screen can list twenty slots without reading
        // twenty saves.
        var infoProblem = WriteGlobalInfo(pSlot);

        var bericht = $"saved slot {pSlot} to "
            + $"{UniversalRPG.Web.MzSaveStore.PathOf(SaveDirectory, name)}, "
            + $"{Facts.Switches.Count} switch(es), "
            + $"{Facts.Variables.Count} variable(s), "
            + $"{Facts.SelfSwitches.Count} self switch(es), and the player at "
            + $"{Facts.Player.MapId}/{Facts.Player.X}/{Facts.Player.Y}";

        if (fehlend.Count > 0)
        {
            bericht += $"; this runtime cannot write {string.Join(", ", fehlend)}"
                + " yet, and it says so rather than writing something made up";
        }

        if (infoProblem.Length > 0)
        {
            bericht += $"; and the file info could not be written: {infoProblem}";
        }

        return bericht;
    }

    /// <summary>
    /// Reads a slot back into the running game.
    /// </summary>
    /// <param name="pSlot">The slot, from one to twenty.</param>
    /// <returns>A report of what was applied.</returns>
    /// <remarks>
    /// <strong>And the map is loaded before the player is put on it</strong>,
    /// because a save from another map carries that map's id -- <strong>and
    /// a reader that moved the player first would put them on a map that is
    /// not loaded.</strong>
    /// </remarks>
    public string Laden(int pSlot)
    {
        SaveProblem = "";

        if (State != PluginRuntimeState.Running)
        {
            SaveProblem = $"the game is {State} and not running, so there is "
                + "nothing to load into";
            return SaveProblem;
        }

        var name = UniversalRPG.Web.MzSaveStore.SlotName(pSlot);
        if (!UniversalRPG.Web.MzSaveStore.TryLoad(
                SaveDirectory, name, out var inhalt, out var problem))
        {
            SaveProblem = problem;
            return SaveProblem;
        }

        if (!UniversalRPG.Web.MzSaveContents.TryRestore(
                inhalt!, out var geladen, out var fehler))
        {
            SaveProblem = fehler;
            return SaveProblem;
        }

        var save = geladen!;

        // **And the map first**, because the player's tile means nothing
        // until the map it is on is the map that is loaded.
        var karteGeladen = "";
        if (save.MapId > 0 && save.MapId != CurrentMapId)
        {
            if (!GoTo(save.MapId))
            {
                SaveProblem = $"the save is from map {save.MapId} and this "
                    + $"runtime cannot load it: {PaintReason}";
                return SaveProblem;
            }

            karteGeladen = $" and map {save.MapId} was loaded";
        }

        // **And the containers the facts already own are filled**, which is
        // the same state the engine reaches by assigning whole objects to
        // its globals.
        Facts.Switches.Clear();
        foreach (var paar in save.Switches)
        {
            Facts.Switches[paar.Key] = paar.Value;
        }

        Facts.Variables.Clear();
        foreach (var paar in save.Variables)
        {
            Facts.Variables[paar.Key] = paar.Value;
        }

        Facts.SelfSwitches.Clear();
        foreach (var paar in save.SelfSwitches)
        {
            Facts.SelfSwitches[paar.Key] = paar.Value;
        }

        // **And the figures are put back where the save left them.** The
        // engine's own `_events` is a sparse array indexed by event id, so
        // an id the save has no entry for is an event that never moved --
        // and it is left alone.
        var bewegt = 0;
        foreach (var paar in save.Figures)
        {
            if (Facts.Characters.TryGetValue(paar.Key, out var figur)
                && figur != null)
            {
                figur.SetLocation(paar.Value.X, paar.Value.Y, paar.Value.Direction);
                bewegt++;
            }
        }

        Facts.Player.StandAt(
            save.MapId > 0 ? save.MapId : CurrentMapId,
            save.PlayerX,
            save.PlayerY,
            save.PlayerDirection);

        // **And no camera has to be moved**, and that is not an omission:
        // this runtime does not model a display position at all. The
        // engine's  and  live on , and a
        // reader that pretended to move them would be moving something that
        // is not there.
        var bericht = $"loaded slot {pSlot}: {Facts.Switches.Count} "
            + $"switch(es), {Facts.Variables.Count} variable(s), "
            + $"{Facts.SelfSwitches.Count} self switch(es), {bewegt} figure(s) "
            + $"put back, and the player at {Facts.Player.MapId}/"
            + $"{Facts.Player.X}/{Facts.Player.Y}{karteGeladen}";

        if (save.Missing.Count > 0)
        {
            bericht += $"; the save does not carry "
                + $"{string.Join(", ", save.Missing)}, which is what it was "
                + "written from";
        }

        return bericht;
    }

    /// <summary>
    /// The playtime as the engine writes it, which is
    /// <c>Game_System.playtimeText</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And it is measured and not guessed:</strong>
    /// <c>playtime</c> is <c>Math.floor(Graphics.frameCount / 60)</c>, and
    /// <c>playtimeText</c> is <c>hh:mm:ss</c> with each field padded to two
    /// digits.
    /// </remarks>
    private string PlaytimeText()
    {
        var sekunden = Frames / 60;
        var stunde = sekunden / 60 / 60;
        var minute = sekunden / 60 % 60;
        var rest = sekunden % 60;
        return stunde.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + minute.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + rest.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes the info file, which is <c>DataManager.saveGlobalInfo</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And the file is an array indexed by slot</strong>, measured:
    /// <c>saveGame</c> sets <c>this._globalInfo[savefileId]</c> and saves the
    /// whole array. <strong>So an entry that is not there stays a hole, and a
    /// reader that packed the entries together would put slot seven's
    /// playtime under slot two.</strong>
    /// </remarks>
    private string WriteGlobalInfo(int pSlot)
    {
        var ordner = SaveDirectory;
        var global = new UniversalRPG.Web.MzValue(UniversalRPG.Web.MzKind.Array);

        if (UniversalRPG.Web.MzSaveStore.TryLoad(
                ordner,
                UniversalRPG.Web.MzSaveStore.GlobalName,
                out var vorhanden,
                out _)
            && vorhanden != null
            && vorhanden.Kind == UniversalRPG.Web.MzKind.Array)
        {
            foreach (var eintrag in vorhanden.Items)
            {
                global.Items.Add(eintrag);
            }
        }

        while (global.Items.Count <= pSlot)
        {
            global.Items.Add(
                new UniversalRPG.Web.MzValue(UniversalRPG.Web.MzKind.Null));
        }

        global.Items[pSlot] = UniversalRPG.Web.MzSaveContents.Info(
            GameTitle,
            Array.Empty<int>(),
            Array.Empty<string>(),
            PlaytimeText(),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        return UniversalRPG.Web.MzSaveStore.Save(
            ordner,
            UniversalRPG.Web.MzSaveStore.GlobalName,
            global,
            out var problem)
                ? ""
                : problem;
    }
}
