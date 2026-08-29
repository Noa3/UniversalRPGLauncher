using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Deterministic simulation state model for RPG Maker 2000/2003 games.
/// All fields mirror the documented RM2K/LCF internal state layout.
/// No JavaScript, DLL, or native execution.
/// </summary>
public sealed class GameSimulationState
{
    public const int MaxPartyMembers = 4;
    public const int MaxTroopMembers = 8;
    public const int MaxActorId = 50000;
    public const int MaxMapId = 1000;
    public const int MaxSwitches = 50000;
    public const int MaxVariables = 50000;

    public string GameTitle { get; init; } = "";
    public int MapId { get; set; } = 0;
    public int MapX { get; set; } = 0;
    public int MapY { get; set; } = 0;
    public byte FacingDirection { get; set; } = 2;
    public int Gold { get; set; } = 0;
    public int FrameCount { get; set; } = 0;
    public int Steps { get; set; } = 0;
    public int FrameRate { get; set; } = 60;
    public bool Timer1Active { get; private set; }
    public bool Timer2Active { get; private set; }
    public int Timer1Seconds { get; private set; }
    public int Timer2Seconds { get; private set; }
    public bool IsPaused { get; set; }
    public bool IsMenuOpen { get; set; }
    public bool IsSaveEnabled { get; set; } = true;
    public bool IsTransferPending { get; set; }
    public int PendingMapId { get; set; } = 0;
    public int PendingX { get; set; } = 0;
    public int PendingY { get; set; } = 0;

    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }
    public Godot.Collections.Array<bool> PassableTiles { get; init; } = new();

    // Switches (bool or byte for RM2000 compatibility)
    public Godot.Collections.Array<bool> Switches { get; init; } = new();

    // Variables (int)
    public Godot.Collections.Array<int> Variables { get; init; } = new();

    // Inventory counts keyed by RM2K item ID.
    public Godot.Collections.Dictionary<int, int> ItemCounts { get; init; } = new();

    // Party members (max 4)
    public Godot.Collections.Array<int> PartyMemberIds { get; init; } = new();
    public int ActiveActorIndex { get; set; } = 0;

    // Actors (mutable battle stats)
    public Godot.Collections.Dictionary<int, Godot.Collections.Dictionary> ActorState { get; init; } = new();

    // Troop (active battle)
    public int ActiveTroopId { get; set; } = -1;
    public Godot.Collections.Array<Godot.Collections.Dictionary> TroopMembers { get; init; } = new();
    public bool IsBattleActive { get; set; }
    public int BattleTurn { get; set; }
    public int BattlePhase { get; set; } // 0=initial, 1=player, 2=enemy, 3=reward, 4=escape, -1=none

    // Common events (parallel execution)
    public Godot.Collections.Array<int> CommonEventIds { get; init; } = new();
    public int CommonEventCounter { get; set; }

    // Scene stack
    public Godot.Collections.Array<string> SceneStack { get; init; } = new();
    public string CurrentScene { get; set; } = "Menu";

    // Audio positions
    public double BgmPosition { get; set; }
    public double BgsPosition { get; set; }
    public double MePosition { get; set; }
    public double SePosition { get; set; }

    // Save state
    public long SaveTimestamp { get; set; }
    public string SaveComment { get; set; } = "";

    // Debug diagnostics
    public Godot.Collections.Array<string> Diagnostics { get; init; } = new();

    public void AddDiagnostic(string pMessage)
    {
        if (Diagnostics.Count < 100)
        {
            Diagnostics.Add(pMessage);
        }
    }

    public void ClearDiagnostics()
    {
        Diagnostics.Clear();
    }

    private int _timer1TickRemainder;
    private int _timer2TickRemainder;

    public void SetTimer(int pTimerId, int pSeconds)
    {
        if (pTimerId is not (1 or 2) || pSeconds < 0 || pSeconds > 86400)
        {
            throw new ArgumentOutOfRangeException(nameof(pSeconds));
        }
        if (pTimerId == 1) { Timer1Seconds = pSeconds; Timer1Active = true; _timer1TickRemainder = 0; }
        else { Timer2Seconds = pSeconds; Timer2Active = true; _timer2TickRemainder = 0; }
    }

    public void StopTimer(int pTimerId)
    {
        if (pTimerId == 1) Timer1Active = false;
        else if (pTimerId == 2) Timer2Active = false;
        else throw new ArgumentOutOfRangeException(nameof(pTimerId));
    }

    public void AdvanceTimers(int pSimulationTicks)
    {
        if (pSimulationTicks < 0) throw new ArgumentOutOfRangeException(nameof(pSimulationTicks));
        var timer1Seconds = Timer1Seconds;
        var timer1Active = Timer1Active;
        AdvanceTimer(ref timer1Seconds, ref timer1Active, ref _timer1TickRemainder, pSimulationTicks);
        Timer1Seconds = timer1Seconds;
        Timer1Active = timer1Active;
        var timer2Seconds = Timer2Seconds;
        var timer2Active = Timer2Active;
        AdvanceTimer(ref timer2Seconds, ref timer2Active, ref _timer2TickRemainder, pSimulationTicks);
        Timer2Seconds = timer2Seconds;
        Timer2Active = timer2Active;
    }

    private static void AdvanceTimer(ref int pSeconds, ref bool pActive, ref int pRemainder, int pTicks)
    {
        if (!pActive) return;
        pRemainder += pTicks;
        var elapsedSeconds = pRemainder / 60;
        pRemainder %= 60;
        pSeconds = Math.Max(0, pSeconds - elapsedSeconds);
        if (pSeconds == 0) pActive = false;
    }

    public void ConfigureMap(int pMapId, int pWidth, int pHeight, IEnumerable<bool> pPassableTiles)
    {
        if (pMapId < 0 || pMapId > MaxMapId || pWidth <= 0 || pHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pWidth), "Map identity and dimensions are outside simulation bounds.");
        }
        var expected = checked(pWidth * pHeight);
        var tiles = new List<bool>(expected);
        foreach (var passable in pPassableTiles)
        {
            if (tiles.Count == expected)
            {
                throw new ArgumentException("Passability data contains more tiles than the map.", nameof(pPassableTiles));
            }
            tiles.Add(passable);
        }
        if (tiles.Count != expected)
        {
            throw new ArgumentException("Passability data does not cover the complete map.", nameof(pPassableTiles));
        }
        MapId = pMapId;
        MapWidth = pWidth;
        MapHeight = pHeight;
        PassableTiles.Clear();
        foreach (var passable in tiles)
        {
            PassableTiles.Add(passable);
        }
        MapX = Math.Clamp(MapX, 0, pWidth - 1);
        MapY = Math.Clamp(MapY, 0, pHeight - 1);
    }

    public bool TryMove(int pDeltaX, int pDeltaY)
    {
        if (Math.Abs(pDeltaX) + Math.Abs(pDeltaY) != 1)
        {
            AddDiagnostic("Movement requires exactly one cardinal tile step.");
            return false;
        }
        var targetX = MapX + pDeltaX;
        var targetY = MapY + pDeltaY;
        FacingDirection = (byte)(pDeltaX > 0 ? 6 : pDeltaX < 0 ? 4 : pDeltaY > 0 ? 2 : 8);
        if (MapWidth <= 0 || MapHeight <= 0 || targetX < 0 || targetX >= MapWidth || targetY < 0 || targetY >= MapHeight)
        {
            AddDiagnostic("Movement blocked by map bounds.");
            return false;
        }
        if (!PassableTiles[targetY * MapWidth + targetX])
        {
            AddDiagnostic("Movement blocked by tile passability.");
            return false;
        }
        MapX = targetX;
        MapY = targetY;
        Steps += 1;
        return true;
    }

    public void Reset()
    {
        MapId = 0; MapX = 0; MapY = 0; FacingDirection = 2;
        Gold = 0; FrameCount = 0; Steps = 0;
        Timer1Active = false; Timer2Active = false; Timer1Seconds = 0; Timer2Seconds = 0; _timer1TickRemainder = 0; _timer2TickRemainder = 0;
        IsPaused = false; IsMenuOpen = false; IsSaveEnabled = true;
        IsTransferPending = false; PendingMapId = 0; PendingX = 0; PendingY = 0; ActiveActorIndex = 0;
        MapWidth = 0; MapHeight = 0; PassableTiles.Clear();
        ActiveTroopId = -1; IsBattleActive = false; BattleTurn = 0; BattlePhase = -1;
        CommonEventCounter = 0;
        SceneStack.Clear(); SceneStack.Add("Menu"); CurrentScene = "Menu";
        BgmPosition = 0; BgsPosition = 0; MePosition = 0; SePosition = 0;
        SaveTimestamp = 0; SaveComment = "";
        ClearDiagnostics();
    }
}
