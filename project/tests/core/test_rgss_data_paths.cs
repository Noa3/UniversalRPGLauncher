using System;
using System.Diagnostics;
using System.IO;
using Godot;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssDataPaths : TestBase
{
    private string _directory = "";
    private string Root => Path.Combine(_directory, "game");

    public override void Setup()
    {
        _directory = Path.Combine(System.Environment.GetEnvironmentVariable("TMPDIR")
            ?? ProjectSettings.GlobalizePath("user://"), "urpg-data-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "Data"));
        Directory.CreateDirectory(Path.Combine(_directory, "game-extra"));
        foreach (var extension in new[] { ".rxdata", ".rvdata", ".rvdata2" })
            File.WriteAllBytes(Path.Combine(Root, "Data", "inside" + extension), new byte[] { 4, 8, MarshalType.True });
        File.WriteAllBytes(Path.Combine(_directory, "game-extra", "outside.rxdata"), new byte[] { 4, 8, MarshalType.True });
    }

    public override void Teardown()
    {
        // Remove owned junctions themselves before recursively deleting the fixture.
        foreach (var link in new[] { Path.Combine(Root, "Linked"), Path.Combine(_directory, "root-link") })
            if (Directory.Exists(link)) Directory.Delete(link);
        Directory.Delete(_directory, true);
    }

    public void Test_SiblingPrefixTraversalIsRefusedBeforeReading()
    {
        var host = RgssDatenHost.Oeffne(Root, out var error);
        AssertTrue(host != null, error);
        if (host == null) return;
        AssertTrue(host.Lade("Data/inside.rxdata") != null, "The real Marshal fixture must be readable.");
        AssertEq(host.Gelesen, 1);
        var paths = new[] { @"..\game-extra\outside.rxdata", "../game-extra/outside.rxdata", @"Data\..\..\game-extra\outside.rxdata",
            @"Data\.. \.. \game-extra\outside.rxdata", @"Data\.. .\.. .\game-extra\outside.rxdata" };
        foreach (var path in paths)
        {
            AssertTrue(host.Lade(path) == null, "Path must not escape into a sibling sharing the root prefix: " + path);
            AssertEq(host.Gelesen, 1, "Refused paths must not read or parse outside bytes.");
        }
        AssertEq(host.Verweigert, paths.Length);
    }

    public void Test_MalformedPathsUseTheExistingRefusalContract()
    {
        var host = RgssDatenHost.Oeffne(Root, out var error);
        AssertTrue(host != null, error);
        if (host == null) return;
        var paths = new[] { "bad\0.rxdata", "Data/inside.rxdata:stream", new string('x', 4097),
            Path.Combine(_directory, "game-extra", "outside.rxdata"), "/Data/inside.rxdata" };
        foreach (var path in paths)
        {
            AssertTrue(host.Lade(path) == null, "Malformed paths must be refused without a CLR exception.");
            AssertEq(host.Gelesen, 0);
        }
        AssertEq(host.Verweigert, paths.Length);
    }

    public void Test_AllGenerationExtensionsRemainReadable()
    {
        var host = RgssDatenHost.Oeffne(Root, out var error);
        AssertTrue(host != null, error);
        if (host == null) return;
        foreach (var extension in new[] { ".rxdata", ".rvdata", ".rvdata2" })
            AssertTrue(host.Lade("Data/inside" + extension) != null);
        AssertEq(host.Gelesen, 3);
        AssertEq(host.Verweigert, 0);
    }

    public void Test_DirectoryLinksCannotRedirectDataReads()
    {
        CreateDirectoryLink(Path.Combine(Root, "Linked"), Path.Combine(_directory, "game-extra"));
        var host = RgssDatenHost.Oeffne(Root, out var error);
        AssertTrue(host != null, error);
        if (host == null) return;
        AssertTrue(host.Lade("Linked/outside.rxdata") == null, "A directory link must not redirect a data read outside the game.");
        AssertEq(host.Gelesen, 0);
        AssertEq(host.Verweigert, 1);

        var rootLink = Path.Combine(_directory, "root-link");
        CreateDirectoryLink(rootLink, Root);
        var linkedHost = RgssDatenHost.Oeffne(rootLink, out error);
        AssertTrue(linkedHost != null, error);
        if (linkedHost == null) return;
        AssertTrue(linkedHost.Lade("Data/inside.rxdata") == null, "The configured root must not bypass link checks.");
        AssertEq(linkedHost.Gelesen, 0);
        AssertEq(linkedHost.Verweigert, 1);
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }
        // Junctions exercise reparse handling without Windows symlink privileges.
        // These are generated fixture paths, never paths supplied by a game.
        if ((link + target).IndexOfAny(new[] { '"', '%', '&', '|', '<', '>', '^', '\r', '\n' }) >= 0)
            throw new InvalidOperationException("Unsafe fixture path for junction creation.");
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "/d", "/c", "mklink", "/J", link, target }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Could not create the junction fixture process.");
        if (!process.WaitForExit(10000))
        {
            process.Kill(true);
            process.WaitForExit();
            throw new IOException("Junction fixture creation timed out.");
        }
        if (process.ExitCode != 0) throw new IOException("Junction fixture failed: " + process.StandardError.ReadToEnd());
        if ((File.GetAttributes(link) & FileAttributes.ReparsePoint) == 0)
            throw new IOException("The junction fixture is not a reparse point.");
    }
}
