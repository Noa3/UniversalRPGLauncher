using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestLogicalGamePath : TestBase
{
    public void Test_NormalizesWindowsSeparators()
    {
        AssertTrue(LogicalGamePath.TryNormalize("Data\\Scripts.rvdata2", out var normalized));
        AssertEq(normalized, "Data/Scripts.rvdata2");
    }

    public void Test_RejectsWindowsDrivePathOnEveryPlatform()
    {
        AssertFalse(LogicalGamePath.TryNormalize("C:\\Games\\Game.rgss3a", out _));
        AssertFalse(LogicalGamePath.TryNormalize("d:/Games/Data/System.json", out _));
    }

    public void Test_RejectsUncTraversalAndAbsolutePaths()
    {
        AssertFalse(LogicalGamePath.TryNormalize("\\\\server\\share\\Game.ini", out _));
        AssertFalse(LogicalGamePath.TryNormalize("../outside.txt", out _));
        AssertFalse(LogicalGamePath.TryNormalize("Data/../outside.txt", out _));
        AssertFalse(LogicalGamePath.TryNormalize("/etc/passwd", out _));
    }

    public void Test_RejectsAlternateDataStreamAndControlCharacters()
    {
        AssertFalse(LogicalGamePath.TryNormalize("Data/file.txt:secret", out _));
        AssertFalse(LogicalGamePath.TryNormalize("Data/bad\nname.txt", out _));
    }

    public void Test_AcceptsUnicodeAndSpaces()
    {
        AssertTrue(LogicalGamePath.TryNormalize("Data/設定 Scripts/メイン.rvdata2", out var normalized));
        AssertEq(normalized, "Data/設定 Scripts/メイン.rvdata2");
    }
}
