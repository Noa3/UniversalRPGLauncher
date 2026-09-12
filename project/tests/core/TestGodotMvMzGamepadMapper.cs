using Godot;
using UniversalRPG.Platform;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestGodotMvMzGamepadMapper : TestBase
{
    public void Test_MapsGodotButtonsToStandardBrowserLayout()
    {
        AssertButton(JoyButton.A,0);AssertButton(JoyButton.B,1);AssertButton(JoyButton.X,2);AssertButton(JoyButton.Y,3);
        AssertButton(JoyButton.LeftShoulder,4);AssertButton(JoyButton.RightShoulder,5);
        AssertButton(JoyButton.Back,8);AssertButton(JoyButton.Start,9);AssertButton(JoyButton.LeftStick,10);AssertButton(JoyButton.RightStick,11);
        AssertButton(JoyButton.DpadUp,12);AssertButton(JoyButton.DpadDown,13);AssertButton(JoyButton.DpadLeft,14);AssertButton(JoyButton.DpadRight,15);AssertButton(JoyButton.Guide,16);
        AssertFalse(GodotStandardGamepadMapper.TryMapButton(JoyButton.Misc1,out _));
    }

    public void Test_MapsFourStandardStickAxes()
    {
        AssertAxis(JoyAxis.LeftX,0);AssertAxis(JoyAxis.LeftY,1);AssertAxis(JoyAxis.RightX,2);AssertAxis(JoyAxis.RightY,3);
        AssertFalse(GodotStandardGamepadMapper.TryMapAxis(JoyAxis.TriggerLeft,out _));
    }

    public void Test_MapsTriggerAxesToStandardButtons()
    {
        AssertTrue(GodotStandardGamepadMapper.TryMapTrigger(JoyAxis.TriggerLeft,0.75,out var left,out var value));
        AssertEq(left,6);AssertTrue(System.Math.Abs(value-0.75)<0.0001);
        AssertTrue(GodotStandardGamepadMapper.TryMapTrigger(JoyAxis.TriggerRight,-1,out var right,out var rest));
        AssertEq(right,7);AssertTrue(System.Math.Abs(rest)<0.0001);
        AssertFalse(GodotStandardGamepadMapper.TryMapTrigger(JoyAxis.LeftX,0,out _,out _));
    }

    private void AssertButton(JoyButton button,int expected){AssertTrue(GodotStandardGamepadMapper.TryMapButton(button,out var actual));AssertEq(actual,expected);}
    private void AssertAxis(JoyAxis axis,int expected){AssertTrue(GodotStandardGamepadMapper.TryMapAxis(axis,out var actual));AssertEq(actual,expected);}
}
