using System.Collections.Generic;
using Godot;
using UniversalRPG.Core;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestVirtualFileSystem : TestBase
{
	private const string TempDir = "user://vfs_test";

	private VirtualFileSystem _vfs = null!;

	public override void Setup()
	{
		_vfs = new VirtualFileSystem();
		DirAccess.MakeDirRecursiveAbsolute(TempDir.PathJoin("game"));
		DirAccess.MakeDirRecursiveAbsolute(TempDir.PathJoin("override"));
		DirAccess.MakeDirRecursiveAbsolute(TempDir.PathJoin("rtp"));
		DirAccess.MakeDirRecursiveAbsolute(TempDir.PathJoin("save"));

		WriteText(TempDir.PathJoin("game/Map001.json"), "{\"map\":1}");
		WriteText(TempDir.PathJoin("game/Map002.json"), "{\"map\":2}");
		WriteText(TempDir.PathJoin("game/data/config.ini"), "[Game]\nTitle=Test");
		WriteText(TempDir.PathJoin("override/Map001.json"), "{\"map\":1,\"override\":true}");
		WriteText(TempDir.PathJoin("rtp/BGM001.ogg"), "rtp_audio_data");
		WriteText(TempDir.PathJoin("save/Save001.rvdata2"), "save_data");
	}

	public override void Teardown()
	{
		CleanupDir(TempDir);
	}

	// === TESTS: Path Normalization ===

	public void Test_NormalizePathForwardsSlashes()
	{
		AssertEq(VirtualFileSystem.NormalizePath("Graphics/Characters/Hero.png"), "Graphics/Characters/Hero.png");
	}

	public void Test_NormalizePathBackslashes()
	{
		AssertEq(VirtualFileSystem.NormalizePath("Graphics\\Characters\\Hero.png"), "Graphics/Characters/Hero.png");
	}

	public void Test_NormalizePathDoubleSlashes()
	{
		AssertEq(VirtualFileSystem.NormalizePath("Graphics//Characters///Hero.png"), "Graphics/Characters/Hero.png");
	}

	public void Test_NormalizePathTrailingSlash()
	{
		AssertEq(VirtualFileSystem.NormalizePath("Graphics/Characters/"), "Graphics/Characters");
	}

	public void Test_NormalizePathRoot()
	{
		AssertEq(VirtualFileSystem.NormalizePath("/"), "/");
	}

	// === TESTS: Path Safety ===

	public void Test_SafePathNormal()
	{
		AssertTrue(VirtualFileSystem.IsSafePath("Graphics/Characters/Hero.png"));
	}

	public void Test_SafePathWithDots()
	{
		AssertFalse(VirtualFileSystem.IsSafePath("../../../etc/passwd"));
	}

	public void Test_SafePathAbsolute()
	{
		AssertFalse(VirtualFileSystem.IsSafePath("/etc/passwd"));
	}

	public void Test_SafePathNullByte()
	{
		AssertTrue(VirtualFileSystem.ContainsNullByte(new byte[] { 0x70, 0x00, 0x6e }));
		AssertFalse(VirtualFileSystem.ContainsNullByte(new byte[] { 0x70, 0x6e }));
	}

	// === TESTS: Mount Management ===

	public void Test_AddMount()
	{
		var mount = _vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		AssertNe(mount, null);
		AssertEq(mount.Path, TempDir.PathJoin("game"));
		AssertEq(mount.MountTypeValue, VirtualFileSystem.MountType.Game);
	}

	public void Test_AddWritableMount()
	{
		var mount = _vfs.AddMount("save", TempDir.PathJoin("save"), VirtualFileSystem.MountType.Save, true);
		AssertTrue(mount.Writable);
	}

	public void Test_RemoveMount()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		AssertTrue(_vfs.RemoveMount(VirtualFileSystem.MountType.Game));
		AssertEq(_vfs.GetGameDirectory(), "");
	}

	public void Test_GetMount()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var mount = _vfs.GetMount(VirtualFileSystem.MountType.Game);
		AssertNe(mount, null);
		AssertEq(mount!.Path, TempDir.PathJoin("game"));
	}

	public void Test_GetNonexistentMount()
	{
		var mount = _vfs.GetMount(VirtualFileSystem.MountType.Game);
		AssertEq(mount, null);
	}

	// === TESTS: File Resolution ===

	public void Test_ResolveExistingFile()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var resolved = _vfs.Resolve("Map001.json");
		AssertNe(resolved, "");
		AssertTrue(FileAccess.FileExists(resolved));
	}

	public void Test_ResolveNonexistentFile()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var resolved = _vfs.Resolve("NonExistent.json");
		AssertEq(resolved, "");
	}

	public void Test_ResolveUnsafePath()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var resolved = _vfs.Resolve("../../../etc/passwd");
		AssertEq(resolved, "");
	}

	public void Test_ResolveCaseInsensitive()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var resolved = _vfs.Resolve("map001.json");
		AssertNe(resolved, "");
		AssertTrue(FileAccess.FileExists(resolved));
	}

	public void Test_ResolvePriorityOverride()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		_vfs.AddMount("override", TempDir.PathJoin("override"), VirtualFileSystem.MountType.Override);
		var resolved = _vfs.Resolve("Map001.json");
		AssertTrue(resolved.StartsWith(TempDir.PathJoin("override") + "/"),
			"Override should take priority, got " + resolved);
	}

	// === TESTS: File Operations ===

	public void Test_FileExists()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		AssertTrue(_vfs.FileExists("Map001.json"));
		AssertFalse(_vfs.FileExists("NonExistent.json"));
	}

	public void Test_DirExists()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		AssertTrue(_vfs.DirExists("data"));
		AssertFalse(_vfs.DirExists("NonExistent"));
	}

	public void Test_ListDir()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var files = _vfs.ListDir("");
		AssertTrue(files.Contains("Map001.json"));
		AssertTrue(files.Contains("Map002.json"));
		AssertTrue(files.Contains("data"));
	}

	public void Test_OpenFile()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		using var file = _vfs.Open("Map001.json", FileAccess.ModeFlags.Read);
		AssertNe(file, null);
		if (file == null)
		{
			return;
		}
		AssertTrue(file.IsOpen());
		AssertEq(file.GetAsText(), "{\"map\":1}");
	}

	public void Test_OpenNonexistentFile()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		var file = _vfs.Open("NonExistent.json", FileAccess.ModeFlags.Read);
		AssertEq(file, null);
	}

	// === TESTS: Game Directory ===

	public void Test_GetGameDirectory()
	{
		_vfs.AddMount("game", TempDir.PathJoin("game"), VirtualFileSystem.MountType.Game);
		AssertEq(_vfs.GetGameDirectory(), TempDir.PathJoin("game"));
	}

	public void Test_GetSaveDirectory()
	{
		_vfs.AddMount("save", TempDir.PathJoin("save"), VirtualFileSystem.MountType.Save);
		AssertEq(_vfs.GetSaveDirectory(), TempDir.PathJoin("save"));
	}

	public void Test_GetNonexistentSaveDirectory()
	{
		AssertEq(_vfs.GetSaveDirectory(), "");
	}

	private static void WriteText(string pPath, string pText)
	{
		DirAccess.MakeDirRecursiveAbsolute(pPath.GetBaseDir());
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Write);
		file?.StoreString(pText);
	}

	private static void CleanupDir(string pDir)
	{
		if (!DirAccess.DirExistsAbsolute(pDir))
		{
			return;
		}
		using var directory = DirAccess.Open(pDir);
		if (directory == null)
		{
			return;
		}
		foreach (var child in directory.GetDirectories())
		{
			CleanupDir(pDir.PathJoin(child));
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(pDir.PathJoin(fileName));
		}
		DirAccess.RemoveAbsolute(pDir);
	}
}
