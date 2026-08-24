using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

public enum Rm2kEventTrigger
{
    Autorun = 0,
    Parallel = 1,
    Action = 2,
    Touch = 3,
}

/// <summary>
/// Selects the active RM2K event page without executing imported scripts.
/// RM2K resolves pages from highest index to lowest index; the first matching
/// page wins. Unknown condition fields fail closed instead of being ignored.
/// </summary>
public static class Rm2kEventPageSelector
{
    public static Rm2kMap.EventPage? Select(
        Rm2kMap.Event pEvent,
        GameSimulationState pState,
        Rm2kEventTrigger pTrigger)
    {
        if (pEvent == null) throw new ArgumentNullException(nameof(pEvent));
        if (pState == null) throw new ArgumentNullException(nameof(pState));

        for (var index = pEvent.Pages.Count - 1; index >= 0; index--)
        {
            var page = pEvent.Pages[index];
            if (page.Trigger != (int)pTrigger || !ConditionsMatch(page, pState))
            {
                continue;
            }
            return page;
        }
        return null;
    }

    private static bool ConditionsMatch(Rm2kMap.EventPage pPage, GameSimulationState pState)
    {
        if (pPage.Conditions.Count == 0)
        {
            return true;
        }

        foreach (var condition in pPage.Conditions)
        {
            switch (condition.Key)
            {
                case "switch_id":
                    if (!TryInt(condition.Value, out var switchId)
                        || switchId < 1 || switchId > GameSimulationState.MaxSwitches
                        || !TryBool(pPage.Conditions, "switch_value", out var switchValue)
                        || !ReadSwitch(pState, switchId, out var actualSwitch)
                        || actualSwitch != switchValue)
                    {
                        return false;
                    }
                    break;
                case "switch_value":
                    break;
                case "variable_id":
                    if (!TryInt(condition.Value, out var variableId)
                        || variableId < 1 || variableId > GameSimulationState.MaxVariables
                        || !TryInt(pPage.Conditions, "variable_value", out var expectedValue)
                        || ReadVariable(pState, variableId) != expectedValue)
                    {
                        return false;
                    }
                    break;
                case "variable_value":
                    break;
                default:
                    return false;
            }
        }
        return true;
    }

    private static bool ReadSwitch(GameSimulationState pState, int pId, out bool pValue)
    {
        var index = pId - 1;
        if (index < 0 || index >= pState.Switches.Count)
        {
            pValue = false;
            return false;
        }
        pValue = pState.Switches[index];
        return true;
    }

    private static int ReadVariable(GameSimulationState pState, int pId)
    {
        var index = pId - 1;
        return index >= 0 && index < pState.Variables.Count ? pState.Variables[index] : 0;
    }

    private static bool TryInt(System.Collections.Generic.Dictionary<string, object> pValues, string pKey, out int pResult)
    {
        if (pValues.TryGetValue(pKey, out var value))
        {
            return TryInt(value, out pResult);
        }
        pResult = 0;
        return false;
    }

    private static bool TryInt(object pValue, out int pResult)
    {
        switch (pValue)
        {
            case int value: pResult = value; return true;
            case long value when value >= int.MinValue && value <= int.MaxValue:
                pResult = (int)value; return true;
            case byte value: pResult = value; return true;
            default: pResult = 0; return false;
        }
    }

    private static bool TryBool(System.Collections.Generic.Dictionary<string, object> pValues, string pKey, out bool pResult)
    {
        if (!pValues.TryGetValue(pKey, out var value) || value is not bool boolean)
        {
            pResult = false;
            return false;
        }
        pResult = boolean;
        return true;
    }
}
