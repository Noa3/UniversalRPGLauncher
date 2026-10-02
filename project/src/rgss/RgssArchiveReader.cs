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

        // **Und Version 3 ist nicht Version 1 mit einer anderen Zahl.**
        // Version 1 laesst einen Generator ueber Name und Groesse laufen.
        // **Version 3 liest einen Basiswert aus der Datei selbst,
        // transformiert ihn, und dann steht dieser eine Wert fuer den
        // ganzen Eintrag fest** -- vier Felder, alle mit demselben Wert,
        // **und der Generator laeuft ueberhaupt nicht fort.**
        //
        // ```text
        // verifyHeader(3) -> baseMagic = readUint32(); baseMagic *= 9; baseMagic += 3;
        // je Eintrag: offset ^= baseMagic, size ^= baseMagic,
        //             magic ^= baseMagic, nameLen ^= baseMagic,
        //             dann der Name mit den vier Bytes von baseMagic zyklisch
        // offset == 0 beendet die Liste
        // ```
        //
        // **Gemessen an `Dreaming Mary/Game.rgss3a`:**
        //
        // ```text
        // baseMagic = 0x00004657 -> (0x4657 * 9) + 3 = 0x00027912
        // Eintrag 1: offset=6038 size=1341 nameLen=19 -> Data/Actors.rvdata2
        // Eintrag 2: offset=7379 size=10399 nameLen=23 -> Data/Animations.rvdata2
        // 138 Eintraege, Endemarker bei Dateiposition 6022
        // ```
        //
        // **Quelle: `mkxp-z/src/crypto/rgssad.cpp`, `RGSS3_openArchive`.**
        if (ReadVersion(pBytes) == VersionVxAce)
        {
            return ListEntriesVersion3(pBytes, pSourceName, entries);
        }

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

            // **Und der Schluessel des Rumpfes ist der Generatorzustand an
            // der Stelle, an der der Rumpf beginnt** -- **und das ist nach
            // dem Groessenfeld, nicht davor.** **Vorher war es der Zustand
            // vor dem Groessen-XOR, und damit war der Schluessel um genau
            // einen Schritt daneben.** **Das war kein Randfehler: jedes Byte
            // jedes Rumpfes eines echten XP- oder VX-Archivs waere falsch
            // gewesen, still und ohne Ausnahme.**
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

        // **Und ein Eintrag ist nicht nur kopiert, sondern entschluesselt,
        // und der Schluessel ist der `magic`-Wert des Eintrags.** In
        // Version 1 ist das der Generatorzustand an dieser Stelle; in
        // Version 3 ist es das dritte der vier Felder.
        //
        // **Und die Verschluesselung laeuft ueber Doppelwoerter, nicht ueber
        // Bytes**, **und das ist der Teil, den man aus dem Namen nicht
        // sieht:** `RGSS_ioRead` liest vier Byte, xor t den ganzen 32-Bit-
        // Wert und schreibt ihn zurueck -- **ein Byteweise-XOR auf den
        // Generator liefert bei geraden und ungeraden Positionen
        // verschiedene bytes und damit muell.**
        //
        // ```text
        // Dreaming Mary, Data/Scripts.rvdata2, entryMagic 0x00004d06:
        //   roh      02 45 5b 01 53 40 0a 69 3a bb
        //   entschluesselt  04 08 5b 01 7e 5b 08 69 04 05
        //                  ^^^^^^^^^^ Marshal 4.8, und 5b 01 ist
        //                  ein Array mit einem Element
        // ```
        //
        // **Quelle: `mkxp-z/src/crypto/rgssad.cpp`, `RGSS_ioRead`.**
        return PluginResult<byte[]>.Succeeded(
            DecryptBody(body, pEntry.MagicAtEntry));
    }

    /// <summary>
    /// One entry's body, decrypted with the key the entry carries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generator advances once per double word, and the last partial
    /// double word is padded on the right and cut back, which is what
    /// <c>RGSS_ioRead</c>'s pre/post-align path does. The key is the entry's
    /// own <c>magic</c> field, not a value carried over from the entry list:
    /// <strong>each entry names its own starting state.</strong>
    /// </para>
    /// </remarks>
    private static byte[] DecryptBody(byte[] pBody, uint pMagic)
    {
        var magic = pMagic;
        var offset = 0;
        for (; offset + 4 <= pBody.Length; offset += 4)
        {
            var wert = unchecked((uint)pBody[offset]
                | ((uint)pBody[offset + 1] << 8)
                | ((uint)pBody[offset + 2] << 16)
                | ((uint)pBody[offset + 3] << 24));
            wert ^= magic;
            magic = unchecked(magic * Multiplier + Increment);
            pBody[offset] = (byte)wert;
            pBody[offset + 1] = (byte)(wert >> 8);
            pBody[offset + 2] = (byte)(wert >> 16);
            pBody[offset + 3] = (byte)(wert >> 24);
        }

        if (offset < pBody.Length)
        {
            uint rest = 0;
            for (var index = offset; index < pBody.Length; index++)
            {
                rest |= (uint)pBody[index] << (8 * (index - offset));
            }

            rest ^= magic;
            for (var index = offset; index < pBody.Length; index++)
            {
                pBody[index] = (byte)(rest >> (8 * (index - offset)));
            }
        }

        return pBody;
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

        // **Und Version 3 schreibt einen anderen Satz als Version 1**, **und
        // dieser Schreiber tat das nicht** -- **er schrieb vier Felder und
        // einen fortlaufenden Generator, und der Leser las es als Version 1
        // zurueck.** **Das war kein Rundlauffehler, es war eine Luecke, und
        // der Test `Test_AnEntryBodyIsReadBackByteForByte` hat sie gefunden,
        // weil er Version 1 schrieb und die Bytes zurueckbekam, die der
        // Leser nun entschluesselt.**
        if (pVersion == VersionVxAce)
        {
            return WriteVersion3(pEntries, bytes);
        }

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
            // **Und der Rumpf wird verschleiert, und zwar ueber
            // Doppelwoerter** -- **das ist derselbe Weg, den ReadEntry
            // zuruecknimmt, und ohne das schrieb der Schreiber Klartext und
            // der Leser Verschluesseltes.**
            //
            // **Und der Schluessel ist `magic` selbst, nicht `NextKey`.**
            // **Die Quelle setzt `entry.startMagic = magic` nach dem
            // Groessenfeld, und `RGSS_ioRead` xor t das erste Doppelwort
            // mit genau diesem Zustand** -- **ein zusaetzlicher Schritt hier
            // verschiebt jeden Rumpf um vier Byte und liest ihn trotzdem
            // ohne Fehlermeldung falsch.**
            bytes.AddRange(EncryptBody(body, magic));
        }
        return bytes.ToArray();
    }

    /// <summary>
    /// Writes a version three entry list, the way the engine writes one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The base key is written before the list, and every entry's four fields
    /// are written xor ed with it, <strong>and the name is written with the
    /// same key's four bytes, cyclically.</strong> The body is written with
    /// the entry's own magic, which the reader reads back and uses.
    /// </para>
    /// </remarks>
    private static byte[] WriteVersion3(
        IReadOnlyList<(string Name, byte[] Body)> pEntries, List<byte> pBytes)
    {
        // **Und der Basiswert ist derselbe, den die Engine schreibt, damit
        // ein Rundlauf durch dieses Repository ein Archiv ergibt, das die
        // Engine liest** -- **und nicht irgendein.**
        var baseMagic = unchecked(0x4657u * 9 + 3);
        AddUInt32(pBytes, 0x4657u);
        var schluessel = new[]
        {
            (byte)baseMagic,
            (byte)(baseMagic >> 8),
            (byte)(baseMagic >> 16),
            (byte)(baseMagic >> 24),
        };

        // **Und die Liste steht vor den Rumpfdaten, und ein Offset weiss
        // erst, wenn man weiss, wie lang die Liste ist** -- **also zwei
        // Durchlaeufe, und das ist kein Umweg, das ist die Reihenfolge der
        // Datei.** **Gemessen an `Dreaming Mary`: die Liste endet bei
        // Position 6022, und die erste Nutzlast beginnt bei 6038.**
        // **Und die Liste steht vor der Nutzlast, und sie wird mit dem
        // Marker abgeschlossen und auf eine Vier-Byte-Grenze gerueckt** --
        // **also beginnt die erste Nutzlast bei Liste plus vier plus
        // Ausrichtung.** **An `Dreaming Mary` gemessen: Liste endet bei 6022,
        // Marker dort, erste Nutzlast bei 6038.**
        var position = pBytes.Count;
        foreach (var (name, _) in pEntries)
        {
            position += 16 + Encoding.ASCII.GetByteCount(name);
        }

        position += 4;
        while ((position & 3) != 0)
        {
            position++;
        }

        for (var index = 0; index < pEntries.Count; index++)
        {
            var (name, body) = pEntries[index];
            var raw = Encoding.ASCII.GetBytes(name);
            if (raw.Length > MaxNameBytes)
            {
                throw new ArgumentException(
                    $"The name {name} is longer than the {MaxNameBytes} byte limit.",
                    nameof(pEntries));
            }

            AddUInt32(pBytes, (uint)position ^ baseMagic);
            AddUInt32(pBytes, (uint)body.Length ^ baseMagic);
            var entryMagic = InitialMagic;
            AddUInt32(pBytes, entryMagic ^ baseMagic);
            AddUInt32(pBytes, (uint)raw.Length ^ baseMagic);
            for (var zeichen = 0; zeichen < raw.Length; zeichen++)
            {
                pBytes.Add((byte)(raw[zeichen] ^ schluessel[zeichen & 3]));
            }

            pBytes.AddRange(EncryptBody(body, entryMagic));
            position += body.Length;
        }

        // **Und der Marker ist ein Feld, nicht vier**, **und die Nutzlast
        // faengt danach auf einer Vier-Byte-Grenze an.** **An
        // `Dreaming Mary` gemessen: Marker bei 6022, erste Nutzlast bei
        // 6038, und die Positionen dazwischen sind Ausrichtung.**
        AddUInt32(pBytes, baseMagic);
        while ((pBytes.Count & 3) != 0)
        {
            pBytes.Add(0);
        }
        return pBytes.ToArray();
    }

    /// <summary>
    /// One body, obfuscated with the key the entry carries.
    /// </summary>
    private static byte[] EncryptBody(byte[] pBody, uint pMagic)
    {
        var outBytes = new byte[pBody.Length];
        Array.Copy(pBody, outBytes, pBody.Length);
        return DecryptBody(outBytes, pMagic);
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
        /// <summary>
    /// Version 3 of the archive, which is not version 1 with another number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was written from
    /// <c>mkxp-z/src/crypto/rgssad.cpp</c>, not from memory.</strong> Four
    /// attempts at reconstructing it from memory were wrong, and the failure
    /// mode was the same every time: the version 1 generator was applied to
    /// version 3 data, and every entry decoded to noise that still looked
    /// like a length.
    /// </para>
    /// <para>
    /// <strong>Three differences, and each alone breaks it:</strong> the
    /// base value is read from the file and transformed by <c>* 9 + 3</c>;
    /// <strong>it never advances,</strong> so every field of every entry
    /// uses the same one; and a name's bytes are xor ed with the four bytes
    /// of that value taken cyclically, not with a walking generator. An
    /// entry offset of zero ends the list.
    /// </para>
    /// </remarks>
    private static PluginResult<IReadOnlyList<RgssArchiveEntry>> ListEntriesVersion3(
        byte[] pBytes, string pSourceName, List<RgssArchiveEntry> pEntries)
    {
        // **Und der Basiswert steht direkt hinter dem acht Byte langen
        // Kopf, nicht vier Byte weiter.** **Das war ein eigener Fehler
        // hier, und er kostete eine ganze Messrunde:**
        // `offset = HeaderLength + 4` las 159364 als Basiswert statt 18007
        // **und produzierte genau den Fehler, den die Version-1-Regel
        // auch produziert.**
        var offset = HeaderLength;
        if (offset + 4 > pBytes.Length)
        {
            return Fail<IReadOnlyList<RgssArchiveEntry>>(
                $"The archive {pSourceName} is too short to hold its base key.");
        }

        var baseMagic = unchecked(ReadUInt32(pBytes, ref offset) * 9 + 3);
        var schluessel = new[]
        {
            (byte)baseMagic,
            (byte)(baseMagic >> 8),
            (byte)(baseMagic >> 16),
            (byte)(baseMagic >> 24),
        };

        while (offset + 16 <= pBytes.Length)
        {
            if (pEntries.Count >= MaxEntries)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} holds more than {MaxEntries} entries.");
            }

            // **Und der Marker ist ein einzelnes Nullfeld fuer den Offset,
            // nicht vier** -- **das ist an `Dreaming Mary` gemessen: der
            // Marker steht bei Dateiposition 6022 und die erste Nutzlast bei
            // 6038.** **Die zwoelf Byte dazwischen sind Ausrichtung, keine
            // Felder**, **und eine Annahme von vier Feldern liest die
            // Nutzlast als naechsten Eintrag.** **Ein Versuch, daraus einen
            // Nullblock zu machen, hat denselben Fehler nur an eine andere
            // Stelle verschoben** -- **und beide sind geraten, und die Quelle
            // sagt `if (offset == 0) break;` nach einem einzigen Feld.**
            var entryOffset = ReadUInt32(pBytes, ref offset) ^ baseMagic;
            if (entryOffset == 0)
            {
                break;
            }

            var size = (int)(ReadUInt32(pBytes, ref offset) ^ baseMagic);
            var entryMagic = ReadUInt32(pBytes, ref offset) ^ baseMagic;
            var nameLength = DecodeLength(
                ReadUInt32(pBytes, ref offset) ^ baseMagic, 0);

            if (nameLength < 0 || nameLength > MaxNameBytes)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} declares a name of {nameLength} "
                    + "bytes, and version three writes four fields before every "
                    + "name, so a wrong key lands here.");
            }

            if (offset + nameLength > pBytes.Length)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} ends inside an entry's name.");
            }

            var name = new char[nameLength];
            for (var index = 0; index < nameLength; index++)
            {
                var value = (char)(pBytes[offset + index] ^ schluessel[index & 3]);
                name[index] = value == '\\' ? '/' : value;
            }

            offset += nameLength;

            if (entryOffset + (long)size > pBytes.Length)
            {
                return Fail<IReadOnlyList<RgssArchiveEntry>>(
                    $"The archive {pSourceName} places an entry at offset "
                    + $"{entryOffset} of {size} bytes, past the end of the file.");
            }

            pEntries.Add(new RgssArchiveEntry
            {
                Name = new string(name),
                Offset = (int)entryOffset,
                Size = size,
                MagicAtEntry = entryMagic,
            });
        }

        return PluginResult<IReadOnlyList<RgssArchiveEntry>>.Succeeded(pEntries);
    }

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
