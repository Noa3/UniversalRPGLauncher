using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;

namespace UniversalRPG.Rgss;

/// <summary>
/// The archive format shared by RPG Maker XP, VX and VX Ace.
/// </summary>
/// <remarks>
/// <para>
/// A file is a header, then a list of entries. Each entry is a name, a size and
/// a body, and the list ends when a name can no longer be read. Every one of
/// those values is obfuscated, not encrypted: each is exclusive ored with the
/// output of a linear congruential generator that starts at a fixed constant
/// for the whole file and advances once per value.
/// </para>
/// <para>
/// The generator is <c>magic = magic * 7 + 3</c>. Advancing it by more than
/// one step at a time is a table driven shortcut in the reference reader, and it
/// is not needed here: the single step form is the definition and the table is
/// an optimisation of it.
/// </para>
/// <para>
/// The header's last byte is the version, which is what tells an XP archive
/// from a VX Ace one. They differ in nothing else that a reader has to know
/// before it can list the entries.
/// </para>
/// <para>
/// This reader lists and reads entries. It does not execute anything an entry
/// contains: a game script is read as bytes and nothing more.
/// </para>
/// </remarks>
public sealed class RgssArchiveReader
{
    /// <summary>The header every RGSS archive starts with.</summary>
    public const string HeaderText = "RGSSAD";

    /// <summary>The generator's starting value, and the value of its first output.</summary>
    public const uint InitialMagic = 0xDEADCAFE;

    /// <summary>The generator's multiplier.</summary>
    public const uint Multiplier = 7;

    /// <summary>The generator's increment.</summary>
    public const uint Increment = 3;

    /// <summary>The version byte an RPG Maker XP or VX archive carries.</summary>
    public const byte VersionXpAndVx = 1;

    /// <summary>The version byte an RPG Maker VX Ace archive carries.</summary>
    public const byte VersionVxAce = 3;

    private const int HeaderLength = 8;
    private const int MaxNameBytes = 512;
    private const int MaxEntries = 100_000;

    /// <summary>True when the payload starts with the RGSS header.</summary>
    /// <remarks>
    /// The reference reader compares the first six bytes against the name and
    /// then reads the version from the eighth. The seventh byte is not part of
    /// the check, so this predicate does not look at it either: a reader that
    /// required it to be zero would refuse a file the format allows.
    /// </remarks>
    public static bool HasArchiveHeader(byte[] pBytes)
    {
        if (pBytes == null || pBytes.Length < HeaderLength)
        {
            return false;
        }
        for (var index = 0; index < HeaderText.Length; index++)
        {
            if (pBytes[index] != (byte)HeaderText[index])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The version byte, or null when the payload is not an archive.</summary>
    public static byte? ReadVersion(byte[] pBytes)
    {
        return HasArchiveHeader(pBytes) ? pBytes[HeaderLength - 1] : null;
    }

    /// <summary>
    /// Advances the generator and returns the value the caller should use.
    /// </summary>
    /// <remarks>
    /// The value that obfuscates a field is the generator's state <em>before</em>
    /// it advances, so a reader that returns the state after advancing is off
    /// by one on every field from the first.
    /// </remarks>
    public static uint AdvanceMagic(ref uint pMagic)
    {
        var current = pMagic;
        pMagic = unchecked(pMagic * Multiplier + Increment);
        return current;
    }

    /// <summary>
    /// The key for one obfuscated value: the generator's current state, after
    /// which the generator advances exactly once.
    /// </summary>
    /// <remarks>
    /// The generator takes one step per obfuscated value, not two. A caller that
    /// advances to get a key and then advances again has consumed two steps for
    /// one field, and every value after it decodes to noise while still looking
    /// like a plausible length. This is the same operation as
    /// <see cref="AdvanceMagic"/> and is named separately because the intent
    /// matters more here than the arithmetic does.
    /// </remarks>
    public static uint NextKey(ref uint pMagic)
    {
        var current = pMagic;
        pMagic = unchecked(pMagic * Multiplier + Increment);
        return current;
    }

    /// <summary>Creates a reader. The reader holds no state.</summary>
    public RgssArchiveReader()
    {
    }

    /// <summary>Lists the entries in an archive, without reading their bodies.</summary>
    public PluginResult<IReadOnlyList<RgssArchiveEntry>> ListEntries(
        byte[] pBytes, string pSourceName)
    {
        if (pBytes == null)
        {
            throw new ArgumentNullException(nameof(pBytes));
        }
        if (!HasArchiveHeader(pBytes))
        {
            return Fail<IReadOnlyList<RgssArchiveEntry>>($"The file {pSourceName} does not start with the RGSS archive header.");
        }
        var entries = new List<RgssArchiveEntry>();
        var magic = InitialMagic;
        var offset = HeaderLength;
        var magicSteps = 0L;

        while (offset + 4 <= pBytes.Length)
        {
            if (entries.Count >= MaxEntries)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} holds more than {MaxEntries} entries.");
            }

            var encodedLength = ReadUInt32(pBytes, ref offset);
            if (offset > pBytes.Length)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} ends inside an entry's name length.");
            }
            var nameLength = DecodeLength(encodedLength, NextKey(ref magic));
            if (nameLength < 0 || nameLength > MaxNameBytes)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} declares a name of {nameLength} bytes, "
                    + (nameLength < 0
                        ? "which cannot be a length at all, and the stream is not "
                            + "an archive this reader can follow."
                        : $"over the {MaxNameBytes} limit."));
            }
            if (offset + nameLength + 4 > pBytes.Length)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} ends inside an entry's name.");
            }

            var name = new char[nameLength];
            for (var index = 0; index < nameLength; index++)
            {
                var value = (char)(pBytes[offset + index] ^ (byte)NextKey(ref magic));
                magicSteps++;
                // The reference reader folds the path separator here so that an
                // archive written on either system lists the same way.
                name[index] = value == '\\' ? '/' : value;
            }
            offset += nameLength;

            var encodedSize = ReadUInt32(pBytes, ref offset);
            var size = DecodeLength(encodedSize, NextKey(ref magic));
            if (size < 0 || (long)offset + size > pBytes.Length)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} declares an entry of {size} bytes at "
                    + $"offset {offset}, which runs past the end of the file.");
            }

            entries.Add(new RgssArchiveEntry
            {
                Name = new string(name),
                Offset = offset,
                Size = size,
                MagicAtEntry = magic,
            });

            offset += (int)size;
        }

        return PluginResult<IReadOnlyList<RgssArchiveEntry>>.Succeeded(entries);
    }

    /// <summary>Reads one entry's body.</summary>
    public PluginResult<byte[]> ReadEntry(
        byte[] pBytes, RgssArchiveEntry pEntry, string pSourceName)
    {
        if (pBytes == null)
        {
            throw new ArgumentNullException(nameof(pBytes));
        }
        if (pEntry == null)
        {
            throw new ArgumentNullException(nameof(pEntry));
        }
        if (pEntry.Offset < 0 || pEntry.Offset + (long)pEntry.Size > pBytes.Length)
        {
            return Fail<byte[]>(
                $"The entry {pEntry.Name} of {pSourceName} lies outside the file.");
        }
        var body = new byte[pEntry.Size];
        Array.Copy(pBytes, pEntry.Offset, body, 0, pEntry.Size);
        return PluginResult<byte[]>.Succeeded(body);
    }

    /// <summary>
    /// Writes an archive, for tests and for tools that repack a game.
    /// </summary>
    /// <remarks>
    /// This is the inverse of the reader and is the only way this repository
    /// can produce an archive to test against, since it has no real RPG Maker
    /// game to take one from. The obfuscation is applied in the same order the
    /// reader undoes it, so a round trip is exact.
    /// </remarks>
    public static byte[] Write(
        byte pVersion, IReadOnlyList<(string Name, byte[] Body)> pEntries)
    {
        ArgumentNullException.ThrowIfNull(pEntries);
        var bytes = new List<byte>();
        for (var index = 0; index < HeaderText.Length; index++)
        {
            bytes.Add((byte)HeaderText[index]);
        }
        bytes.Add(0);
        bytes.Add(pVersion);

        var magic = InitialMagic;
        foreach (var (name, body) in pEntries)
        {
            var raw = Encoding.ASCII.GetBytes(name);
            if (raw.Length > MaxNameBytes)
            {
                throw new ArgumentException(
                    $"The name {name} is longer than the {MaxNameBytes} byte limit.",
                    nameof(pEntries));
            }
            AddUInt32(bytes, (uint)raw.Length ^ NextKey(ref magic));
            for (var index = 0; index < raw.Length; index++)
            {
                bytes.Add((byte)(raw[index] ^ (byte)NextKey(ref magic)));
            }
            AddUInt32(bytes, (uint)body.Length ^ NextKey(ref magic));
            bytes.AddRange(body);
        }
        return bytes.ToArray();
    }

    /// <summary>
        /// A decoded length, and -1 when the value cannot be one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And this exists because of a real crash on a real archive.</strong>
        /// The stream carries an obfuscated <see cref="uint"/>, and casting the
        /// xor result to <see cref="int"/> turns any value above
        /// <c>int.MaxValue</c> into a negative length. <strong>The guard that
        /// followed only tested the upper bound, so a negative length reached
        /// <c>new char[nameLength]</c> and threw
        /// <see cref="OverflowException"/> out of the reader</strong> -- <strong>and
        /// that is what happened on <c>Dreaming Mary/Game.rgss3a</c>, the first
        /// finished VX Ace archive this repository was pointed at.</strong>
        /// </para>
        /// <para>
        /// <strong>And a negative length is not an obfuscation mistake to be
        /// retried, it is the end of the archive.</strong> The engine's own
        /// reader stops there; so does this one, and it says why.
        /// </para>
        /// </remarks>
        private static int DecodeLength(uint pEncoded, uint pKey)
        {
            var wert = pEncoded ^ pKey;
            return wert > int.MaxValue ? -1 : (int)wert;
        }

        private static uint ReadUInt32(byte[] pBytes, ref int pOffset)
    {
        var value = (uint)(pBytes[pOffset]
            | (pBytes[pOffset + 1] << 8)
            | (pBytes[pOffset + 2] << 16)
            | (pBytes[pOffset + 3] << 24));
        pOffset += 4;
        return value;
    }

    private static void AddUInt32(List<byte> pBytes, uint pValue)
    {
        pBytes.Add((byte)(pValue & 0xFF));
        pBytes.Add((byte)((pValue >> 8) & 0xFF));
        pBytes.Add((byte)((pValue >> 16) & 0xFF));
        pBytes.Add((byte)((pValue >> 24) & 0xFF));
    }

    private static PluginResult<T> Fail<T>(string pMessage)
    {
        return PluginResult<T>.Failed(PluginError.Create(
            PluginErrorCode.InvalidGame,
            pMessage,
            EnginePluginIds.RpgMakerXp,
            "rgss-archive"));
    }
}

/// <summary>One entry in an RGSS archive.</summary>
public sealed class RgssArchiveEntry
{
    /// <summary>The entry's path, with the path separator normalised to a slash.</summary>
    public required string Name { get; init; }

    /// <summary>Where the body starts in the archive.</summary>
    public required int Offset { get; init; }

    /// <summary>How many bytes the body is.</summary>
    public required int Size { get; init; }

    /// <summary>The generator's state at the entry's body, which its own reads use.</summary>
    public required uint MagicAtEntry { get; init; }
}
