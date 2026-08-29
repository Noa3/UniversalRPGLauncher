using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Rm2k.Input;

public enum Rm2kInputAction
{
    None,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Confirm,
    Cancel,
    Menu,
}

/// <summary>
/// Converts host input events into engine-neutral RM2K actions. Bindings are
/// local to this mapper and never execute imported game input/script code.
/// </summary>
public sealed class Rm2kInputMapper
{
    public const int MaxBindings = 64;
    private const float TouchEdgeFraction = 0.25f;

    private readonly Dictionary<Key, Rm2kInputAction> _keys = new();
    private readonly Dictionary<JoyButton, Rm2kInputAction> _buttons = new();
    private Vector2 _touchViewport;

    public Rm2kInputMapper()
    {
        AddDefaultKey(Rm2kInputAction.MoveUp, Key.Up);
        AddDefaultKey(Rm2kInputAction.MoveUp, Key.W);
        AddDefaultKey(Rm2kInputAction.MoveDown, Key.Down);
        AddDefaultKey(Rm2kInputAction.MoveDown, Key.S);
        AddDefaultKey(Rm2kInputAction.MoveLeft, Key.Left);
        AddDefaultKey(Rm2kInputAction.MoveLeft, Key.A);
        AddDefaultKey(Rm2kInputAction.MoveRight, Key.Right);
        AddDefaultKey(Rm2kInputAction.MoveRight, Key.D);
        AddDefaultKey(Rm2kInputAction.Confirm, Key.Enter);
        AddDefaultKey(Rm2kInputAction.Confirm, Key.Space);
        AddDefaultKey(Rm2kInputAction.Confirm, Key.KpEnter);
        AddDefaultKey(Rm2kInputAction.Cancel, Key.Escape);
        AddDefaultKey(Rm2kInputAction.Menu, Key.M);
        _buttons[JoyButton.A] = Rm2kInputAction.Confirm;
        _buttons[JoyButton.B] = Rm2kInputAction.Cancel;
    }

    public bool BindKey(Rm2kInputAction pAction, Key pKey)
    {
        if (pAction == Rm2kInputAction.None || pKey == Key.None)
        {
            return false;
        }
        RemoveAction(_keys, pAction);
        if (_keys.Count >= MaxBindings)
        {
            return false;
        }
        _keys[pKey] = pAction;
        return true;
    }

    private void AddDefaultKey(Rm2kInputAction pAction, Key pKey) => _keys[pKey] = pAction;

    public bool BindJoypad(Rm2kInputAction pAction, JoyButton pButton)
    {
        if (pAction == Rm2kInputAction.None || pButton == JoyButton.Invalid)
        {
            return false;
        }
        RemoveAction(_buttons, pAction);
        if (_buttons.Count >= MaxBindings)
        {
            return false;
        }
        _buttons[pButton] = pAction;
        return true;
    }

    public void SetTouchViewport(Vector2 pViewport)
    {
        _touchViewport = new Vector2(Math.Max(0f, pViewport.X), Math.Max(0f, pViewport.Y));
    }

    public Rm2kInputAction Resolve(InputEvent pEvent)
    {
        if (pEvent == null || !pEvent.IsPressed())
        {
            return Rm2kInputAction.None;
        }
        if (pEvent is InputEventKey keyEvent && _keys.TryGetValue(keyEvent.Keycode, out var keyAction))
        {
            return keyAction;
        }
        if (pEvent is InputEventJoypadButton buttonEvent && _buttons.TryGetValue(buttonEvent.ButtonIndex, out var buttonAction))
        {
            return buttonAction;
        }
        if (pEvent is InputEventScreenTouch touchEvent)
        {
            return ResolveTouch(touchEvent.Position);
        }
        return Rm2kInputAction.None;
    }

    private Rm2kInputAction ResolveTouch(Vector2 pPosition)
    {
        if (_touchViewport.X <= 0 || _touchViewport.Y <= 0 || !new Rect2(Vector2.Zero, _touchViewport).HasPoint(pPosition))
        {
            return Rm2kInputAction.None;
        }
        if (pPosition.X < _touchViewport.X * TouchEdgeFraction) return Rm2kInputAction.MoveLeft;
        if (pPosition.X > _touchViewport.X * (1f - TouchEdgeFraction)) return Rm2kInputAction.MoveRight;
        if (pPosition.Y < _touchViewport.Y * TouchEdgeFraction) return Rm2kInputAction.MoveUp;
        if (pPosition.Y > _touchViewport.Y * (1f - TouchEdgeFraction)) return Rm2kInputAction.Confirm;
        return Rm2kInputAction.None;
    }

    private static void RemoveAction<T>(Dictionary<T, Rm2kInputAction> pBindings, Rm2kInputAction pAction)
        where T : notnull
    {
        foreach (var key in new List<T>(pBindings.Keys))
        {
            if (pBindings[key] == pAction) pBindings.Remove(key);
        }
    }
}
