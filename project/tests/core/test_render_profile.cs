using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRenderProfile : TestBase
{
    public void Test_FaithfulModeRequiresIntegerScaling()
    {
        var profile = new RenderProfile();

        AssertEq(profile.Mode, RenderCompatibilityMode.Faithful);
        AssertTrue(profile.IntegerScaling);
        AssertFalse(profile.TrySetIntegerScaling(false));
        AssertTrue(profile.TrySetMode(RenderCompatibilityMode.Enhanced));
        AssertTrue(profile.TrySetIntegerScaling(false));
        AssertFalse(profile.IntegerScaling);
    }

    public void Test_IntegerScaleIsBoundedAndViewportFits()
    {
        var profile = new RenderProfile();
        AssertFalse(profile.TrySetIntegerScale(0));
        AssertFalse(profile.TrySetIntegerScale(9));
        AssertTrue(profile.TrySetIntegerScale(2));

        AssertEq(profile.CalculateViewportSize(new Vector2I(320, 240), new Vector2I(1920, 1080)), new Vector2I(640, 480));
        AssertEq(profile.CalculateViewportSize(new Vector2I(320, 240), new Vector2I(100, 100)), new Vector2I(320, 240));
        AssertEq(profile.CalculateViewportSize(Vector2I.Zero, new Vector2I(1920, 1080)), Vector2I.Zero);
    }
}
