using System;
using System.Collections.Generic;
using System.Text.Json;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>Bounded, JSON-only persistence for deterministic RM2K simulation state.</summary>
public static class Rm2kSimulationSaveCodec
{
    public const int MaxPayloadBytes = 1024 * 1024;
    private const int CurrentVersion = 1;

    public sealed class SaveData
    {
        public int Version { get; set; } = CurrentVersion;
        public string GameTitle { get; set; } = "";
        public int MapId { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public byte FacingDirection { get; set; }
        public int Gold { get; set; }
        public int FrameCount { get; set; }
        public int Steps { get; set; }
        public bool Timer1Active { get; set; }
        public bool Timer2Active { get; set; }
        public int Timer1Seconds { get; set; }
        public int Timer2Seconds { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public List<bool> PassableTiles { get; set; } = new();
        public List<bool> Switches { get; set; } = new();
        public List<int> Variables { get; set; } = new();
        public Dictionary<int, int> ItemCounts { get; set; } = new();
        public List<int> PartyMemberIds { get; set; } = new();
        public int ActiveActorIndex { get; set; }
        public string CurrentScene { get; set; } = "Menu";
        public List<string> SceneStack { get; set; } = new();
        public long SaveTimestamp { get; set; }
        public string SaveComment { get; set; } = "";
    }

    public static string Serialize(GameSimulationState pState)
    {
        ArgumentNullException.ThrowIfNull(pState);
        var json = JsonSerializer.Serialize(Capture(pState));
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxPayloadBytes)
        {
            throw new InvalidOperationException("Simulation save exceeds the bounded payload limit.");
        }
        return json;
    }

    public static bool TryRestore(string pJson, GameSimulationState pState, out string pError)
    {
        pError = "";
        if (string.IsNullOrWhiteSpace(pJson) || System.Text.Encoding.UTF8.GetByteCount(pJson) > MaxPayloadBytes)
        {
            pError = "Save payload is empty or exceeds the bounded size limit.";
            return false;
        }
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(pJson);
            if (data == null) { pError = "Save payload is null."; return false; }
            if (data.Version != CurrentVersion) { pError = $"Unsupported save version {data.Version}."; return false; }
            Validate(data);
            Apply(data, pState);
            return true;
        }
        catch (JsonException) { pError = "Save payload is not valid JSON."; return false; }
        catch (ArgumentException exception) { pError = exception.Message; return false; }
        catch (InvalidOperationException exception) { pError = exception.Message; return false; }
    }

    private static SaveData Capture(GameSimulationState pState)
    {
        var data = new SaveData
        {
            GameTitle = pState.GameTitle,
            MapId = pState.MapId, MapX = pState.MapX, MapY = pState.MapY,
            FacingDirection = pState.FacingDirection, Gold = pState.Gold,
            FrameCount = pState.FrameCount, Steps = pState.Steps,
            Timer1Active = pState.Timer1Active, Timer2Active = pState.Timer2Active,
            Timer1Seconds = pState.Timer1Seconds, Timer2Seconds = pState.Timer2Seconds,
            MapWidth = pState.MapWidth, MapHeight = pState.MapHeight,
            ActiveActorIndex = pState.ActiveActorIndex, CurrentScene = pState.CurrentScene,
            SaveTimestamp = pState.SaveTimestamp, SaveComment = pState.SaveComment,
        };
        foreach (var value in pState.PassableTiles) data.PassableTiles.Add(value);
        foreach (var value in pState.Switches) data.Switches.Add(value);
        foreach (var value in pState.Variables) data.Variables.Add(value);
        foreach (var pair in pState.ItemCounts) data.ItemCounts[pair.Key] = pair.Value;
        foreach (var value in pState.PartyMemberIds) data.PartyMemberIds.Add(value);
        foreach (var value in pState.SceneStack) data.SceneStack.Add(value);
        return data;
    }

    private static void Validate(SaveData pData)
    {
        if (pData.MapId < 0 || pData.MapId > GameSimulationState.MaxMapId || pData.MapWidth <= 0 || pData.MapHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pData.MapId), "Save map metadata is outside bounds.");
        if (pData.MapWidth > 512 || pData.MapHeight > 512 || pData.MapWidth * pData.MapHeight != pData.PassableTiles.Count)
            throw new ArgumentException("Save passability dimensions are invalid.");
        if (pData.Switches.Count > GameSimulationState.MaxSwitches || pData.Variables.Count > GameSimulationState.MaxVariables)
            throw new ArgumentException("Save switch or variable data exceeds bounds.");
        if (pData.Timer1Seconds < 0 || pData.Timer1Seconds > 86400 || pData.Timer2Seconds < 0 || pData.Timer2Seconds > 86400)
            throw new ArgumentException("Save timer data is outside bounds.");
        if (pData.PartyMemberIds.Count > GameSimulationState.MaxPartyMembers || pData.SceneStack.Count > 64)
            throw new ArgumentException("Save party or scene data exceeds bounds.");
        if (pData.MapX < 0 || pData.MapX >= pData.MapWidth || pData.MapY < 0 || pData.MapY >= pData.MapHeight)
            throw new ArgumentException("Save player position is outside map bounds.");
    }

    private static void Apply(SaveData pData, GameSimulationState pState)
    {
        pState.ConfigureMap(pData.MapId, pData.MapWidth, pData.MapHeight, pData.PassableTiles);
        pState.MapX = pData.MapX; pState.MapY = pData.MapY; pState.FacingDirection = pData.FacingDirection;
        pState.Gold = pData.Gold; pState.FrameCount = pData.FrameCount; pState.Steps = pData.Steps;
        pState.Switches.Clear(); foreach (var value in pData.Switches) pState.Switches.Add(value);
        pState.Variables.Clear(); foreach (var value in pData.Variables) pState.Variables.Add(value);
        pState.ItemCounts.Clear(); foreach (var pair in pData.ItemCounts) pState.ItemCounts[pair.Key] = pair.Value;
        pState.PartyMemberIds.Clear(); foreach (var value in pData.PartyMemberIds) pState.PartyMemberIds.Add(value);
        pState.SceneStack.Clear(); foreach (var value in pData.SceneStack) pState.SceneStack.Add(value);
        pState.ActiveActorIndex = pData.ActiveActorIndex; pState.CurrentScene = pData.CurrentScene;
        pState.SaveTimestamp = pData.SaveTimestamp; pState.SaveComment = pData.SaveComment;
        pState.StopTimer(1); pState.StopTimer(2);
        if (pData.Timer1Active) pState.SetTimer(1, pData.Timer1Seconds);
        if (pData.Timer2Active) pState.SetTimer(2, pData.Timer2Seconds);
    }
}
