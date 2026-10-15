using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Web;

namespace UniversalRPG.Mz;

/// <summary>
/// And the battle's own page, run on the game's own commands.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a second interpreter and not a special
/// one.</strong> -- <strong>A troop page is a list of
/// <c>MzCommandEntry</c> like any map page</strong>, -- <strong>and
/// the same reader runs it</strong>, -- <strong>and a battle
/// interpreter of its own would be a second dialect for one
/// command list.</strong>
/// </para>
/// <para>
/// <para>
/// <strong>And the battle reads the troop's own conditions
/// itself</strong>, -- <strong>because <c>command601</c> through
/// <c>command603</c> are written by the game on the page and
/// evaluated by the game there.</strong> -- <strong>And this
/// runner does not guess at them.</strong>
/// </para>
/// </remarks>
public sealed class MzBattleRunner
{
    private readonly MzBranchFacts _facts;
    private readonly string _gameDirectory;

    private MzEventRunner? _seite;
    private List<MzCommandEntry>? _commands;
    private MzZugfolge? _zugfolge;
    private int _truppenId = -1;

    /// <summary>Builds a runner over one state.</summary>
    public MzBattleRunner(
        MzBranchFacts pFacts,
        string pGameDirectory)
    {
        _facts = pFacts ?? throw new ArgumentNullException(
            nameof(pFacts));
        _gameDirectory = pGameDirectory ?? throw new ArgumentNullException(
            nameof(pGameDirectory));
    }

    /// <summary>And whether a page is loaded.</summary>
    public bool Laeuft => _seite != null;

    /// <summary>
    /// And it loads the troop's first page, or says why not.
    /// </summary>
    /// <param name="pBank">The parsed database.</param>
    /// <param name="pTroopId">The troop the encounter named.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether a page is running.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the first page, and the measurement says that is
    /// the whole of it.</strong> --
    /// <strong>All one hundred and five troops of the measured game
    /// carry exactly one page each</strong>, -- <strong>and exactly one
    /// carries a condition at all</strong>, --
    /// <strong>and with one page per troop a condition has nothing to
    /// choose between.</strong>
    /// </para>
    /// <para>
    /// <strong>And I wrote that taking the first page was "a decision
    /// and not a guess" and that was weaker than the
    /// truth.</strong> -- <strong>On this game it is not a decision:
    /// there is no second page.</strong> -- <strong>And a runner that
    /// meets a game with several pages must read their conditions,
    /// which it does not do yet.</strong>
    /// </para>
    /// </remarks>
    public bool Lade(
        MzDataFile pBank,
        int pTroopId,
        out string pFehler)
    {
        pFehler = "";
        _commands = null;
        _seite = null;
        _zugfolge = null;

        // **And the bank that was handed in is the bank that gets read.**
        // This passed null while holding the parsed database, and the
        // decoder dereferenced it on its first line -- so every battle in a
        // project with a troop page crashed the frame it started.
        if (!MzTroopPageDecoder.TryDecode(
                pBank, pTroopId, out var befehle, out var warum))
        {
            pFehler = "troop " + pTroopId + ": " + warum;
            return false;
        }

        _commands = befehle;
        _truppenId = pTroopId;

        // **Und  die  Seite  laeuft  auf  demselben  Zustand** --
        // **denn  eine  Kampfseite  hat  keine  Kartenposition.**
        _seite = new MzEventRunner();

        // Load Enemies.json and Classes.json for agility values
        var enemiesPath = Path.Combine(_gameDirectory, "data", "Enemies.json");
        var classesPath = Path.Combine(_gameDirectory, "data", "Classes.json");

        MzDataFile? enemiesData = null;
        MzDataFile? classesData = null;

        if (File.Exists(enemiesPath))
        {
            enemiesData = MzDataFile.Read("data/Enemies.json", File.ReadAllBytes(enemiesPath));
        }
        else
        {
            enemiesData = MzDataFile.ReadText("data/Enemies.json", "[]");
        }
        if (File.Exists(classesPath))
        {
            classesData = MzDataFile.Read("data/Classes.json", File.ReadAllBytes(classesPath));
        }
        else
        {
            classesData = MzDataFile.ReadText("data/Classes.json", "[]");
        }

        // Build turn order from party and troop
        var kampf = _facts.Kampf;
        _zugfolge = new MzZugfolge(
            _facts.Actors,
            _facts.PartyMembers,
            kampf.Gegner,
            enemiesData,
            classesData);

        return true;
    }

    /// <summary>
    /// And who acts now, by agility order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is the agility order</strong>, --
    /// <strong>because the engine sorts all battlers by agi
    /// before each round.</strong>
    /// </para>
    /// <para>
    /// <strong>And it returns a negative value when nobody acts</strong>, --
    /// <strong>which is the round's end and not an error.</strong>
    /// </para>
    /// </remarks>
    public MzZugfolge.Eintrag? WerIstDran()
    {
        if (_zugfolge == null)
        {
            return null;
        }
        return _zugfolge.Naechster();
    }

    /// <summary>
    /// And one frame of the battle, and it is the same reader.
    /// </summary>
    /// <returns>Whether the battle page carries on.</returns>
    public bool Schritt()
    {
        if (_seite == null)
        {
            return false;
        }

        if (_commands == null || _commands.Count == 0)
        {
            return false;
        }

        var result = _seite.Run(
            _commands,
            _facts,
            0, // No map ID for battle
            -1, // No event ID for battle
            new MzRandom());

        if (result.Stopped == MzStep.Finished)
        {
            _commands = null;
            return false;
        }

        // If waiting or frozen, continue
        return true;
    }

    /// <summary>Starts a new round, resetting turn order.</summary>
    public void NeueRunde()
    {
        _zugfolge?.NeueRunde();
    }
}