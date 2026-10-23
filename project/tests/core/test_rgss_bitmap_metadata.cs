using System;
using System.IO;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssBitmapMetadata : TestBase
{
    public void Test_MetadataHostRefusesRootedAndTraversalNames()
    {
        using var fixture = new RgssImageFixture();
        var outside = fixture.Root + "-outside";
        Directory.CreateDirectory(outside);
        try
        {
            File.Copy(Path.Combine(fixture.Root, "Graphics", "Pictures", "shared.png"), Path.Combine(outside, "external.png"));
            var graphics = new RgssGrafikHost { Wurzel = fixture.Root };
            AssertTrue(graphics.KnownMethods.Contains("BildGroesse"), "Keep the metadata API available.");
            var valid = QuerySize(graphics, "shared");
            AssertTrue(valid != null);
            if (valid != null)
            {
                AssertEq(valid.Items[0].Integer, 2);
                AssertEq(valid.Items[1].Integer, 2);
            }
            var traversal = "../../../" + Path.GetFileName(outside) + "/external";
            foreach (var name in new[] { traversal, traversal.Replace('/', '\\'), Path.Combine(outside, "external"), "bad\0", "shared.png:stream" })
            {
                AssertTrue(QuerySize(graphics, name) == null, "Unsafe metadata name must not expose image dimensions.");
                AssertTrue(graphics.FehlendeBilder.Any(item => item.Contains(name)), "Refusal must identify the requested name.");
            }
            AssertEq(graphics.Bitmaps.Count, 0, "Metadata queries must not allocate pixel surfaces.");
        }
        finally { Directory.Delete(outside, true); }
    }

    public void Test_MetadataHostRefusesDirectoryAndRootLinks()
    {
        using var fixture = new RgssImageFixture();
        var outside = fixture.Root + "-outside";
        var rootLink = fixture.Root + "-link";
        var link = Path.Combine(fixture.Root, "Graphics", "Pictures", "redirect");
        Directory.CreateDirectory(outside);
        try
        {
            File.Copy(Path.Combine(fixture.Root, "Graphics", "Pictures", "shared.png"), Path.Combine(outside, "external.png"));
            TestRgssDataPaths.CreateDirectoryLink(link, outside);
            var graphics = new RgssGrafikHost { Wurzel = fixture.Root };
            AssertTrue(QuerySize(graphics, "redirect/external") == null, "A junction must not redirect the metadata reader.");
            AssertTrue(graphics.FehlendeBilder.Any(item => item.Contains("redirect/external")));
            TestRgssDataPaths.CreateDirectoryLink(rootLink, fixture.Root);
            var rooted = new RgssGrafikHost { Wurzel = rootLink };
            AssertTrue(QuerySize(rooted, "shared") == null, "A linked configured root must not bypass the guard.");
            AssertTrue(rooted.FehlendeBilder.Any(item => item.Contains("shared")));
            AssertEq(graphics.Bitmaps.Count, 0);
            AssertEq(rooted.Bitmaps.Count, 0);
        }
        finally
        {
            if (Directory.Exists(link)) Directory.Delete(link);
            if (Directory.Exists(rootLink)) Directory.Delete(rootLink);
            Directory.Delete(outside, true);
        }
    }

    private static RubyValue? QuerySize(RgssGrafikHost graphics, string name) => graphics.CallMethod(
        RubyValue.Nil, "BildGroesse", new[] { RubyValue.OfSymbol("pictures"),
            RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(name)) });

    public void Test_MetadataRejectsInvalidHeadersAndUnsupportedDimensionsByName()
    {
        var root = Path.Combine(Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath(),
            "urpg-bitmap-metadata-" + Guid.NewGuid().ToString("N"));
        var pictures = Path.Combine(root, "Graphics", "Pictures");
        Directory.CreateDirectory(pictures);
        try
        {
            var graphics = new RgssGrafikHost { Wurzel = root };
            // IHDR-only metadata fixtures: these do not claim to be decoded images.
            byte[] Header(int width, int height)
            {
                var bytes = new byte[33];
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 }.CopyTo(bytes, 0);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
                bytes[24] = 8;
                bytes[25] = 6;
                return bytes;
            }
            File.WriteAllBytes(Path.Combine(pictures, "valid.png"), Header(2, 3));
            var size = graphics.BildGroesse("pictures", "valid");
            AssertTrue(size != null);
            AssertEq(size!.Items[0].Integer, 2);
            AssertEq(size.Items[1].Integer, 3);
            foreach (var name in new[] { "signature", "chunk", "zero", "negative", "oversized", "truncated" })
            {
                var bytes = Header(2, 3);
                if (name == "signature") bytes[0] = 0;
                if (name == "chunk") bytes[12] = 0;
                if (name == "zero") bytes = Header(0, 3);
                if (name == "negative") bytes = Header(-1, 3);
                if (name == "oversized") bytes = Header(RgssBitmapStore.MaximumDimension + 1, 3);
                if (name == "truncated") bytes = bytes[..24];
                File.WriteAllBytes(Path.Combine(pictures, name + ".png"), bytes);
                AssertTrue(graphics.BildGroesse("pictures", name) == null, name + " must not become bitmap dimensions");
                AssertTrue(graphics.FehlendeBilder.Any(diagnostic => diagnostic.Contains(name + ".png")),
                    name + " must produce a named diagnostic");
            }
            AssertEq(graphics.Bitmaps.Count, 0, "Metadata inspection must not allocate pixel surfaces.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
