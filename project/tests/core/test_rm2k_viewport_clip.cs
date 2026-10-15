using System;

using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The two viewport copies, at the pixel: they are what a screen shake moves.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are the routines the shake found broken.</strong> The camera
/// used to be clamped inside the map, so the offset was never negative and neither
/// routine had to clip on the left; the shake adds a signed offset to the same
/// origin, and then <c>TryCopyRegion</c> refused the whole region (a black screen
/// instead of a shifted map) and the over-copy indexed behind the array.
/// </para>
/// <para>
/// <strong>And the over-copy had a second defect that the shake only made
/// visible</strong>: it advanced the source on a transparent pixel but not the
/// destination, so every opaque pixel behind a transparent one in the same row was
/// written one column too far left. A wall tile drawn through it could hide the
/// wrong part of a character.
/// </para>
/// </remarks>
public partial class TestRm2kRuntimeRendering
{
    private static Rm2kPixelBuffer Layer(params (byte R, byte G, byte B, byte A)[] pPixels)
    {
        var layer = new Rm2kPixelBuffer(pPixels.Length, 1);
        for (var column = 0; column < pPixels.Length; column++)
        {
            layer.TrySetPixel(column, 0, pPixels[column].R, pPixels[column].G,
                pPixels[column].B, pPixels[column].A);
        }
        return layer;
    }

    private static Rm2kPixelBuffer Frame(int pWidth, byte pRed, byte pGreen, byte pBlue)
    {
        var frame = new Rm2kPixelBuffer(pWidth, 1);
        for (var column = 0; column < pWidth; column++)
        {
            frame.TrySetPixel(column, 0, pRed, pGreen, pBlue, 255);
        }
        return frame;
    }

    /// <summary>
    /// A transparent pixel in the upper layer keeps the frame's own pixel in place.
    /// </summary>
    /// <remarks>
    /// <strong>And the pixel behind it stays where it belongs</strong>: with four
    /// columns of transparent, red, transparent, green the frame has to read
    /// itself, red, itself, green -- and it read red, green, itself, itself before,
    /// because the destination cursor stood still on the transparent columns.
    /// </remarks>
    public void Test_DieObereSchichtBleibtAusgerichtet()
    {
        var layer = Layer((0, 0, 0, 0), (200, 0, 0, 255), (0, 0, 0, 0), (0, 200, 0, 255));
        var frame = Frame(4, 10, 20, 30);

        Rm2kEngineRuntime.CopyViewportOver(layer, frame, 0, 0);

        AssertEq(frame.Pixels[0], 10, "**and column zero keeps the frame's pixel**");
        AssertEq(frame.Pixels[4], 200, "**and the red lands in column one**");
        AssertEq(frame.Pixels[8], 10, "**and column two keeps the frame's pixel**");
        AssertEq(frame.Pixels[12], 0,
            "**and column three holds the layer's green**, whose red byte is zero --"
                + " a green pixel written one column too far left would leave the"
                + " frame's own pixel here instead");
        AssertEq(frame.Pixels[13], 200, "**and the green really is there**");
    }

    /// <summary>
    /// A negative offset shifts the layer right and keeps the frame on the left.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the case a screen shake creates</strong>: the camera can
    /// be one pixel left of the map's own origin, so the layer's first column belongs
    /// in the frame's second one and the frame's first column is what lies outside
    /// the map.
    /// </remarks>
    public void Test_EinNegativerOffsetVerschiebtDieSchichtNachRechts()
    {
        var layer = Layer((200, 0, 0, 255), (0, 200, 0, 255));
        var frame = Frame(4, 10, 20, 30);

        Rm2kEngineRuntime.CopyViewportOver(layer, frame, -1, 0);

        AssertEq(frame.Pixels[0], 10,
            "**and the pixel left of the map keeps what the frame had**");
        AssertEq(frame.Pixels[4], 200, "**and the layer starts in column one**");
        AssertEq(frame.Pixels[9], 200,
            "**and continues in column two**, where the layer's green sits");
        AssertEq(frame.Pixels[12], 10, "and the rest of the frame is untouched");
    }

    /// <summary>
    /// The lower layer clears the frame and clips a negative offset.
    /// </summary>
    /// <remarks>
    /// <strong>And a negative offset used to empty the whole frame</strong>: the
    /// region was handed to <c>TryCopyRegion</c> as it was, that call refuses a
    /// negative source, and the screen came out black -- a shake to the left would
    /// have blacked out the map.
    /// </remarks>
    public void Test_EinNegativerOffsetMachtDenFrameNichtSchwarz()
    {
        var layer = Layer((200, 0, 0, 255), (0, 200, 0, 255));
        var frame = Frame(4, 10, 20, 30);

        Rm2kEngineRuntime.CopyViewport(layer, frame, -1, 0);

        AssertEq(frame.Pixels[0], 0, "**and outside the map the frame is cleared**");
        AssertEq(frame.Pixels[4], 200, "**and the layer is drawn from column one on**");
        AssertEq(frame.Pixels[9], 200, "and its green lands in column two");
        AssertEq(frame.Pixels[12], 0, "and the rest stays cleared");
    }
}
