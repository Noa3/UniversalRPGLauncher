using Godot;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kInputMapper : TestBase
{
    public void Test_DefaultKeyboardAndControllerActionsAreMapped()
    {
        var mapper = new Rm2kInputMapper();

        AssertEq(mapper.Resolve(new InputEventKey { Keycode = Key.Left, Pressed = true }), Rm2kInputAction.MoveLeft);
        AssertEq(mapper.Resolve(new InputEventKey { Keycode = Key.Enter, Pressed = true }), Rm2kInputAction.Confirm);
        AssertEq(mapper.Resolve(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }), Rm2kInputAction.Confirm);
        AssertEq(mapper.Resolve(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = true }), Rm2kInputAction.Cancel);
    }

    public void Test_CustomBindingsReplaceDefaultsAndUnboundEventsAreIgnored()
    {
        var mapper = new Rm2kInputMapper();
        AssertTrue(mapper.BindKey(Rm2kInputAction.Confirm, Key.F));
        AssertEq(mapper.Resolve(new InputEventKey { Keycode = Key.F, Pressed = true }), Rm2kInputAction.Confirm);
        AssertEq(mapper.Resolve(new InputEventKey { Keycode = Key.Enter, Pressed = true }), Rm2kInputAction.None);
        AssertEq(mapper.Resolve(new InputEventKey { Keycode = Key.F, Pressed = false }), Rm2kInputAction.None);
    }

    public void Test_TouchZonesProduceDirectionalAndActionInput()
    {
        var mapper = new Rm2kInputMapper();
        mapper.SetTouchViewport(new Vector2(320, 240));

        AssertEq(mapper.Resolve(new InputEventScreenTouch { Position = new Vector2(16, 120), Pressed = true }), Rm2kInputAction.MoveLeft);
        AssertEq(mapper.Resolve(new InputEventScreenTouch { Position = new Vector2(304, 120), Pressed = true }), Rm2kInputAction.MoveRight);
        AssertEq(mapper.Resolve(new InputEventScreenTouch { Position = new Vector2(160, 224), Pressed = true }), Rm2kInputAction.Confirm);
        AssertEq(mapper.Resolve(new InputEventScreenTouch { Position = new Vector2(160, 120), Pressed = true }), Rm2kInputAction.None);
    }
}
