using System;
using Godot;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Streaming reader for the LCF binary container format used by RPG Maker
/// 2000/2003: BER-encoded length + header, followed by BER-encoded
/// ID/length/payload chunks. Structures are terminated by a chunk with ID 0.
/// </summary>
public partial class LcfBinaryReader : RefCounted
{
	public const int MaxBerBytes = 5;
	public const int MaxInteger = 0x7fffffff;
	public const uint MaxUnsignedInteger = 0xffffffff;
	public const int MaxChunkBytes = 32 * 1024 * 1024;
	public const int MaxChunks = 100_000;
	public const int MaxArrayItems = 100_000;
	public const int MaxStructFields = 10_000;

	public string ErrorMessage { get; private set; } = "";
	public int ErrorOffset { get; private set; } = -1;

	private byte[] _data = [];
	private int _position = 0;

	public LcfBinaryReader(byte[] pData)
	{
		Reset(pData);
	}

	public void Reset(byte[] pData)
	{
		_data = pData;
		_position = 0;
		ErrorMessage = "";
		ErrorOffset = -1;
	}

	public bool HasError() => !string.IsNullOrEmpty(ErrorMessage);

	public int GetPosition() => _position;

	public int GetSize() => _data.Length;

	public int GetRemaining() => _data.Length - _position;

	public bool IsEof() => _position >= _data.Length;

	public int ReadBer()
	{
		var start = _position;
		var value = 0;
		for (var index = 0; index < MaxBerBytes; index++)
		{
			if (IsEof())
			{
				Fail("Unexpected end of data while reading BER integer", start);
				return -1;
			}
			var currentByte = _data[_position];
			_position += 1;
			if (value > (MaxInteger >> 7))
			{
				Fail("BER integer overflow", start);
				return -1;
			}
			value = (value << 7) | (currentByte & 0x7f);
			if ((currentByte & 0x80) == 0)
			{
				return value;
			}
			if (index == MaxBerBytes - 1)
			{
				Fail($"BER integer exceeds {MaxBerBytes} bytes", start);
			}
		}
		return -1;
	}

	public int ReadSignedBer()
	{
		uint value = 0;
		var start = _position;
		for (var index = 0; index < MaxBerBytes; index++)
		{
			if (IsEof())
			{
				Fail("Unexpected end of data while reading signed BER integer", start);
				return 0;
			}
			var currentByte = _data[_position];
			_position += 1;
			if (value > (MaxUnsignedInteger >> 7))
			{
				Fail("Signed BER integer overflow", start);
				return 0;
			}
			value = (value << 7) | (uint)(currentByte & 0x7f);
			if ((currentByte & 0x80) == 0)
			{
				// Two's complement over 32 bits, mirroring liblcf's LMT reader.
				return unchecked((int)value);
			}
			if (index == MaxBerBytes - 1)
			{
				Fail($"Signed BER integer exceeds {MaxBerBytes} bytes", start);
				return 0;
			}
		}
		return 0;
	}

	public byte[] ReadBytes(int pLength)
	{
		if (pLength < 0)
		{
			Fail("Negative read length", _position);
			return [];
		}
		if (pLength > GetRemaining())
		{
			Fail($"Read of {pLength} bytes exceeds remaining {GetRemaining()} bytes", _position);
			return [];
		}
		var result = new byte[pLength];
		Array.Copy(_data, _position, result, 0, pLength);
		_position += pLength;
		return result;
	}

	public string ReadHeader(string pExpected)
	{
		var length = ReadBer();
		if (HasError())
		{
			return "";
		}
		if (length <= 0 || length > 64)
		{
			Fail($"Invalid LCF header length {length}", _position);
			return "";
		}
		var headerBytes = ReadBytes(length);
		if (HasError())
		{
			return "";
		}
		var header = System.Text.Encoding.ASCII.GetString(headerBytes);
		if (header != pExpected)
		{
			Fail($"Expected LCF header {pExpected}, got {header}", 0);
			return "";
		}
		return header;
	}

	public Godot.Collections.Dictionary ReadChunk()
	{
		var chunkOffset = _position;
		var id = ReadBer();
		if (HasError())
		{
			return [];
		}
		if (id == 0)
		{
			return new Godot.Collections.Dictionary
			{
				{ "id", 0 },
				{ "length", 0 },
				{ "offset", chunkOffset },
				{ "payload_offset", _position },
				{ "data", new byte[0] },
				{ "terminator", true },
			};
		}
		var length = ReadBer();
		if (HasError())
		{
			return [];
		}
		if (length > MaxChunkBytes)
		{
			Fail($"Chunk {id} exceeds {MaxChunkBytes}-byte limit", chunkOffset);
			return [];
		}
		var payloadOffset = _position;
		var payload = ReadBytes(length);
		if (HasError())
		{
			return [];
		}
		return new Godot.Collections.Dictionary
		{
			{ "id", id },
			{ "length", length },
			{ "offset", chunkOffset },
			{ "payload_offset", payloadOffset },
			{ "data", payload },
			{ "terminator", false },
		};
	}

	private void Fail(string pMessage, int pOffset)
	{
		if (HasError())
		{
			return;
		}
		ErrorMessage = pMessage;
		ErrorOffset = pOffset;
	}
}