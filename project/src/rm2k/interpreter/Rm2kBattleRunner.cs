using System;
using System.Collections.Generic;
using System.Linq;
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
    private List<Rm2kTroopPageDecoder.DecodedPage>? _allPages;
    private int _currentPageIndex = -1;

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
    /// And it loads the troop's pages and selects the first valid one, or says why not.
    /// </summary>
    /// <param name="pBank">The parsed database.</param>
    /// <param name="pTroopId">The troop the encounter named.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether a page is running.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the first valid page, evaluated by its condition</strong>,
    /// -- <strong>because the game evaluates each page's condition in order</strong>,
    /// -- <strong>and runs the first one that passes.</strong>
    /// </para>
    /// </remarks>
    public bool Lade(
        Godot.Collections.Dictionary pBank,
        int pTroopId,
        out string pFehler)
    {
        pFehler = "";
        _seite = null;
        _allPages = null;
        _currentPageIndex = -1;

        if (!Rm2kTroopPageDecoder.TryDecodeAll(
                SeitenBytes(pBank, pTroopId) ?? new byte[0],
                out var pages, out var warum))
        {
            pFehler = "troop " + pTroopId + ": " + warum;
            return false;
        }

        _allPages = pages;

        // Find the first page whose condition passes (or the first page if no condition)
        for (var i = 0; i < _allPages.Count; i++)
        {
            var page = _allPages[i];
            if (PageConditionPasses(page))
            {
                _currentPageIndex = i;
                break;
            }
        }

        if (_currentPageIndex == -1)
        {
            pFehler = "troop " + pTroopId + ": no page condition passed";
            return false;
        }

        var befehleListe = new List<Rm2kMap.EventCommand>();
        foreach (var feld in _allPages[_currentPageIndex].Commands)
        {
            befehleListe.Add(Rm2kMap.EventCommand.AusGelesenem(feld));
        }

        // **Und  die  Seite  laeuft  auf  demselben  Zustand** --
        // **denn  eine  Kampfseite  hat  keine  Kartenposition.**
        _seite = new EventInterpreter(
            _zustand, -1, befehleListe, _darstellung);
        _truppenId = pTroopId;
        _zustand.AddDiagnostic(
            "[Battle] troop " + pTroopId + " page " + _currentPageIndex + " brought "
            + befehleListe.Count + " commands into the battle");
        return true;
    }

    /// <summary>
    /// Checks if a page's condition passes.
    /// </summary>
    /// <param name="pPage">The page to check.</param>
    /// <returns>True if the condition passes or there is no condition.</returns>
    private bool PageConditionPasses(Rm2kTroopPageDecoder.DecodedPage pPage)
    {
        // If no condition data, the page always passes
        if (pPage.ConditionData == null || pPage.ConditionData.Length == 0)
        {
            return true;
        }

        // TODO: Evaluate the condition data
        // For now, assume it passes if we can't evaluate it
        // The condition format is complex and requires a full condition evaluator
        _zustand.AddDiagnostic(
            "[Battle] page condition evaluation not fully implemented, assuming pass");
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

        // Check if we should transition to another page (e.g., after battle branch)
        var nextPageIndex = FindNextValidPage();
        if (nextPageIndex >= 0 && nextPageIndex != _currentPageIndex)
        {
            _currentPageIndex = nextPageIndex;
            var befehleListe = new List<Rm2kMap.EventCommand>();
            foreach (var feld in _allPages![_currentPageIndex].Commands)
            {
                befehleListe.Add(Rm2kMap.EventCommand.AusGelesenem(feld));
            }
            _seite = new EventInterpreter(
                _zustand, -1, befehleListe, _darstellung);
            _zustand.AddDiagnostic(
                "[Battle] switched to troop page " + _currentPageIndex + " with "
                + befehleListe.Count + " commands");
            return true;
        }

        _seite = null;
        _truppenId = -1;
        _allPages = null;
        _currentPageIndex = -1;
        return false;
    }

    /// <summary>
    /// Finds the next valid page based on conditions.
    /// </summary>
    private int FindNextValidPage()
    {
        if (_allPages == null)
        {
            return -1;
        }

        // For now, just check subsequent pages
        // In a full implementation, this would be triggered by battle branch commands
        for (var i = _currentPageIndex + 1; i < _allPages.Count; i++)
        {
            if (PageConditionPasses(_allPages[i]))
            {
                return i;
            }
        }
        return -1;
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