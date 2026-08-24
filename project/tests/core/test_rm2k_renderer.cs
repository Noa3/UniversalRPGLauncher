using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kRenderer : TestBase
{
    public void Test_FramebufferStoresSeparateLowerAndUpperLayers()
    {
        var framebuffer = new VirtualFramebuffer(2, 2);
        framebuffer.SetTile(RenderLayer.Lower, 0, 0, 11);
        framebuffer.SetTile(RenderLayer.Upper, 0, 0, 22);

        AssertEq(framebuffer.GetTile(RenderLayer.Lower, 0, 0), 11);
        AssertEq(framebuffer.GetTile(RenderLayer.Upper, 0, 0), 22);
        AssertEq(framebuffer.GetTile(RenderLayer.Lower, 1, 1), 0);
    }

    public void Test_FramebufferRejectsOutOfBoundsWrites()
    {
        var framebuffer = new VirtualFramebuffer(2, 2);
        AssertFalse(framebuffer.TrySetTile(RenderLayer.Lower, -1, 0, 5));
        AssertFalse(framebuffer.TrySetTile(RenderLayer.Upper, 2, 0, 5));
        AssertEq(framebuffer.GetTile(RenderLayer.Lower, 0, 0), 0);
    }

    public void Test_RendererAdapterLoadsParsedMapLayers()
    {
        var adapter = new Rm2kRendererAdapter();
        var lower = new Godot.Collections.Array();
        lower.Add(101);
        lower.Add(102);
        var upper = new Godot.Collections.Array();
        upper.Add(201);
        upper.Add(202);
        var data = new Godot.Collections.Dictionary
        {
            { "width", 2 },
            { "height", 1 },
            { "lower_layer", lower },
            { "upper_layer", upper }
        };

        var result = adapter.CreateFramebuffer(data);

        AssertTrue(result.Success, result.Error);
        AssertTrue(result.Framebuffer != null);
        if (!result.Success || result.Framebuffer == null)
        {
            return;
        }
        AssertEq(result.Framebuffer.GetTile(RenderLayer.Lower, 1, 0), 102);
        AssertEq(result.Framebuffer.GetTile(RenderLayer.Upper, 0, 0), 201);
    }

    public void Test_RendererAdapterRejectsMalformedLayerData()
    {
        var adapter = new Rm2kRendererAdapter();
        var data = new Godot.Collections.Dictionary
        {
            { "width", 2 },
            { "height", 1 },
            { "lower_layer", new[] { 101 } },
            { "upper_layer", new[] { 201, 202 } },
        };

        var result = adapter.CreateFramebuffer(data);

        AssertFalse(result.Success);
        AssertTrue(result.Framebuffer == null);
    }
}
