using System;
using System.Text;
using Godot;

namespace UniversalRPG.Core;

public partial class LegacyTextDecoder : RefCounted
{
	// Godot's multibyte decoder accepts SHIFT_JIS on Windows. CP932 and SJIS are
	// common aliases used by game metadata and are normalized before this list is
	// attempted so they do not produce an avoidable engine error first.
	public static readonly string[] JapaneseEncodings = { "SHIFT_JIS" };
	private static bool _codePagesRegistered;

	public string Decode(byte[] pBytes, string pPreferredEncoding = "")
	{
		if (pBytes.Length == 0)
		{
			return "";
		}
		if (pBytes.Length >= 3 && pBytes[0] == 0xef && pBytes[1] == 0xbb && pBytes[2] == 0xbf)
		{
			return Encoding.UTF8.GetString(pBytes, 3, pBytes.Length - 3);
		}
		if (pBytes.Length >= 2 && pBytes[0] == 0xff && pBytes[1] == 0xfe)
		{
			return Encoding.Unicode.GetString(pBytes, 2, pBytes.Length - 2);
		}

		if (IsValidUtf8(pBytes))
		{
			return Encoding.UTF8.GetString(pBytes);
		}

		var encodings = new System.Collections.Generic.List<string>();
		if (!string.IsNullOrEmpty(pPreferredEncoding))
		{
			encodings.Add(NormalizeEncoding(pPreferredEncoding));
		}
		foreach (var encoding in JapaneseEncodings)
		{
			if (!encodings.Contains(encoding))
			{
				encodings.Add(encoding);
			}
		}
		foreach (var encodingName in encodings)
		{
			var decoded = DecodeMultibyte(pBytes, encodingName);
			if (!string.IsNullOrEmpty(decoded))
			{
				return decoded;
			}
		}
		return "";
	}

	private static string NormalizeEncoding(string pEncoding)
	{
		var normalized = pEncoding.Trim().ToUpperInvariant().Replace("-", "_");
		if (normalized is "CP932" or "SJIS" or "SHIFTJIS" or "SHIFT_JIS")
		{
			return "SHIFT_JIS";
		}
		return pEncoding;
	}

	private static string DecodeMultibyte(byte[] pBytes, string pEncodingName)
	{
		try
		{
			EnsureCodePagesRegistered();
			// CP932 and Shift_JIS both map to Windows code page 932.
			var encoding = pEncodingName.ToUpperInvariant() switch
			{
				"CP932" or "SHIFT_JIS" or "SJIS" => Encoding.GetEncoding(932),
				_ => Encoding.GetEncoding(pEncodingName),
			};
			var decoded = encoding.GetString(pBytes);
			if (decoded.Contains("\uFFFD"))
			{
				return "";
			}
			return decoded;
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static void EnsureCodePagesRegistered()
	{
		if (_codePagesRegistered)
		{
			return;
		}
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		_codePagesRegistered = true;
	}

	private static bool IsValidUtf8(byte[] pBytes)
	{
		var index = 0;
		while (index < pBytes.Length)
		{
			var first = pBytes[index];
			if (first <= 0x7f)
			{
				index += 1;
				continue;
			}
			var continuationCount = 0;
			if (first >= 0xc2 && first <= 0xdf)
			{
				continuationCount = 1;
			}
			else if (first >= 0xe0 && first <= 0xef)
			{
				continuationCount = 2;
			}
			else if (first >= 0xf0 && first <= 0xf4)
			{
				continuationCount = 3;
			}
			else
			{
				return false;
			}
			if (index + continuationCount >= pBytes.Length)
			{
				return false;
			}
			var second = pBytes[index + 1];
			if (second < 0x80 || second > 0xbf)
			{
				return false;
			}
			if (first == 0xe0 && second < 0xa0)
			{
				return false;
			}
			if (first == 0xed && second > 0x9f)
			{
				return false;
			}
			if (first == 0xf0 && second < 0x90)
			{
				return false;
			}
			if (first == 0xf4 && second > 0x8f)
			{
				return false;
			}
			for (var offset = 2; offset <= continuationCount; offset++)
			{
				var continuation = pBytes[index + offset];
				if (continuation < 0x80 || continuation > 0xbf)
				{
					return false;
				}
			}
			index += continuationCount + 1;
		}
		return true;
	}
}