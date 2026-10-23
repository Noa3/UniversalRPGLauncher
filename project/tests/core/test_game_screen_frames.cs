using System;
using System.Reflection;
using Godot;
using UniversalRPG.App.Ui;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestGameScreenFrames : TestBase
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public void Test_NativeTextureAcceptsSmallFramesAndShrinkingFrames()
    {
        var screen = new Rm2kGameScreen();
        try
        {
            foreach (var size in new[] { (4, 3), (400, 300), (4, 3) })
            {
                var pixels = new Rm2kPixelBuffer(size.Item1, size.Item2);
                pixels.Pixels[0] = 255;
                pixels.Pixels[3] = 255;
                screen.SetGameState(pixels, 1);
                using var texture = Texture(screen);
                AssertTrue(texture != null, "Native texture creation must succeed for the frame's exact dimensions.");
                if (texture == null) continue;
                AssertEq(texture.GetWidth(), size.Item1);
                AssertEq(texture.GetHeight(), size.Item2);
                using var image = texture.GetImage();
                AssertTrue(image != null && !image.IsEmpty(), "The native texture must retain readable image data.");
                if (image != null && !image.IsEmpty())
                    AssertEq(image.GetPixel(0, 0), new Color(1, 0, 0, 1));
                screen.Reset();
            }
        }
        finally
        {
            screen.Free();
        }
    }

    public void Test_ResetClearsFrameIdentityAndPreviousEngineOverlays()
    {
        var screen = new Rm2kGameScreen();
        try
        {
            screen.SetGameState(new Rm2kPixelBuffer(4, 3), 17, "old warning");
            screen.SetCommandWindow(1, 2, 90, 60, 20, new[] { "Old", "Menu" }, 1);
            screen.SetPresentation(true, "old message", new[] { "old choice" }, 0, true, 12);
            screen.Reset();
            AssertEq(screen.CommandWindow().Rows.Count, 0);
            AssertEq(screen.CommandWindow().Index, 0);
            AssertFalse(Field<bool>(screen, "_commandVisible"));
            AssertEq(Field<int>(screen, "_frameCount"), 0);
            AssertEq(screen.RenderDiagnostic, "");
            AssertEq(Field<string>(screen, "_messageText"), "");
            AssertEq(Field<int>(screen, "_choiceSelected"), -1);
            AssertFalse(Field<bool>(screen, "_inputPending"));
            AssertTrue(Texture(screen) == null);
        }
        finally { screen.Free(); }
    }

    private static T Field<T>(Rm2kGameScreen screen, string name) =>
        (T)typeof(Rm2kGameScreen).GetField(name, Private)!.GetValue(screen)!;

    private static ImageTexture? Texture(Rm2kGameScreen screen) =>
        (ImageTexture?)typeof(Rm2kGameScreen).GetMethod("GetMapTexture", Private)!.Invoke(screen, null);
}
