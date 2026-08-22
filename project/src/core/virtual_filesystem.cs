using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Core;

/// <summary>
/// A non-destructive virtual filesystem layer for UniversalRPG.
/// Merges multiple mount points into a unified read-only or read-write view.
/// Handles case-insensitive path resolution for Windows compatibility.
/// </summary>
public partial class VirtualFileSystem : RefCounted
{
	public enum MountType
	{
		Game,
		Override,
		Rtp,
		Save,
		Cache,
	}

	public class Mount
	{
		public string Path;
		public MountType MountTypeValue;
		public bool Writable;

		public Mount(string pPath, MountType pType, bool pWritable = false)
		{
			Path = pPath;
			MountTypeValue = pType;
			Writable = pWritable;
		}
	}

	/// <summary>File entry in the virtual filesystem.</summary>
	public class VFile : IDisposable
	{
		private readonly FileAccess _file;

		public VFile(string pPath, FileAccess.ModeFlags pMode)
		{
			_file = FileAccess.Open(pPath, pMode);
		}

		public bool IsOpen() => _file != null && _file.IsOpen();

		public string GetAsText()
		{
			if (!IsOpen())
			{
				return "";
			}
			_file.Seek(0);
			return _file.GetAsText();
		}

		public ulong GetPosition() => IsOpen() ? _file.GetPosition() : 0;

		public void Seek(ulong pPosition)
		{
			if (IsOpen())
			{
				_file.Seek(pPosition);
			}
		}

		public bool EofReached() => !IsOpen() || _file.EofReached();

		public byte GetByte() => IsOpen() ? _file.Get8() : (byte)0;

		public byte[] GetBuffer(long pSize) => IsOpen() ? _file.GetBuffer(pSize) : Array.Empty<byte>();

		public string GetLine() => IsOpen() ? _file.GetLine() : "";

		public ulong GetLength() => IsOpen() ? _file.GetLength() : 0;

		public void Close()
		{
			if (IsOpen())
			{
				_file.Close();
			}
		}

		public void Dispose()
		{
			Close();
			_file?.Dispose();
		}
	}

	public const string PathSeparator = "/";

	private readonly List<Mount> _mounts = new();

	// Case-insensitive lookup cache
	private readonly Dictionary<string, string> _caseMap = new();

	/// <summary>Add a mount point to the filesystem.</summary>
	public Mount AddMount(string pName, string pPath, MountType pMountType, bool pWritable = false)
	{
		var mount = new Mount(pPath, pMountType, pWritable);
		_mounts.Add(mount);
		RebuildCaseMap();
		return mount;
	}

	/// <summary>Remove a mount point.</summary>
	public bool RemoveMount(MountType pMountType)
	{
		for (var index = _mounts.Count - 1; index >= 0; index--)
		{
			if (_mounts[index].MountTypeValue == pMountType)
			{
				_mounts.RemoveAt(index);
				RebuildCaseMap();
				return true;
			}
		}
		return false;
	}

	/// <summary>Get the mount of a specific type.</summary>
	public Mount? GetMount(MountType pMountType)
	{
		foreach (var mount in _mounts)
		{
			if (mount.MountTypeValue == pMountType)
			{
				return mount;
			}
		}
		return null;
	}

	/// <summary>Normalize a path to use forward slashes and remove redundant separators.</summary>
	public static string NormalizePath(string pPath)
	{
		var normalized = pPath.Replace("\\", "/");
		while (normalized.Contains("//"))
		{
			normalized = normalized.Replace("//", "/");
		}
		if (normalized.Length > 1 && normalized.EndsWith("/"))
		{
			normalized = normalized.TrimEnd('/');
		}
		return normalized;
	}

	/// <summary>Check if a path is safe (no traversal outside mount points).</summary>
	public static bool IsSafePath(string pPath)
	{
		var normalized = NormalizePath(pPath);

		// Block path traversal
		if (normalized.Contains("../") || normalized.StartsWith("../"))
		{
			return false;
		}

		// Block absolute paths (must be relative to mount)
		if (normalized.StartsWith("/"))
		{
			return false;
		}

		// Block embedded NUL bytes
		if (normalized.Contains('\u0000'))
		{
			return false;
		}

		return true;
	}

	/// <summary>Detect embedded NUL bytes in raw path data.</summary>
	public static bool ContainsNullByte(byte[] pBytes)
	{
		foreach (var value in pBytes)
		{
			if (value == 0)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Resolve a path case-insensitively across all mounts.
	/// Returns the resolved path or empty string if not found.
	/// </summary>
	public string Resolve(string pPath)
	{
		if (!IsSafePath(pPath))
		{
			GD.PrintErr("[VirtualFileSystem] Unsafe path rejected: ", pPath);
			return "";
		}

		var normalized = NormalizePath(pPath).ToLowerInvariant();

		if (string.IsNullOrEmpty(normalized))
		{
			var gameMount = GetMount(MountType.Game);
			return gameMount?.Path ?? "";
		}

		// Check case map first (cached lookup)
		if (_caseMap.TryGetValue(normalized, out var cached))
		{
			return cached;
		}

		// Search through mounts in priority order
		foreach (var mount in MountsInPriorityOrder())
		{
			if (!DirAccess.DirExistsAbsolute(mount.Path))
			{
				continue;
			}
			var candidates = FindCaseInsensitive(mount.Path, normalized);
			if (candidates.Count > 0)
			{
				_caseMap[normalized] = candidates[0];
				return candidates[0];
			}
		}

		return "";
	}

	/// <summary>Find files case-insensitively in a directory.</summary>
	private static List<string> FindCaseInsensitive(string pDir, string pNormalized)
	{
		var results = new List<string>();

		if (!DirAccess.DirExistsAbsolute(pDir))
		{
			return results;
		}

		using var dir = DirAccess.Open(pDir);
		if (dir == null)
		{
			return results;
		}

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (!string.IsNullOrEmpty(fileName))
		{
			var lowerName = fileName.ToLowerInvariant();
			if (lowerName == pNormalized || lowerName.StartsWith(pNormalized + "/"))
			{
				results.Add(pDir + "/" + fileName);
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();

		return results;
	}

	/// <summary>Rebuild the case-insensitive lookup cache.</summary>
	public void RebuildCaseMap()
	{
		_caseMap.Clear();

		foreach (var mount in MountsInPriorityOrder())
		{
			if (!DirAccess.DirExistsAbsolute(mount.Path))
			{
				continue;
			}
			ScanDirectory(mount.Path, mount.Path, "");
		}
	}

	/// <summary>Return mounts ordered by resolution priority (highest first).</summary>
	private List<Mount> MountsInPriorityOrder()
	{
		var ordered = new List<Mount>(_mounts);
		ordered.Sort((pLeft, pRight) => MountPriority(pLeft.MountTypeValue).CompareTo(MountPriority(pRight.MountTypeValue)));
		return ordered;
	}

	/// <summary>Resolution priority of a mount type (lower wins first).</summary>
	private static int MountPriority(MountType pType)
	{
		return pType switch
		{
			MountType.Override => 0,
			MountType.Rtp => 1,
			MountType.Game => 2,
			MountType.Save => 3,
			_ => 4,
		};
	}

	/// <summary>Recursively scan a directory and build case map.</summary>
	private void ScanDirectory(string pBase, string pCurrent, string pRelative)
	{
		using var dir = DirAccess.Open(pCurrent);
		if (dir == null)
		{
			return;
		}

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (!string.IsNullOrEmpty(fileName))
		{
			var fullPath = pCurrent + "/" + fileName;
			var relative = pRelative + fileName;

			if (DirAccess.DirExistsAbsolute(fullPath))
			{
				ScanDirectory(pBase, fullPath, relative + "/");
			}
			else if (FileAccess.FileExists(fullPath))
			{
				var lower = relative.ToLowerInvariant();
				if (!_caseMap.ContainsKey(lower))
				{
					_caseMap[lower] = fullPath;
				}
			}

			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
	}

	/// <summary>Open a file through the virtual filesystem.</summary>
	public VFile? Open(string pPath, FileAccess.ModeFlags pMode)
	{
		var resolved = Resolve(pPath);
		if (resolved == "")
		{
			GD.PrintErr("[VirtualFileSystem] Path not found: ", pPath);
			return null;
		}

		return new VFile(resolved, pMode);
	}

	/// <summary>Check if a file exists.</summary>
	public bool FileExists(string pPath)
	{
		var resolved = Resolve(pPath);
		return resolved != "" && FileAccess.FileExists(resolved);
	}

	/// <summary>Check if a directory exists.</summary>
	public bool DirExists(string pPath)
	{
		var resolved = Resolve(pPath);
		return resolved != "" && DirAccess.DirExistsAbsolute(resolved);
	}

	/// <summary>List files in a directory.</summary>
	public List<string> ListDir(string pPath)
	{
		var results = new List<string>();
		var resolved = Resolve(pPath);
		if (resolved == "")
		{
			return results;
		}

		using var dir = DirAccess.Open(resolved);
		if (dir == null)
		{
			return results;
		}

		dir.ListDirBegin();
		var fileName = dir.GetNext();
		while (!string.IsNullOrEmpty(fileName))
		{
			results.Add(fileName);
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();

		return results;
	}

	/// <summary>Get all mount points.</summary>
	public List<Mount> GetMounts() => new(_mounts);

	/// <summary>Get the base game directory.</summary>
	public string GetGameDirectory() => GetMount(MountType.Game)?.Path ?? "";

	/// <summary>Get the save directory.</summary>
	public string GetSaveDirectory() => GetMount(MountType.Save)?.Path ?? "";
}
