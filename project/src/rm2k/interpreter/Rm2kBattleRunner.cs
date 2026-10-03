using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// And the battle's own page, run on the game's own commands.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a second interpreter and not a special
/// one.</strong> -- <strong>A troop page is a list of
/// <c>EventCommand</c> like any map page</strong>, -- <strong>and
/// the same reader runs it</strong>, -- <strong>and a battle
/// interpreter of its own would be a second dialect for one
/// command list.</strong>
/// </para>
/// <para>
/// <strong>And the battle reads the troop's own conditions
/// itself</strong>, -- <strong>because <c>13310 Battle Branch</c> is
/// written by the game on the page and evaluated by the game
/// there.</strong> -- <strong>And this runner does not guess at
/// them.</strong>
/// </para>
/// </remarks>
public sealed class Rm2kBattleRunner
{
    private readonly GameSimulationState _zustand;
    private readonly PresentationState? _darstellung;

    private EventInterpreter? _seite;
    private int _truppenId = -1;

    /// <summary>Builds a runner over one state.</summary>
    public Rm2kBattleRunner(
        GameSimulationState pZustand, PresentationState? pDarstellung = null)
    {
        _zustand = pZustand ?? throw new ArgumentNullException(
            nameof(pZustand));
        _darstellung = pDarstellung;
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
        Godot.Collections.Dictionary pBank,
        int pTroopId,
        out string pFehler)
    {
        pFehler = "";
        _seite = null;

        if (!Rm2kTroopPageDecoder.TryDecode(
                SeitenBytes(pBank, pTroopId) ?? new byte[0],
                out var befehle, out var warum))
        {
            pFehler = "troop " + pTroopId + ": " + warum;
            return false;
        }

        // **Und  die  Dictionaries  werden  zu  Befehlen** -- **und
        //  das  ist  dieselbe  Bruecke,  die  eine  Kartenseite
        //  braucht**, -- **und  es  ist  kein  zweiter  Dialekt.**
        var befehleListe = new List<Rm2kMap.EventCommand>();
        foreach (var feld in befehle)
        {
            befehleListe.Add(Rm2kMap.EventCommand.AusGelesenem(feld));
        }

        // **Und  die  Seite  laeuft  auf  demselben  Zustand** --
        // **denn  eine  Kampfseite  hat  keine  Kartenposition.**
        _seite = new EventInterpreter(
            _zustand, -1, befehleListe, _darstellung);
        _truppenId = pTroopId;
        _zustand.AddDiagnostic(
            "[Battle] troop " + pTroopId + " brought "
            + befehleListe.Count + " commands into the battle");
        return true;
    }

    /// <summary>
    /// And who acts now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is the troop's own order</strong>, --
    /// <strong>because the members array is the order the game wrote
    /// and nothing in this repository may reorder it.</strong>
    /// </para>
    /// <para>
    /// <strong>And it returns -1 when nobody acts</strong>, --
    /// <strong>which is the round's end and not an error.</strong>
    /// </para>
    /// </remarks>
    public int WerIstDran() => Rm2kZugfolge.Naechster(_zustand);

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

        if (_seite.ExecuteFrame())
        {
            return true;
        }

        _seite = null;
        _truppenId = -1;
        return false;
    }

    private static byte[]? SeitenBytes(
        Godot.Collections.Dictionary pBank, int pId)
    {
        if (!pBank.ContainsKey("troops"))
        {
            return null;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["troops"];
        foreach (var t in truppen)
        {
            if (!t.ContainsKey("id") || t["id"].AsInt32() != pId)
            {
                continue;
            }

            if (!t.ContainsKey("unknown_fields"))
            {
                return null;
            }

            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                t["unknown_fields"])
            {
                if (f["id"].AsInt32() == 0x0B)
                {
                    return (byte[])f["data"];
                }
            }

            return null;
        }

        return null;
    }
}
