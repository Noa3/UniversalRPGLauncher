using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The three outcomes a battle can end in, and the numbers the engine gives
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these three numbers are the whole of <c>601</c>,
/// <c>602</c> and <c>603</c>.</strong>
/// </para>
/// <code>
/// command601() { if (this._branch[this._indent] !== 0) { this.skipBranch(); } return true; }
/// command602() { if (this._branch[this._indent] !== 1) { this.skipBranch(); } return true; }
/// command603() { if (this._branch[this._indent] !== 2) { this.skipBranch(); } return true; }
/// </code>
/// <para>
/// <strong>And the slot gets its number from
/// <c>command301</c></strong> -- <strong>not from a condition</strong>:
/// </para>
/// <code>
/// BattleManager.setEventCallback(function(n) {
///     this._branch[this._indent] = n;
/// }.bind(this));
/// </code>
/// <para>
/// <strong>And <c>BattleManager.endBattle(result)</c> ruft es mit
/// <c>endBattle(0)</c> aus dem Sieg, <c>endBattle(1)</c> aus der Flucht
/// und <c>endBattle(2)</c> aus der Niederlage.</strong> <strong>Und ein
/// <c>111</c> schreibt an denselben Platz <c>true</c> oder
/// <c>false</c></strong> -- <strong>und das heisst, dass ein <c>601</c>
/// neben einem <c>111</c> an demselben Einzug eine Zahl findet, wo der
/// Bedingungszweig einen Wahrheitswert erwartet.</strong>
/// </para>
/// </remarks>
public enum MzBattleResult
{
    /// <summary>Won. <c>endBattle(0)</c>.</summary>
    Win = 0,

    /// <summary>Escaped. <c>endBattle(1)</c>.</summary>
    Escape = 1,

    /// <summary>Lost. <c>endBattle(2)</c>.</summary>
    Lose = 2,
}

/// <summary>
/// One enemy of the troop, and the numbers a command can change.
/// </summary>
/// <param name="Id">Its place in <c>Troops.json</c>'s list.</param>
/// <param name="Name">
/// Its name, and <c>$gameTroop.makeUniqueNames()</c> gives every living
/// enemy a number of its own so two of the same kind read differently.
/// </param>
/// <param name="Klasse">Which enemy it became, and <c>336</c> changes it.</param>
public sealed class MzEnemy
{
    public MzEnemy(int pId, string pName, int pKlasse)
    {
        Id = pId;
        Name = pName;
        Klasse = pKlasse;
    }

    /// <summary>Its place in the troop's own list.</summary>
    public int Id { get; }

    /// <summary>
    /// Its name, and <c>$gameTroop.makeUniqueNames()</c> gives every
    /// living enemy a name of its own so two of a kind read differently.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Which enemy it became, and <c>337</c> changes it and
    /// <c>makeUniqueNames</c> renames it after it.
    /// </summary>
    public int Klasse { get; set; }

    /// <summary>Its hit points, and this repository changes them as told.</summary>
    public int Hp { get; set; }

    /// <summary>And its magic points.</summary>
    public int Mp { get; set; }

    /// <summary>Its tactical points, which MV calls TP.</summary>
    public int Tp { get; set; }

    /// <summary>
    /// Whether it is on the field, and it is not death.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>command339</c> fragt
    /// <c>enemy.isAlive()</c></strong>, <strong>und
    /// <c>command336</c> ruft <c>enemy.appear()</c></strong> -- **und
    /// beide benutzen dasselbe Feld</strong>, <strong>und dieses
    /// Repository fuehrt es unter dem Namen, den der Befehl
    /// benutzt.</strong>
    /// </remarks>
    public bool Sichtbar { get; set; } = true;

    /// <summary>The states it carries, by their number.</summary>
    public HashSet<int> Zustaende { get; } = new();

    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "enemy " + Id + " '" + Name + "' from class " + Klasse
        + " at " + Hp + " hp";
}

/// <summary>
/// The troop in the battle, and what has happened to it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is <c>$gameTroop</c> for the eleven commands that
/// need it</strong> -- <strong><c>331</c> through <c>340</c></strong>,
/// <strong>and every one of them starts with
/// <c>iterateEnemyIndex</c>.</strong>
/// </para>
/// <code>
/// iterateEnemyIndex(param, callback) {
///     if (param &lt; 0) {
///         $gameTroop.members().forEach(callback);
///     } else {
///         var enemy = $gameTroop.members()[param];
///         if (enemy) {
///             callback(enemy);
///         }
///     }
/// }
/// </code>
/// <para>
/// <strong>And a negative index is every enemy and a positive one is one
/// enemy</strong> -- <strong>and an index past the end of the list is
/// nothing at all and not an error</strong>, <strong>because of
/// <c>if (enemy)</c>.
/// </para>
/// </remarks>
public sealed class MzBattle
{
    /// <summary>
    /// How the fight ended, and none of the three means "still going".
    /// </summary>
    /// <remarks>
    /// <strong>And this is the second of two questions.</strong>
    /// <strong><c>$gameParty.inBattle()</c> asks whether a fight is
    /// happening, and that lives in <c>MzBranchFacts.InBattle</c></strong>
    /// -- <strong>and this asks how it ended, and only <c>601</c>,
    /// <c>602</c> and <c>603</c> care.</strong>
    /// </remarks>
    public MzBattleResult? Ausgang { get; private set; }

    /// <summary>The enemy list, and <c>members()</c> is the same array.</summary>
    public IReadOnlyList<MzEnemy> Gegner => _gegner;

    private readonly List<MzEnemy> _gegner = new();

    /// <summary>
    /// Whether it is over, and it is not "in a battle" -- that is
    /// <c>$gameParty.inBattle()</c> and it is a different question.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>$gameParty.inBattle()</c> answers whether a fight is
    /// happening</strong> -- <strong>and <c>301</c>, <c>352</c>,
    /// <c>236</c> und <c>303</c> fragen das</strong> -- <strong>und
    /// <c>601</c> bis <c>603</c> fragen, wie es ausgegangen ist.</strong>
    /// <strong>Zwei verschiedene Fragen, und <c>MzBranchFacts</c> haelt
    /// die erste und dies die zweite.</strong>
    /// </remarks>
    public bool Laeuft => Ausgang == null;

    /// <summary>
    /// The troop's enemy list, and <c>members()</c> is the same array.
    /// </summary>
    /// <remarks>
    /// <strong>And who the troop is, whether the player may run and
    /// whether a loss ends the game, lives in
    /// <c>MzBranchFacts.BattleTroop</c> and its two neighbours</strong> --
    /// <strong>because <c>301</c> sets all three in one place</strong> --
    /// <strong>and a second copy here would be a second place to get it
    /// wrong.</strong>
    /// </remarks>

    /// <summary>
    /// <c>BattleManager.endBattle(result)</c> and its callback, and the
    /// three numbers are the enum's.
    /// </summary>
    /// <param name="pErgebnis">Zero, one or two.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string Endet(int pErgebnis)
    {
        Ausgang = pErgebnis switch
        {
            0 => MzBattleResult.Win,
            1 => MzBattleResult.Escape,
            _ => MzBattleResult.Lose,
        };
        return "the battle ends in " + Ausgang;
    }

    /// <summary>
    /// <c>$gameTroop.members()</c>, and <c>this repository</c> fills it
    /// from the troop's own file.
    /// </summary>
    public void SetzeGegner(IEnumerable<MzEnemy> pGegner)
    {
        _gegner.Clear();
        _gegner.AddRange(pGegner);
    }

    /// <summary>
    /// <c>iterateEnemyIndex</c>, and it is what the eleven commands all
    /// start with.
    /// </summary>
    /// <param name="pIndex">Negative for every enemy, otherwise the one.</param>
    /// <returns>The enemy, or null when the index is past the end.</returns>
    public MzEnemy GegnerNummer(int pIndex)
    {
        if (pIndex < 0)
        {
            return null;
        }

        return pIndex < _gegner.Count ? _gegner[pIndex] : null;
    }

    /// <summary>
    /// Every enemy of the troop, and that is what a negative index means.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was a real mistake of mine, and the engine's own
    /// words caught it.</strong>
    /// </para>
    /// <code>
    /// if (param &lt; 0) {
    ///     $gameTroop.members().forEach(callback);
    /// }
    /// </code>
    /// <para>
    /// <strong>And <c>members()</c> is the whole list</strong> -- <strong>not
    /// the living ones.</strong> <strong>Only <c>command339</c> asks
    /// <c>enemy.isAlive()</c></strong>, <strong>and it asks it inside its
    /// own callback</strong>, <strong>and every other one of the nine takes
    /// the enemy whether it is on the field or not.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that filters here would hand
    /// <c>336</c> an empty list</strong> -- <strong>and <c>336</c> is
    /// <c>enemy.appear()</c></strong>, <strong>which is exactly the command
    /// that brings a hidden enemy back.</strong>
    /// </para>
    /// </remarks>
    public IEnumerable<MzEnemy> Alle()
    {
        foreach (var gegner in _gegner)
        {
            yield return gegner;
        }
    }

    /// <summary>
    /// And <c>enemy.isAlive()</c>, which is what <c>339</c> asks and the
    /// other eight do not.
    /// </summary>
    public static bool Lebt(MzEnemy pGegner) => pGegner.Sichtbar;

    /// <summary>
    /// <c>$gameTroop.makeUniqueNames()</c>, and two enemies of the same
    /// kind get different names because of it.
    /// </summary>
    /// <param name="pNamen">The base name every enemy of a class carries.</param>
    /// <returns>One line, for an action and for a log.</returns>
    /// <summary>
    /// <c>$gameTroop.makeUniqueNames()</c>, and two enemies of the same
    /// kind get different names because of it.
    /// </summary>
    /// <param name="pNamen">The base name every enemy of a class carries.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string MacheNamenEindeutig(Func<int, string> pNamen)
    {
        var anzahlJeName = new Dictionary<string, int>();
        foreach (var gegner in _gegner)
        {
            var name = pNamen(gegner.Klasse);
            var anzahl = anzahlJeName.GetValueOrDefault(name) + 1;
            anzahlJeName[name] = anzahl;
            gegner.Name = anzahl == 1 ? name : name + " " + anzahl;
        }

        return "the troop's names were made unique, and "
            + anzahlJeName.Count + " kinds of enemy carry them, and "
            + _gegner.Count + " enemies in all";
    }
    /// <summary>
    /// <c>iterateBattler</c>, and it does nothing outside a battle.
    /// </summary>
    /// <param name="pWer">Zero is an enemy, anything else an actor.</param>
    /// <param name="pNummer">Which one.</param>
    /// <param name="pInKampf">The engine's own condition.</param>
    /// <returns>Whether the two of them name the same kind.</returns>
    public static bool IstGegner(int pWer) => pWer == 0;
}
