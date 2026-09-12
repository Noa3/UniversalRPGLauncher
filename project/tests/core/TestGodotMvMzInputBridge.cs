using System;
using Godot;
using UniversalRPG.Platform;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestGodotMvMzInputBridge : TestBase
{
    public void Test_MapsOriginalMvKeyboardBindings()
    {
        AssertMap(Key.Tab, 9);
        AssertMap(Key.Enter, 13);
        AssertMap(Key.Shift, 16);
        AssertMap(Key.Ctrl, 17);
        AssertMap(Key.Alt, 18);
        AssertMap(Key.Escape, 27);
        AssertMap(Key.Space, 32);
        AssertMap(Key.Pageup, 33);
        AssertMap(Key.Pagedown, 34);
        AssertMap(Key.Left, 37);
        AssertMap(Key.Up, 38);
        AssertMap(Key.Right, 39);
        AssertMap(Key.Down, 40);
        AssertMap(Key.Insert, 45);
        AssertMap(Key.Q, 81);
        AssertMap(Key.W, 87);
        AssertMap(Key.X, 88);
        AssertMap(Key.Z, 90);
        AssertMap(Key.Kp0, 96);
        AssertMap(Key.Kp2, 98);
        AssertMap(Key.Kp4, 100);
        AssertMap(Key.Kp6, 102);
        AssertMap(Key.Kp8, 104);
        AssertMap(Key.F9, 120);
    }

    public void Test_MapsLettersDigitsFunctionAndKeypadWithoutLayoutGuessing()
    {
        AssertMap(Key.A, 65);
        AssertMap(Key.M, 77);
        AssertMap(Key.Key0, 48);
        AssertMap(Key.Key9, 57);
        AssertMap(Key.Kp9, 105);
        AssertMap(Key.F1, 112);
        AssertMap(Key.F12, 123);
        AssertMap(Key.Numlock, 144);
        AssertMap(Key.Scrolllock, 145);
    }

    public void Test_UnmappedKeysRemainAvailableToHostUi()
    {
        AssertFalse(GodotLegacyKeyMapper.TryMap(Key.None, out var code));
        AssertEq(code, 0);
    }

    public void Test_InputEventFallsBackToPhysicalKeycodeOnlyWhenLogicalCodeMissing()
    {
        var physical = new InputEventKey { Keycode = Key.None, PhysicalKeycode = Key.Z };
        AssertTrue(GodotLegacyKeyMapper.TryMap(physical, out var physicalCode));
        AssertEq(physicalCode, 90);

        var logical = new InputEventKey { Keycode = Key.Q, PhysicalKeycode = Key.Z };
        AssertTrue(GodotLegacyKeyMapper.TryMap(logical, out var logicalCode));
        AssertEq(logicalCode, 81);
    }

    private void AssertMap(Key key, int expected)
    {
        AssertTrue(GodotLegacyKeyMapper.TryMap(key, out var actual), key.ToString());
        AssertEq(actual, expected, key.ToString());
    }
}
