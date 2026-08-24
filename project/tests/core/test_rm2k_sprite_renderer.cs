using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kSpriteRenderer : TestBase
{
    public void Test_CameraClampsToMapBounds()
    {
        var camera = new Rm2kCameraState(20, 15, 8, 6);
        camera.SetCenter(100, -4);

        AssertEq(camera.CenterX, 16);
        AssertEq(camera.CenterY, 3);
        AssertEq(camera.ViewportWidth, 8);
        AssertEq(camera.ViewportHeight, 6);
    }

    public void Test_CameraViewportRejectsInvalidDimensions()
    {
        var threw = false;
        try
        {
            _ = new Rm2kCameraState(20, 15, 0, 6);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            threw = true;
        }
        AssertTrue(threw);
    }

    public void Test_SpriteAdapterBuildsPlayerAndEventDescriptors()
    {
        var adapter = new Rm2kSpriteAdapter();
        var events = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            new()
            {
                { "id", 7 }, { "x", 3 }, { "y", 4 }, { "name", "Chest" },
                { "pages", new Godot.Collections.Array<Godot.Collections.Dictionary>() }
            }
        };
        var map = new Godot.Collections.Dictionary
        {
            { "width", 10 }, { "height", 8 }, { "events", events }
        };

        var result = adapter.BuildDescriptors(map, 2, 1);

        AssertTrue(result.Success, result.Error);
        AssertEq(result.Descriptors.Count, 2);
        AssertEq(result.Descriptors[0].Kind, Rm2kSpriteKind.Player);
        AssertEq(result.Descriptors[1].EventId, 7);
        AssertEq(result.Descriptors[1].Name, "Chest");
    }

    public void Test_SpriteAdapterRejectsInvalidEventCoordinates()
    {
        var adapter = new Rm2kSpriteAdapter();
        var events = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            new() { { "id", 7 }, { "x", 99 }, { "y", 4 } }
        };
        var map = new Godot.Collections.Dictionary
        {
            { "width", 10 }, { "height", 8 }, { "events", events }
        };

        var result = adapter.BuildDescriptors(map, 2, 1);

        AssertFalse(result.Success);
        AssertEq(result.Descriptors.Count, 0);
    }
}
