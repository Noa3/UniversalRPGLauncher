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
        var conditions = pPage.Conditions;
        if (conditions.Count == 0) return true;

        if (conditions.TryGetValue("switch_id", out var legacySwitchId))
        {
            if (!TryInt(legacySwitchId, out var id) || !TryBool(conditions, "switch_value", out var expected)
                || !ReadSwitch(pState, id, out var actual) || actual != expected) return false;
        }

        if (!EvaluateSwitchCondition(conditions, "a", pState)
            || !EvaluateSwitchCondition(conditions, "b", pState)) return false;

        if (TryBool(conditions, "variable_enabled", out var variableEnabled) && variableEnabled)
        {
            if (!TryInt(conditions, "variable_id", out var variableId)
                || variableId < 1 || variableId > GameSimulationState.MaxVariables
                || !TryInt(conditions, "variable_value", out var expectedValue)
                || !Compare(ReadVariable(pState, variableId), expectedValue, conditions)) return false;
        }

        foreach (var condition in conditions)
        {
            if (condition.Key is "switch_id" or "switch_value" or "switch_a_enabled" or "switch_a_id"
                or "switch_b_enabled" or "switch_b_id" or "variable_enabled" or "variable_id"
                or "variable_value" or "compare_operator") continue;
            if (condition.Key == "item_enabled" && condition.Value is bool itemEnabled)
            {
                if (itemEnabled && (!TryInt(conditions, "item_id", out var itemId) || itemId < 1
                    || !pState.ItemCounts.TryGetValue(itemId, out var itemCount) || itemCount <= 0)) return false;
                continue;
            }
            if (condition.Key == "actor_enabled" && condition.Value is bool actorEnabled)
            {
                if (actorEnabled && (!TryInt(conditions, "actor_id", out var actorId) || actorId < 1 || !pState.PartyMemberIds.Contains(actorId))) return false;
                continue;
            }
            if (condition.Key is "item_id" or "actor_id") continue;
            if (condition.Key is "timer_enabled" or "timer2_enabled")
            {
                if (condition.Value is bool enabled && enabled) return false;
                continue;
            }
            return false;
        }
        return true;
    }

    private static bool EvaluateSwitchCondition(System.Collections.Generic.Dictionary<string, object> pConditions, string pSuffix, GameSimulationState pState)
    {
        var enabledKey = $"switch_{pSuffix}_enabled";
        if (!TryBool(pConditions, enabledKey, out var enabled) || !enabled) return true;
        if (!TryInt(pConditions, $"switch_{pSuffix}_id", out var id) || id < 1 || id > GameSimulationState.MaxSwitches
            || !ReadSwitch(pState, id, out var actual)) return false;
        return actual;
    }

    private static bool Compare(int pActual, int pExpected, System.Collections.Generic.Dictionary<string, object> pConditions)
    {
        var op = 0;
        if (pConditions.TryGetValue("compare_operator", out var rawOperator) && !TryInt(rawOperator, out op)) return false;
        return op switch
        {
            0 => pActual == pExpected,
            1 => pActual >= pExpected,
            2 => pActual <= pExpected,
            3 => pActual > pExpected,
            4 => pActual < pExpected,
            5 => pActual != pExpected,
            _ => false,
        };
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
