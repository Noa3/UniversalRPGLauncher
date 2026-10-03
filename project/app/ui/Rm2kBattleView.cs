using Godot;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.App;

/// <summary>
/// And the battle, drawn from the state and not from a screen of its
/// own.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because the launcher drew the map and the
/// message and nothing else.</strong> --
/// <strong><c>IsBattleActive</c> appeared in no file under
/// <c>app/ui</c></strong>, -- <strong>and a battle that happens but
/// cannot be seen is indistinguishable from a battle that did
/// not happen.</strong>
/// </para>
/// <para>
/// <strong>And it draws the truth</strong>: one figure per living
/// member, its name and its hit points, -- <strong>and the numbers
/// come from the state's members</strong>, -- <strong>which are the
/// game's own numbers out of <c>RPG_RT.ldb</c>.</strong>
/// </para>
/// <para>
/// <strong>And a dead member is not drawn at all</strong>, --
/// <strong>because a figure standing there with no health is a
/// figure the game never put there.</strong>
/// </para>
/// </remarks>
public partial class Rm2kBattleView : Control
{
    private const int ZeilenHoehe = 22;

    private GameSimulationState? _zustand;

    /// <summary>And it takes the state.</summary>
    public void SetzeZustand(GameSimulationState? pZustand)
    {
        _zustand = pZustand;
    }

    /// <summary>And whether the battle is running at all.</summary>
    /// <remarks>
    /// <strong>And this is the one question a caller should have to
    /// ask.</strong>
    /// </remarks>
    public bool LaeuftEinKampf()
    {
        return _zustand != null && _zustand.IsBattleActive;
    }

    /// <summary>
    /// And how many members are still standing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it counts through <c>CanMonsterAct</c></strong>,
    /// -- <strong>because that is the question the interpreter
    /// asks</strong>, -- <strong>and a view that counted members
    /// would show a fallen enemy as alive.</strong>
    /// </para>
    /// </remarks>
    public int LebendeGegner()
    {
        if (_zustand == null)
        {
            return 0;
        }

        var anzahl = 0;
        for (var i = 0; i < _zustand.TroopMembers.Count; i++)
        {
            if (_zustand.CanMonsterAct(i))
            {
                anzahl++;
            }
        }

        return anzahl;
    }

    public override void _Draw()
    {
        if (_zustand == null || !_zustand.IsBattleActive)
        {
            return;
        }

        var y = 24;
        // **Und  die  Beschriftung  ist  englisch  und  nicht
        //  uebersetzt** -- **und  das  ist  die  Konvention  der
        //  Nachbarvorschau** (`No parsed RM2K map`), --
        // **und  ein  deutsches  Wort  hier  waere  eine
        //  Inkonsistenz  und  keine  Verbesserung.**
        DrawString(ThemeDB.FallbackFont, new Vector2(12, y), "Battle",
            HorizontalAlignment.Left, -1, 18, new Color("f5f0ff"));
        y += ZeilenHoehe + 6;

        for (var i = 0; i < _zustand.TroopMembers.Count; i++)
        {
            var gegner = _zustand.TroopMembers[i];
            var lebendig = _zustand.CanMonsterAct(i);
            var hp = _zustand.MonsterHp(i);
            var maxHp = _zustand.MonsterMaxHp(i);
            var name = gegner.ContainsKey("name")
                ? gegner["name"].AsString() : "(ohne Namen)";

            var farbe = lebendig
                ? new Color("f5f0ff") : new Color("8f8a99");
            DrawString(ThemeDB.FallbackFont, new Vector2(20, y),
                $"{i + 1}. {name}   {hp}/{maxHp}",
                HorizontalAlignment.Left, -1, 14, farbe);
            y += ZeilenHoehe;
        }

        if (_zustand.TroopMembers.Count == 0)
        {
            // **Und  das  ist  der  Fall,  der  nie  eintreten
            //  darf** -- **und  wenn  er  es  doch  tut,  steht  hier,
            //  warum**, -- **und  nicht  eine  leere  Box.**
            DrawString(ThemeDB.FallbackFont, new Vector2(20, y),
                "no monster in this encounter",
                HorizontalAlignment.Left, -1, 14,
                new Color("e8a24a"));
            y += ZeilenHoehe;
        }

        DrawString(ThemeDB.FallbackFont, new Vector2(20, y + 6),
            $"Turn {_zustand.BattleTurn}   "
            + $"Troop {_zustand.ActiveTroopId}   "
            + $"Outcome {_zustand.BattleSubcommand}",
            HorizontalAlignment.Left, -1, 13,
            new Color("b6b0c2"));
    }
}
