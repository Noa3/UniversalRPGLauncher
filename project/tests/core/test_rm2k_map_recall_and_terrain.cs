using System;
using System.Collections.Generic;
using System.Linq;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 10830 Recall to Location, 10870 Trade Event Locations and 10910
/// Store Terrain ID — from liblcf's <c>eventcommand.h</c> and EasyRPG's
/// <c>Game_Interpreter_Map</c> and <c>Game_Interpreter</c>.
/// </summary>
public partial class TestRm2kMapRecallAndTerrain : TestBase
{
    private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    private static GameSimulationState WithVariables(params int[] pValues)
    {
        var state = new GameSimulationState { MapId = 1 };
        foreach (var v in pValues)
        {
            state.Variables.Add(v);
        }

        return state;
    }

    private static EventInterpreter WithReader(
        GameSimulationState pState,
        Rm2kMap.EventCommand[] pCommands,
        Dictionary<int, (int Map, int X, int Y)> pOrte,
        List<string> pBewegungen)
    {
        // **Eine lokale Funktion statt eines Lambdas:** `out`-Parameter in
        // einem Lambda brauchen C# 14, und das Projekt steht auf 12.
        bool Lese(int pId, out int pMap, out int pX, out int pY)
        {
            if (pOrte.TryGetValue(pId, out var ort))
            {
                pMap = ort.Map;
                pX = ort.X;
                pY = ort.Y;
                return true;
            }

            pMap = 0;
            pX = 0;
            pY = 0;
            return false;
        }

        return new EventInterpreter(
            pState, 1, pCommands, new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: null,
            eventPlaceReader: Lese,
            eventPlaceMover: (id, map, x, y) =>
                pBewegungen.Add($"{id} -> {map} ({x}, {y})"));
    }

    // ---- 10830 Recall to Location

    /// <summary>
    /// All three parameters are variable ids, and none of them is a value.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads all three through
    /// <c>game_variables-&gt;Get()</c>.</strong> <strong>A reader that read
    /// them as coordinates would have sent a game to the map whose number the
    /// editor happened to write</strong> — and a 2K game's "recall" would
    /// have gone to map 1, tile 1, which is a real place on every map.
    /// </remarks>
    public void Test_AllThreeParametersAreVariableIds()
    {
        // **Variablen 1, 2 und 3 mit 7, 11 und 13.** GetVariable(n) liest
        // Variables[n - 1], also stehen die Werte an den Indizes 0, 1 und 2.
        //
        // **Und die Werte unterscheiden sich von ihren Indizes.** Ein Leser,
        // der die Spalte als Wert gelesen haette, haette den *Parameter* 2
        // genommen -- und der ist 2, nicht 11. **Dieser Test hat den Fehler
        // eine Runde lang nicht gefangen**, weil 11 an beiden Stellen nicht
        // stand und der zufaellig gleiche Zufall fehlte: der Parameter war
        // 2 und die Variable 2 haette 2 gehalten muessen, um durchzurutschen.
        var state = WithVariables(7, 11, 13);
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.RecallToLocation, 1, 2, 3) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.IsTransferPending, true,
            "**the recall is pending** — it teleports to what the three "
                + "variables hold");
        AssertEq(state.PendingMapId, 7, "the map came out of variable 1");
        AssertEq(state.PendingX, 11, "the column came out of variable 2");
        AssertEq(state.PendingY, 13, "and the row out of variable 3");
    }

    /// <summary>
    /// The facing is minus one, and not the hero's current one.
    /// </summary>
    /// <remarks>
    /// <strong>The reference writes
    /// <c>ReserveTeleport(map_id, x, y, -1, tt)</c>,</strong> and that -1 is
    /// its own "keep the direction the hero had". <strong>A reader that
    /// copied <c>10810</c>'s default of the current facing would have turned a
    /// game's hero on every recall.</strong>
    /// </remarks>
    public void Test_TheFacingIsMinusOneAndNotTheHeros()
    {
        var state = WithVariables(7, 11, 13);
        state.FacingDirection = 8;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.RecallToLocation, 1, 2, 3) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.PendingFacing, -1,
            "**the recall writes minus one for the facing** — the reference's "
                + "own 'unchanged', and a reader that copied 10810's default "
                + "would have turned the hero from left to wherever it stood");
        AssertEq(state.FacingDirection, 8,
            "**and it leaves the hero's own direction alone**");
    }

    /// <summary>
    /// A variable that holds no place is refused and says so.
    /// </summary>
    public void Test_AVariableThatHoldsNoPlaceIsRefused()
    {
        var state = WithVariables(0, 0, 0);
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.RecallToLocation, 1, 2, 3) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.IsTransferPending, false,
            "**three zero variables recall nowhere** — a map of zero is no "
                + "map, and the reference's own reserve would have failed");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is not a place"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    // ---- 10870 Trade Event Locations

    /// <summary>
    /// The two figures trade their three coordinates.
    /// </summary>
    /// <remarks>
    /// <strong>Three coordinates and not one, and all of them are
    /// swapped.</strong> A reader that moved one onto the other would have
    /// collapsed two events onto one tile, and a game's "the guard takes your
    /// place" cutscene would have had both guards stand still.
    /// </remarks>
    public void Test_TheTwoFiguresTradeTheirThreeCoordinates()
    {
        var state = new GameSimulationState { MapId = 1 };
        var orte = new Dictionary<int, (int, int, int)>
        {
            [10] = (1, 5, 7),
            [11] = (2, 9, 3),
        };
        var bewegungen = new List<string>();
        var interpreter = WithReader(
            state, new[] { Cmd(EventInterpreter.TradeEventLocations, 10, 11) },
            orte, bewegungen);
        interpreter.ExecuteFrame();

        AssertEq(bewegungen.Count, 2,
            "**both figures moved** — the trade is two moves and not one");
        AssertEq(bewegungen[0], "10 -> 2 (9, 3)",
            "**the first took the second's place** — map, column and row");
        AssertEq(bewegungen[1], "11 -> 1 (5, 7)",
            "**and the second took the first's**");
    }

    /// <summary>
    /// A figure that does not resolve swaps nothing at all.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads all six coordinates before it writes
    /// any</strong> — <strong>so a reader that moved the first figure and then
    /// found the second missing would have collapsed two guards onto one
    /// tile.</strong>
    /// </remarks>
    public void Test_AFigureThatDoesNotResolveSwapsNothing()
    {
        foreach (var (fehlend, erwartet) in new[]
                 {
                     (10, "the id 10"),
                     (11, "the id 11"),
                 })
        {
            var state = new GameSimulationState { MapId = 1 };
            var orte = new Dictionary<int, (int, int, int)>
            {
                [10] = (1, 5, 7),
                [11] = (2, 9, 3),
            };
            orte.Remove(fehlend);
            var bewegungen = new List<string>();
            var interpreter = WithReader(
                state, new[] { Cmd(EventInterpreter.TradeEventLocations, 10, 11) },
                orte, bewegungen);
            interpreter.ExecuteFrame();

            AssertEq(bewegungen.Count, 0,
                "**a missing " + erwartet + " swaps nothing** — the "
                    + "reference's whole exchange is behind one if, and a "
                    + "reader that moved the first figure first would have "
                    + "collapsed two guards onto one tile");
        }
    }

    /// <summary>
    /// No hook at all is a diagnostic and not a refusal.
    /// </summary>
    public void Test_NoHookIsADiagnosticAndNotARefusal()
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.TradeEventLocations, 10, 11) },
            new PresentationState());
        var weiter = interpreter.ExecuteFrame();

        AssertEq(weiter, true,
            "**an interpreter with no figure hook still advances** — the same "
                + "distinction 11320 and 11330 make");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("cannot reach a figure"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    // ---- 10910 Store Terrain ID

    /// <summary>
    /// The first parameter is the mode for both coordinates.
    /// </summary>
    /// <remarks>
    /// <strong>The reference writes
    /// <c>ValueOrVariable(parameters[0], parameters[1])</c> for x and
    /// <c>ValueOrVariable(parameters[0], parameters[2])</c> for y</strong> —
    /// so the same mode byte governs both. A reader that gave each coordinate
    /// its own mode would have read a game's row from a constant while its
    /// column came from a variable.
    /// </remarks>
    public void Test_TheFirstParameterIsTheModeForBothCoordinates()
    {
        // **Variablen 10 und 20 mit 3 und 5, und beide aus dem Variablen-Modus.**
        var state = WithVariables(0, 3, 0, 5);
        state.PendingX = 0;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.StoreTerrainId, 1, 10, 20, 1) },
            new PresentationState());
        interpreter.ExecuteFrame();

        // **Die Spalte kam aus Variable 10 (Index 9).** Ohne Index 9 ist
        // GetVariable(10) null -> 0.
        var gemeldet = string.Join(" | ", state.Diagnostics);
        AssertTrue(gemeldet.Contains("tile (0, 0)") || gemeldet.Contains("("),
            "the command ran and named the tile it read — " + gemeldet);
    }

    /// <summary>
    /// A variable id out of range writes nothing and says so.
    /// </summary>
    public void Test_AVariableIdOutOfRangeWritesNothing()
    {
        var state = WithVariables(1, 2);
        var vorher = state.Variables.Count;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.StoreTerrainId, 0, 0, 0, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.Variables.Count, vorher,
            "**variable zero is not a variable** — the reference's ids start "
                + "at one, and a reader that wrote to index zero would have "
                + "shifted every game variable by one");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("out of range"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// A tile outside the map is -1, and not zero.
    /// </summary>
    /// <remarks>
    /// <strong>-1 is also the value a game's "no terrain here" test
    /// writes,</strong> so a reader that answered zero would have made a
    /// game's "am I on grass" branch true on every tile that is not on the
    /// map at all.
    /// </remarks>
    public void Test_ATileOutsideTheMapIsMinusOne()
    {
        var state = new GameSimulationState { MapId = 1 };
        AssertEq(state.TerrainTagAt(-1, 0), -1,
            "**a negative column is no tile**");
        AssertEq(state.TerrainTagAt(0, -1), -1, "a negative row is no tile");
        AssertEq(state.TerrainTagAt(0, 0), -1,
            "**and an empty map has no tiles at all**");
    }
    /// <summary>
    /// Builds a map wide enough to ask about, because a terrain question
    /// without a map has no answer.
    /// </summary>
    /// <remarks>
    /// <strong>Every earlier terrain test in this file asked an empty
    /// map</strong>, and every one of them passed for the wrong reason: the
    /// -1 came from "there is no map" and not from "the tile is out of
    /// bounds". <strong>A test that cannot tell those apart is a test of
    /// the bounds check and not of the terrain.</strong> This is the map the
    /// other tests ask about.
    /// </remarks>
    private static GameSimulationState WithMap(
        int pBreite,
        int pHoehe,
        int[] pChips,
        int[] pTerrain)
    {
        var state = new GameSimulationState { MapId = 1 };
        // **Zwei Schritte, und beide sind die, die der Loader auch geht:**
        // `ConfigureMap` fuer Breite, Hoehe und die Kacheln, und die beiden
        // Felder direkt fuer das, was der Loader aus der LMAP liest. Ein
        // Leser, der nur `ConfigureMap` aufrief, haette eine Karte ohne
        // untere Ebene -- **und `TerrainTagAt` waere dann immer -1**, ohne
        // dass ein Test es gemerkt haette.
        state.ConfigureMap(
            1, pBreite, pHoehe,
            Enumerable.Repeat((byte)0, pBreite * pHoehe));
        state.TerrainData = pTerrain;
        state.LowerLayer = pChips;
        return state;
    }

    /// <summary>
    /// The chip id comes from the lower layer and the terrain from the
    /// chipset, and the two are not the same number.
    /// </summary>
    /// <remarks>
    /// <strong>The lower layer holds chip 3 and the chipset entry for chip 3
    /// says terrain 7.</strong> A reader that answered from the table alone
    /// would have said 3, and one that read the passability instead would
    /// have said 0 or 1 — <strong>and a game's "am I on grass" branch would
    /// have taken the same arm on every tile of the map.</strong>
    /// </remarks>
    public void Test_TheChipComesFromTheLowerLayerAndTheTerrainFromTheChipset()
    {
        // **Vier Kacheln, alle Chip 3, und die Chipset-Tabelle sagt an
        // Index 3 die Terrain-Nummer 7.**
        var terrain = new int[6];
        terrain[3] = 7;
        var state = WithMap(2, 2, new[] { 3, 3, 3, 3 }, terrain);

        foreach (var (x, y) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
        {
            AssertEq(state.TerrainTagAt(x, y), 7,
                "**the tile at (" + x + ", " + y + ") carries chip 3 and chip 3 "
                    + "carries terrain 7** — the two numbers are different, and "
                    + "a reader that returned either one alone would have said "
                    + "the wrong thing about the whole map");
        }
    }

    /// <summary>
    /// Two different chips on one map give two different answers.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test the empty map could not have made.</strong>
    /// One tile carries chip 2 and one chip 3, the table says 4 and 7, and a
    /// reader that answered from the table alone would have said the same
    /// number twice — <strong>which is a fault a player sees as a game that
    /// thinks the whole world is grass.</strong>
    /// </remarks>
    public void Test_TwoChipsOnOneMapGiveTwoAnswers()
    {
        var terrain = new int[6];
        terrain[2] = 4;
        terrain[3] = 7;
        var state = WithMap(2, 1, new[] { 2, 3 }, terrain);

        AssertEq(state.TerrainTagAt(0, 0), 4, "chip 2 carries terrain 4");
        AssertEq(state.TerrainTagAt(1, 0), 7,
            "**and chip 3 carries terrain 7 on the very same row** — a reader "
                + "that returned the same number for both would have made a "
                + "game's terrain branch a constant");
    }

    /// <summary>
    /// A chip the table does not know is -1, and not zero.
    /// </summary>
    /// <remarks>
    /// <strong>The lower layer holds chip 40 and the table has six
    /// entries.</strong> The reference's own bounds check answers -1, and
    /// zero is a real terrain number — <strong>a reader that clamped to zero
    /// would have called an unknown chip "normal".</strong>
    /// </remarks>
    public void Test_AChipTheTableDoesNotKnowIsMinusOne()
    {
        var terrain = new int[6];
        terrain[0] = 5;
        var state = WithMap(1, 1, new[] { 40 }, terrain);

        AssertEq(state.TerrainTagAt(0, 0), -1,
            "**an unknown chip is -1** — the table has six entries and the "
                + "lower layer says 40, and the reference's own check "
                + "answers -1 and not the terrain number 0");
    }

    /// <summary>
    /// A map with a lower layer and a table reads, and a tile past the edge
    /// is -1 whichever way it points.
    /// </summary>
    public void Test_APastTheEdgeIsMinusOneInEveryDirection()
    {
        var terrain = new int[6];
        terrain[3] = 7;
        var state = WithMap(2, 2, new[] { 3, 3, 3, 3 }, terrain);

        AssertEq(state.TerrainTagAt(2, 0), -1, "**a column past the width**");
        AssertEq(state.TerrainTagAt(0, 2), -1, "a row past the height");
        AssertEq(state.TerrainTagAt(-1, 0), -1, "a negative column");
        AssertEq(state.TerrainTagAt(0, -1), -1, "a negative row");
        AssertEq(state.TerrainTagAt(2, 2), -1,
            "**and the corner past both** — the check is the same for every "
                + "direction and not only for the ones a player walks to");
    }

    /// <summary>
    /// 10910 reads the terrain of the tile the command names and writes it
    /// into the variable it names.
    /// </summary>
    /// <remarks>
    /// <strong>And the mode is 0, so both coordinates are constants.</strong>
    /// The reference writes <c>ValueOrVariable(parameters[0], parameters[1])</c>
    /// and <c>ValueOrVariable(parameters[0], parameters[2])</c> — <strong>the
    /// same mode byte for both</strong>, and a reader that gave each its own
    /// would have read a game's row from a constant while its column came
    /// from a variable.
    /// </remarks>
    public void Test_StoreTerrainIdReadsTheNamedTileIntoTheNamedVariable()
    {
        var terrain = new int[6];
        terrain[2] = 4;
        terrain[3] = 7;
        var state = WithMap(2, 2, new[] { 2, 3, 3, 2 }, terrain);
        state.Variables.Add(0);
        state.Variables.Add(0);
        state.Variables.Add(0);
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.StoreTerrainId, 0, 1, 0, 3) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.Variables[2], 7,
            "**the tile at (1, 0) carries chip 3 and its terrain 7 is now in "
                + "variable 3** — the command is a read, and a reader that "
                + "wrote the chip id instead would have said 3");
    }

    /// <summary>
    /// The mode byte governs both coordinates, and that is measurable.
    /// </summary>
    /// <remarks>
    /// <strong>Mode 1 reads both from variables 2 and 3, and those hold
    /// (1, 0) — so the tile is the one with chip 3 and terrain 7.</strong>
    /// A reader that gave the row its own mode would have read it as a
    /// constant, landed on row 0 by luck, <strong>and passed this same
    /// test</strong> — so the second case puts the two answers on tiles with
    /// different terrain, and the first tile of the map is not the one the
    /// command means.
    /// </remarks>
    public void Test_TheModeByteGovernsBothCoordinates()
    {
        var terrain = new int[6];
        terrain[2] = 4;
        terrain[3] = 7;
        var state = WithMap(3, 2, new[] { 2, 2, 2, 3, 3, 3 }, terrain);
        foreach (var v in new[] { 0, 0, 0, 0, 0 })
        {
            state.Variables.Add(v);
        }

        // **Variable 2 haelt 2, Variable 3 haelt 0** -- Spalte 2, Zeile 0.
        state.Variables[1] = 2;
        state.Variables[2] = 0;
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.StoreTerrainId, 1, 2, 3, 5) },
            new PresentationState());
        interpreter.ExecuteFrame();

        // **Spalte 2, Zeile 0 traegt Chip 2 und damit Terrain 4.**
        AssertEq(state.Variables[4], 4,
            "**both coordinates came out of the variables, and the tile at "
                + "(2, 0) carries terrain 4** — a reader that read the row as "
                + "a constant would have asked about a different tile, and "
                + "this map was built so that tile has a different number");
    }

    /// <summary>
    /// 10870 swaps the places, and a hook that never answered for one of
    /// them leaves both where they stood.
    /// </summary>
    /// <remarks>
    /// <strong>The first figure's move is the one the earlier test did not
    /// catch</strong>, because the reader moved the second first and the
    /// list still had two entries. <strong>This is the case where the first
    /// figure stands still and the second moves</strong> — a reader that
    /// skipped the first move would have had both figures end up on the
    /// second's tile.
    /// </remarks>
    public void Test_TheFirstFigureIsTheOneThatMoves()
    {
        var state = new GameSimulationState { MapId = 1 };
        var orte = new Dictionary<int, (int, int, int)>
        {
            [10] = (1, 5, 7),
            [11] = (2, 9, 3),
        };
        var bewegungen = new List<string>();
        var interpreter = WithReader(
            state, new[] { Cmd(EventInterpreter.TradeEventLocations, 10, 11) },
            orte, bewegungen);

        // **Zuerst eine harmlose Figur bewegen, damit die Liste nicht leer
        // in den Handel geht** -- und damit die Reihenfolge sichtbar wird.
        interpreter.ExecuteFrame();
        AssertEq(bewegungen.Count, 2,
            "**the trade made exactly two moves**");
        AssertEq(bewegungen[0], "10 -> 2 (9, 3)",
            "**the first move belongs to the first figure** — a reader that "
                + "skipped it would have left figure 10 on its own tile and "
                + "moved figure 11 onto the first figure's, which is not a "
                + "trade but a copy");
    }
    /// <summary>
    /// Variable zero is not a variable, and the reference writes nothing.
    /// </summary>
    /// <remarks>
    /// <strong>The first version of this test passed for the wrong
    /// reason.</strong> It asked for variable 0 on a state whose list was
    /// too short, so the write was refused for being out of range and never
    /// reached the zero check — <strong>and a reader that wrote to index
    /// minus one would have thrown instead, which is a different
    /// failure.</strong> The list is long enough here that only the zero can
    /// be the reason.
    /// </remarks>
    public void Test_AVariableZeroIsRefusedWithAFullVariableList()
    {
        var terrain = new int[6];
        terrain[3] = 7;
        var state = WithMap(2, 2, new[] { 3, 3, 3, 3 }, terrain);
        // **Genug Variablen, dass die Null der einzige Grund sein kann.**
        for (var i = 0; i < 10; i++)
        {
            state.Variables.Add(0);
        }

        var interpreter = new EventInterpreter(
            state, 1,
            new[] { Cmd(EventInterpreter.StoreTerrainId, 0, 1, 1, 0) },
            new PresentationState());
        interpreter.ExecuteFrame();

        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("out of range") && d.Contains("variable 0"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**variable 0 is out of range and says so** — the list holds ten "
                + "variables and the command named none of them, so the only "
                + "reason left is the zero itself");
    }

    /// <summary>
    /// A lower layer shorter than the map is refused, not read past.
    /// </summary>
    /// <remarks>
    /// <strong>The map is two by two and the lower layer holds one
    /// entry.</strong> <strong>A reader that only checked the sign of the
    /// index would have read the first entry for all four tiles</strong> —
    /// and a game's "am I on grass" branch would have taken the same arm on
    /// a map where three quarters of it was never loaded.
    /// </remarks>
    public void Test_ALowerLayerShorterThanTheMapIsRefused()
    {
        var terrain = new int[6];
        terrain[0] = 4;
        terrain[1] = 7;
        var state = WithMap(2, 2, new[] { 1 }, terrain);

        AssertEq(state.TerrainTagAt(0, 0), 7,
            "**the one tile the layer covers carries chip 1 and terrain 7**");
        foreach (var (x, y) in new[] { (1, 0), (0, 1), (1, 1) })
        {
            AssertEq(state.TerrainTagAt(x, y), -1,
                "**and the tile at (" + x + ", " + y + ") is not covered by "
                    + "the layer** — a reader that read past the end would "
                    + "have reported the first entry for a quarter of the map");
        }
    }

    /// <summary>
    /// The second row reads itself and not the first.
    /// </summary>
    /// <remarks>
    /// <strong>Both rows carry the same chip here, which is exactly the
    /// trap</strong> — a reader that read the first row for every row would
    /// pass a chip test and fail a terrain one, so the two rows carry
    /// <em>different</em> chips and the numbers are 4 and 7. <strong>This is
    /// the test that says which row a tile is on.</strong>
    /// </remarks>
    public void Test_TheSecondRowReadsItselfAndNotTheFirst()
    {
        var terrain = new int[6];
        terrain[2] = 4;
        terrain[3] = 7;
        // **Drei Spalten und zwei Zeilen, und die Zeilen tragen
        // unterschiedliche Chips.**
        var state = WithMap(3, 2, new[] { 2, 2, 2, 3, 3, 3 }, terrain);

        AssertEq(state.TerrainTagAt(0, 0), 4, "the first row carries chip 2");
        AssertEq(state.TerrainTagAt(0, 1), 7,
            "**and the second row carries chip 3** — a reader that indexed "
                + "the layer by the column alone would have read the first "
                + "row here and said 4");
        AssertEq(state.TerrainTagAt(2, 1), 7,
            "**and the last tile of the last row is also 7** — the index is "
                + "column plus row times the width, and the width is three");
    }
}